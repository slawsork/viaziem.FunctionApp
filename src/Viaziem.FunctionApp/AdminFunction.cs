using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Viaziem.Contracts.Dtos;
using Viaziem.Core.DataProviders;
using Viaziem.Core.Helpers;

namespace Viaziem.FunctionApp
{
    public class AdminFunction : FunctionBase
    {
        private readonly IUsersDataProvider _usersDataProvider;

        public AdminFunction(IUsersDataProvider usersDataProvider,
            IPasswordHelper passwordHelper,
            IAuthenticationManager authenticationManager,
            IMapper mapper) : base(authenticationManager, mapper)
        {
            _usersDataProvider = usersDataProvider;
        }

        [FunctionName("GetUsers")]
        public Task<IActionResult> GetUsers(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            HttpRequest request,
            ILogger log)
        {
            return ResponseScope(request,
                false,
                "Admin",
                async () =>
                {
                    var users = await _usersDataProvider.GetAllUsers();
                    return new OkObjectResult(users.Select(x => Mapper.Map<UserInfo>(x)).ToArray());
                });
        }

        [FunctionName("SetUserStatus")]
        public Task<IActionResult> SetUserStatus(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest request,
            ILogger log)
        {
            return ResponseScope(request,
                false,
                "Admin",
                async () =>
                {
                    var content = await new StreamReader(request.Body).ReadToEndAsync();

                    var userInfo = JsonConvert.DeserializeObject<UserInfo>(content);

                    await _usersDataProvider.SetUserStatus(userInfo);
                    return new OkResult();
                });
        }
    }
}