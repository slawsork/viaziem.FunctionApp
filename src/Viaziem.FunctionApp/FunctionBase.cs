using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Viaziem.Contracts.Entities;
using Viaziem.Contracts.Exceptions;

namespace Viaziem.FunctionApp
{
    public class FunctionBase
    {
        protected readonly IAuthenticationManager AuthenticationManager;
        protected readonly IMapper Mapper;

        protected FunctionBase(IAuthenticationManager authenticationManager, IMapper mapper)
        {
            AuthenticationManager = authenticationManager ?? throw new ArgumentException();
            Mapper = mapper ?? throw new AggregateException();
        }

        protected static Guid UserId { get; set; }

        protected async Task<IActionResult> ResponseScope(HttpRequest request,
            bool allowAnonymous,
            string role,
            Func<Task<IActionResult>> func)
        {
            try
            {
                if (!allowAnonymous)
                {
                    var jwtToken = Authenticate(request);

                    if (!string.IsNullOrEmpty(role)) TryAuthorize(role, jwtToken);
                }

                return await func();
            }
            catch (AuthenticationException)
            {
                return new UnauthorizedResult();
            }
            catch (AuthorizationException)
            {
                return new StatusCodeResult(StatusCodes.Status403Forbidden);
            }
            catch (NotFoundException ex)
            {
                return new NotFoundObjectResult(ex.Message);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult($"{ex.Message}");
            }
        }

        private static void TryAuthorize(string role, JwtSecurityToken jwtToken)
        {
            var roleFromToken = jwtToken?.Claims.FirstOrDefault(x => x.Type == "role")?.Value;

            if (roleFromToken == null ||
                !string.Equals(roleFromToken, role, StringComparison.InvariantCultureIgnoreCase))
                throw new AuthorizationException("Forbidden");
        }

        private JwtSecurityToken Authenticate(HttpRequest request)
        {
            var token = request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (token == null) throw new AuthenticationException();

            var (userId, jwtToken) = AuthenticationManager.ValidateToken(token);

            if (!userId.HasValue) throw new AuthenticationException();

            UserId = userId.Value;

            return jwtToken;
        }

        protected string SetTokenToResponse(HttpRequest httpRequest, User user)
        {
            var token = AuthenticationManager.GenerateJwtToken(user);

            var tokenHeader = $"Bearer {token}";
            httpRequest.HttpContext.Response.Headers["Authorization"] = tokenHeader;

            return tokenHeader;
        }
    }
}