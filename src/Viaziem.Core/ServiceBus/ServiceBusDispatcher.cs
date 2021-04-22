using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Viaziem.Core.DataProviders;
using Viaziem.Core.EventHub;
using Viaziem.Core.Extensions;

namespace Viaziem.Core.ServiceBus
{
    public interface IServiceBusDispatcher
    {
        Task SendMessageAsync(Stream imageStream, Guid userId);
    }

    public class ServiceBusDispatcher : IServiceBusDispatcher
    {
        private const string StorageConnectionString = "[StorageConnectionString]";

        private const string StorageContainerName = "imgcontainer";

        private const string UserId = "UserId";
        private readonly string _connectionString;

        private readonly IEventDispatcher _eventDispatcher;
        private readonly string _queueName;
        private readonly IUsersProfilesDataProvider _usersProfilesDataProvider;

        public ServiceBusDispatcher(IEventDispatcher eventDispatcher,
            IUsersProfilesDataProvider usersProfilesDataProvider,
            string connectionString,
            string queueName)
        {
            _eventDispatcher = eventDispatcher;
            _usersProfilesDataProvider = usersProfilesDataProvider;
            _connectionString = connectionString;
            _queueName = queueName;

            var client = new ServiceBusClient(_connectionString);

            var processor = client.CreateProcessor(_queueName, new ServiceBusProcessorOptions());

            // add handler to process messages
            processor.ProcessMessageAsync += MessageHandler;

            // add handler to process any errors
            processor.ProcessErrorAsync += ErrorHandler;

            // start processing 
            processor.StartProcessingAsync();
        }

        public async Task SendMessageAsync(Stream imageStream, Guid userId)
        {
            await using var client = new ServiceBusClient(_connectionString);
            // create a sender for the queue 
            var sender = client.CreateSender(_queueName);

            // create a message that we can send
            var imageAsBytesArray = imageStream.ReadFully();
            var bd = new BinaryData(imageAsBytesArray);
            var message = new ServiceBusMessage(bd);
            message.ApplicationProperties.Add(UserId, userId);

            // send the message
            await sender.SendMessageAsync(message);
        }

        public async Task MessageHandler(ProcessMessageEventArgs args)
        {
            var body = args.Message.Body;

            var messageApplicationProperties = args.Message.ApplicationProperties;
            if (!messageApplicationProperties.ContainsKey(UserId)) return;

            var userId = Guid.Parse(messageApplicationProperties[UserId].ToString() ?? string.Empty);

            var fileId = Guid.NewGuid();
            var fileName = $"{fileId}.jpg";

            var blobServiceClient = new BlobServiceClient(StorageConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(StorageContainerName);

            await using var stream = new MemoryStream(body.ToArray());

            await containerClient.UploadBlobAsync(fileName, stream);

            await _usersProfilesDataProvider.AddPictureId(fileId, userId);
            await _eventDispatcher.SendImageWasUpload(fileId, userId);

            // complete the message. messages is deleted from the queue. 
            await args.CompleteMessageAsync(args.Message);
        }

        // handle any errors when receiving messages
        public static Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }
    }
}