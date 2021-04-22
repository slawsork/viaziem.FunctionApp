using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Viaziem.Core.ServiceBus;

namespace Viaziem.FunctionApp
{
    public class ImageFunction : FunctionBase
    {
        private const string ProfileImageFileName = "image";
        private readonly IServiceBusDispatcher _serviceBusDispatcher;

        public ImageFunction(IServiceBusDispatcher serviceBusDispatcher,
            IAuthenticationManager authenticationManager,
            IMapper mapper) : base(authenticationManager, mapper)
        {
            _serviceBusDispatcher = serviceBusDispatcher;
        }

        [FunctionName("Upload")]
        public Task<IActionResult> Upload(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest request,
            ILogger log)
        {
            return ResponseScope(request,
                true,
                string.Empty,
                async () =>
                {
                    log.LogInformation("Upload picture");

                    var formFile = request.Form.Files[ProfileImageFileName];

                    using var reader = new StreamReader(formFile.OpenReadStream());

                    await _serviceBusDispatcher.SendMessageAsync(reader.BaseStream, UserId);

                    return new OkObjectResult("Image was uploaded ok.");
                });
        }
    }
}