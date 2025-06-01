using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SmartCityParking.Services.Interfaces;

namespace SmartCityParking.Services.Implementations
{
    public class AuthService : IAuthService
    {
        // Simple in-memory user store for demo purposes
        private readonly Dictionary<string, string> _users = new()
        {
            { "user1", "password123" },
            { "admin", "admin123" },
            { "test", "test123" }
        };

        public Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            return Task.FromResult(_users.TryGetValue(username, out var storedPassword) && storedPassword == password);
        }

        // You might also want to add a method to generate JWT tokens
        public string GenerateJwtToken(string username, string secretKey)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.NameIdentifier, username)
                }),
                Expires = DateTime.UtcNow.AddHours(24),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}