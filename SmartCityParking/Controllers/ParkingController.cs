using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCityParking.Grains;
using SmartCityParking.Models;
using SmartCityParking.Services;
using System.Security.Claims;
using SmartCityParking.Grains.Interfaces;
using SmartCityParking.Services.Interfaces;

namespace SmartCityParking.Controllers
{
    [ApiController]
        [Route("api/[controller]")]
        [Authorize]
        public class ParkingController : ControllerBase
        {
            private readonly IGrainFactory _grainFactory;
            private readonly IMongoService _mongoService;

            public ParkingController(IGrainFactory grainFactory, IMongoService mongoService)
            {
                _grainFactory = grainFactory;
                _mongoService = mongoService;
            }

            [HttpPost("park")]
            public async Task<ActionResult<ApiResponse<ParkingResponse>>> Park()
            {
                var userId = User.FindFirst(ClaimTypes.Name)?.Value; 
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<ParkingResponse>
                    {
                        Success = false,
                        Message = "Invalid token"
                    });
                }

                var parkingManager = _grainFactory.GetGrain<IParkingManagerGrain>("main");
                var spotId = await parkingManager.ReserveParkingSpotAsync(userId);

                if (spotId.HasValue)
                {
                    return Ok(new ApiResponse<ParkingResponse>
                    {
                        Success = true,
                        Message = $"Successfully reserved parking spot {spotId.Value}",
                        Data = new ParkingResponse
                        {
                            SpotId = spotId.Value,
                            UserId = userId,
                            Timestamp = DateTime.UtcNow
                        }
                    });
                }

                return BadRequest(new ApiResponse<ParkingResponse>
                {
                    Success = false,
                    Message = "No available parking spots or user already has a spot"
                });
            }

            [HttpPost("leave")]
            public async Task<ActionResult<ApiResponse<object>>> Leave()
            {
                var userId = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid token"
                    });
                }

                var parkingManager = _grainFactory.GetGrain<IParkingManagerGrain>("main");
                var success = await parkingManager.ReleaseParkingSpotAsync(userId);

                if (success)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Successfully released parking spot"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User does not have a reserved parking spot"
                });
            }

            [HttpGet("status")]
            public async Task<ActionResult<ApiResponse<ParkingStatus>>> GetStatus()
            {
                var parkingManager = _grainFactory.GetGrain<IParkingManagerGrain>("main");
                var status = await parkingManager.GetParkingStatusAsync();

                return Ok(new ApiResponse<ParkingStatus>
                {
                    Success = true,
                    Message = "Parking status retrieved successfully",
                    Data = status
                });
            }

            [HttpGet("history")]
            public async Task<ActionResult<ApiResponse<List<ParkingEvent>>>> GetHistory()
            {
                var userId = User.FindFirst("userId")?.Value;
                var history = await _mongoService.GetParkingHistoryAsync(userId);

                return Ok(new ApiResponse<List<ParkingEvent>>
                {
                    Success = true,
                    Message = "Parking history retrieved successfully",
                    Data = history
                });
            }
        }
}