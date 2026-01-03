using Microsoft.AspNetCore.Mvc;
using RayBus.Models.DTOs;
using RayBus.Services;

namespace RayBus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserStatisticsController : ControllerBase
    {
        private readonly IUserStatisticsService _userStatisticsService;
        private readonly ILogger<UserStatisticsController> _logger;

        public UserStatisticsController(
            IUserStatisticsService userStatisticsService,
            ILogger<UserStatisticsController> logger)
        {
            _userStatisticsService = userStatisticsService;
            _logger = logger;
        }

        /// <summary>
        /// Kullanıcı istatistiklerini getirir
        /// </summary>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserStatistics(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new ApiResponse<UserStatisticsDTO>
                {
                    Success = false,
                    Message = "Geçersiz kullanıcı ID'si"
                });
            }

            var response = await _userStatisticsService.GetUserStatisticsAsync(userId);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Kullanıcı raporunu getirir (detaylı)
        /// </summary>
        [HttpGet("{userId}/report")]
        public async Task<IActionResult> GetUserReport(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Geçersiz kullanıcı ID'si"
                });
            }

            var response = await _userStatisticsService.GetUserReportAsync(userId);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}

