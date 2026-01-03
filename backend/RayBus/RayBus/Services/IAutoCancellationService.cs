using RayBus.Models.DTOs;

namespace RayBus.Services
{
    /// <summary>
    /// Otomatik iptal servis interface'i
    /// </summary>
    public interface IAutoCancellationService
    {
        Task<ApiResponse<AutoCancellationResultDTO>> ProcessTimeoutReservationsAsync(int timeoutMinutes = 15);
        Task<ApiResponse<AutoCancellationSettingsDTO>> GetSettingsAsync();
        Task<ApiResponse<AutoCancellationSettingsDTO>> UpdateSettingsAsync(int timeoutMinutes);
        Task<ApiResponse<IEnumerable<AutoCancellationLogDTO>>> GetCancellationLogsAsync(int? userId = null);
    }
}

