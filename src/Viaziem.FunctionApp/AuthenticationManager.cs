using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Viaziem.Contracts.Entities;

namespace Viaziem.FunctionApp
{
    public interface IAuthenticationManager
    {
        string GenerateJwtToken(User user);
        (Guid? userId, JwtSecurityToken jwtToken) ValidateToken(string token);
    }

    public class AuthenticationManager : IAuthenticationManager
    {
        private readonly int _expirationDays;
        private readonly string _secret;

        public AuthenticationManager(string secret, string expirationDays)
        {
            _secret = secret ?? throw new ArgumentException();
            _expirationDays = int.Parse(expirationDays);
        }

        public string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secret);

            var claims = new List<Claim> {new Claim("id", user.Id.ToString())};

            if (!string.IsNullOrEmpty(user.Role)) claims.Add(new Claim("role", user.Role));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(_expirationDays),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public (Guid? userId, JwtSecurityToken jwtToken) ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secret);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);

                var jwtToken = (JwtSecurityToken) validatedToken;
                var readOnlySpan = jwtToken.Claims.First(x => x.Type == "id").Value;
                var userId = Guid.Parse(readOnlySpan);

                // attach user to context on successful jwt validation
                return (userId, jwtToken);
            }
            catch (Exception ex)
            {
                // do nothing if jwt validation fails
                // user is not attached to context so request won't have access to secure routes
                return (null, null);
            }
        }
    }
}