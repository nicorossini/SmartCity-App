using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCityParking.Grains;
using SmartCityParking.Models;
using SmartCityParking.Services.Interfaces;
using System.Security.Claims;

namespace SmartCityParking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtService _jwtService;

        public AuthController(IAuthService authService, IJwtService jwtService)
        {
            _authService = authService;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
        {
            if (await _authService.ValidateCredentialsAsync(request.Username, request.Password))
            {
                var token = _jwtService.GenerateToken(request.Username);
                return Ok(new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = new LoginResponse
                    {
                        Token = token,
                        Username = request.Username
                    }
                });
            }

            return Unauthorized(new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = "Invalid credentials"
            });
        }
    }
}