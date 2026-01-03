using RayBus.Models.DTOs;

namespace RayBus.Services
{
    /// <summary>
    /// Kullanıcı istatistikleri servis interface'i
    /// </summary>
    public interface IUserStatisticsService
    {
        Task<ApiResponse<UserStatisticsDTO>> GetUserStatisticsAsync(int userId);
        Task<ApiResponse<object>> GetUserReportAsync(int userId);
    }
}

