using RayBus.Models.DTOs;

namespace RayBus.Services
{
    /// <summary>
    /// Bildirim servis interface'i
    /// </summary>
    public interface INotificationService
    {
        Task<ApiResponse<object>> SendNotificationAsync(SendNotificationDTO sendDto);
        Task<ApiResponse<IEnumerable<NotificationQueueDTO>>> GetPendingNotificationsAsync(int maxCount = 100);
        Task<ApiResponse<bool>> UpdateNotificationStatusAsync(UpdateNotificationStatusDTO updateDto);
        Task<ApiResponse<UserNotificationPreferencesDTO>> GetUserPreferencesAsync(int userId);
        Task<ApiResponse<UserNotificationPreferencesDTO>> UpdateUserPreferencesAsync(int userId, UserNotificationPreferencesDTO preferences);
        Task<ApiResponse<IEnumerable<NotificationQueueDTO>>> GetUserNotificationsAsync(int userId, int? limit = null);
    }
}

