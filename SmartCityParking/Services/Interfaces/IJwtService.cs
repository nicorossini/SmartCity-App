using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartCityParking.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(string userId);
        string? ValidateToken(string token);
    }

    
}