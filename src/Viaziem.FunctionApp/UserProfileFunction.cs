using System.IO;
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

namespace Viaziem.FunctionApp
{
    public class UserProfileFunction : FunctionBase
    {
        private readonly IUsersProfilesDataProvider _usersProfilesDataProvider;

        public UserProfileFunction(IUsersProfilesDataProvider usersProfilesDataProvider,
            IAuthenticationManager authenticationManager,
            IMapper mapper) : base(authenticationManager, mapper)
        {
            _usersProfilesDataProvider = usersProfilesDataProvider;
        }

        [FunctionName("GetUserProfile")]
        public Task<IActionResult> GetUserProfile(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            HttpRequest request,
            ILogger log)
        {
            return ResponseScope(request,
                false,
                string.Empty,
                async () =>
                {
                    log.LogInformation("Get profile");

                    var userProfile = await _usersProfilesDataProvider.GetUserProfile(UserId);

                    var result = userProfile != null ? Mapper.Map<UserProfileDto>(userProfile) : new UserProfileDto();

                    return new OkObjectResult(result);
                });
        }

        [FunctionName("UpdateUserProfile")]
        public Task<IActionResult> UpdateUserProfile(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest request,
            ILogger log)
        {
            return ResponseScope(request,
                true,
                string.Empty,
                async () =>
                {
                    log.LogInformation("Update user profile");

                    var content = await new StreamReader(request.Body).ReadToEndAsync();

                    var userProfileDto = JsonConvert.DeserializeObject<UserProfileDto>(content);

                    if (userProfileDto == null) return new BadRequestObjectResult("User profile is not valid.");

                    await _usersProfilesDataProvider.UpdateUserProfile(UserId, userProfileDto);

                    return new OkObjectResult("User profile was updated.");
                });
        }
    }
}