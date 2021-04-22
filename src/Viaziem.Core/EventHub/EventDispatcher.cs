using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace Viaziem.Core.EventHub
{
    public interface IEventDispatcher
    {
        Task SendImageWasUpload(Guid fileId, Guid userId);
    }

    public class EventDispatcher : IEventDispatcher
    {
        private const string ConnectionString = "[event hub connection string]";
        private const string EventHubName = "imageaddition";

        public async Task SendImageWasUpload(Guid fileId, Guid userId)
        {
            await using var producerClient = new EventHubProducerClient(ConnectionString, EventHubName);
            // Create a batch of events 
            using var eventBatch = await producerClient.CreateBatchAsync();

            // Add events to the batch. An event is a represented by a collection of bytes and metadata.             
            eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(fileId.ToString())));

            // Use the producer client to send the batch of events to the event hub
            await producerClient.SendAsync(eventBatch);
            Console.WriteLine("A batch of 3 events has been published.");
        }
    }
}