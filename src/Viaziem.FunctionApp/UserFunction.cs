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
using Viaziem.Core.Helpers;

namespace Viaziem.FunctionApp
{
    public class UserFunction : FunctionBase
    {
        private readonly IPasswordHelper _passwordHelper;
        private readonly IUsersDataProvider _usersDataProvider;

        public UserFunction(IUsersDataProvider usersDataProvider,
            IPasswordHelper passwordHelper,
            IAuthenticationManager authenticationManager,
            IMapper mapper) : base(authenticationManager, mapper)
        {
            _usersDataProvider = usersDataProvider;
            _passwordHelper = passwordHelper;
        }

        [FunctionName("Signup")]
        public Task<IActionResult> Signup(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest request,
            ILogger log)
        {
            return ResponseScope(request,
                true,
                string.Empty,
                async () =>
                {
                    log.LogInformation("Signup");

                    var content = await new StreamReader(request.Body).ReadToEndAsync();

                    var userDto = JsonConvert.DeserializeObject<UserRequestDto>(content);

                    if (userDto == null ||
                        string.IsNullOrEmpty(userDto.Email) ||
                        string.IsNullOrEmpty(userDto.Password))
                        return new BadRequestObjectResult("User credentials are not valid.");

                    var userExists = await _usersDataProvider.UserExists(userDto.Email);
                    if (userExists) return new BadRequestObjectResult("User with such email is already exists.");

                    var hashedPassword = _passwordHelper.Create(userDto.Password);
                    await _usersDataProvider.CreateUser(userDto.Email, hashedPassword);

                    return new OkObjectResult("User was registered.");
                });
        }

        [FunctionName("Login")]
        public Task<IActionResult> Login(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest request,
            ILogger log)
        {
            return ResponseScope(request,
                true,
                string.Empty,
                async () =>
                {
                    log.LogInformation("Login");

                    var content = await new StreamReader(request.Body).ReadToEndAsync();

                    var userDto = JsonConvert.DeserializeObject<UserRequestDto>(content);

                    if (userDto == null ||
                        string.IsNullOrEmpty(userDto.Email) ||
                        string.IsNullOrEmpty(userDto.Password))
                        return new BadRequestObjectResult("User credentials are not valid.");

                    var user = await _usersDataProvider.GetUser(userDto.Email);

                    // not found 

                    if (user == null || !_passwordHelper.Validate(userDto.Password, user.Password))
                        return new UnauthorizedResult();

                    var token = SetTokenToResponse(request, user);

                    return new OkObjectResult(new UserResponseDto
                    {
                        Email = user.Email,
                        Token = token
                    });
                });
        }
    }
}