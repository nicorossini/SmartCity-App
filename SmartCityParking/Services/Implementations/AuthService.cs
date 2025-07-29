using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SmartCityParking.Services.Interfaces;

namespace SmartCityParking.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IJwtService _jwtService;
        // Simple in-memory user store for demo purposes
        private readonly Dictionary<string, string> _users = new()
        {
            { "user1", "password123" },
            { "admin", "admin123" },
            { "test", "test123" }
        };

        public AuthService(IJwtService jwtService) // Inject JwtService
        {
            _jwtService = jwtService;
        }

        public Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            return Task.FromResult(_users.TryGetValue(username, out var storedPassword) && storedPassword == password);
        }
         public async Task<string?> LoginAsync(string username, string password)
        {
            if (await ValidateCredentialsAsync(username, password))
            {
                return _jwtService.GenerateToken(username); 
            }
            return null;
        }
    }
}