using Microsoft.AspNetCore.Mvc;
using RayBus.Models.DTOs;
using RayBus.Services;

namespace RayBus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationService notificationService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Bildirim gönderir
        /// </summary>
        [HttpPost("send")]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationDTO sendDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _notificationService.SendNotificationAsync(sendDto);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Bekleyen bildirimleri getirir (işlem için)
        /// </summary>
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingNotifications([FromQuery] int maxCount = 100)
        {
            var response = await _notificationService.GetPendingNotificationsAsync(maxCount);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Bildirim durumunu günceller
        /// </summary>
        [HttpPut("status")]
        public async Task<IActionResult> UpdateNotificationStatus([FromBody] UpdateNotificationStatusDTO updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _notificationService.UpdateNotificationStatusAsync(updateDto);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Kullanıcı bildirim tercihlerini getirir
        /// </summary>
        [HttpGet("preferences/{userId}")]
        public async Task<IActionResult> GetUserPreferences(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new ApiResponse<UserNotificationPreferencesDTO>
                {
                    Success = false,
                    Message = "Geçersiz kullanıcı ID'si"
                });
            }

            var response = await _notificationService.GetUserPreferencesAsync(userId);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Kullanıcı bildirim tercihlerini günceller
        /// </summary>
        [HttpPut("preferences/{userId}")]
        public async Task<IActionResult> UpdateUserPreferences(int userId, [FromBody] UserNotificationPreferencesDTO preferences)
        {
            if (userId <= 0)
            {
                return BadRequest(new ApiResponse<UserNotificationPreferencesDTO>
                {
                    Success = false,
                    Message = "Geçersiz kullanıcı ID'si"
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _notificationService.UpdateUserPreferencesAsync(userId, preferences);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Kullanıcı bildirimlerini getirir
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserNotifications(int userId, [FromQuery] int? limit = null)
        {
            if (userId <= 0)
            {
                return BadRequest(new ApiResponse<IEnumerable<NotificationQueueDTO>>
                {
                    Success = false,
                    Message = "Geçersiz kullanıcı ID'si"
                });
            }

            var response = await _notificationService.GetUserNotificationsAsync(userId, limit);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}

