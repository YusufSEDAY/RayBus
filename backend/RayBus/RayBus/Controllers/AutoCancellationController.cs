using Microsoft.AspNetCore.Mvc;
using RayBus.Models.DTOs;
using RayBus.Services;

namespace RayBus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutoCancellationController : ControllerBase
    {
        private readonly IAutoCancellationService _autoCancellationService;
        private readonly ILogger<AutoCancellationController> _logger;

        public AutoCancellationController(
            IAutoCancellationService autoCancellationService,
            ILogger<AutoCancellationController> logger)
        {
            _autoCancellationService = autoCancellationService;
            _logger = logger;
        }

        /// <summary>
        /// Zaman aşımına uğrayan rezervasyonları iptal eder
        /// </summary>
        [HttpPost("process")]
        public async Task<IActionResult> ProcessTimeoutReservations([FromQuery] int timeoutMinutes = 15)
        {
            var response = await _autoCancellationService.ProcessTimeoutReservationsAsync(timeoutMinutes);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Otomatik iptal ayarlarını getirir
        /// </summary>
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var response = await _autoCancellationService.GetSettingsAsync();
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Otomatik iptal ayarlarını günceller
        /// </summary>
        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] int timeoutMinutes)
        {
            if (timeoutMinutes < 1 || timeoutMinutes > 1440) // 1 dakika - 24 saat
            {
                return BadRequest(new ApiResponse<AutoCancellationSettingsDTO>
                {
                    Success = false,
                    Message = "Timeout süresi 1-1440 dakika arasında olmalıdır"
                });
            }

            var response = await _autoCancellationService.UpdateSettingsAsync(timeoutMinutes);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// İptal loglarını getirir
        /// </summary>
        [HttpGet("logs")]
        public async Task<IActionResult> GetCancellationLogs([FromQuery] int? userId = null)
        {
            var response = await _autoCancellationService.GetCancellationLogsAsync(userId);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}

