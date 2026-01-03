using RayBus.Models.DTOs;

namespace RayBus.Services
{
    /// <summary>
    /// Tren servis interface'i
    /// </summary>
    public interface ITrainService
    {
        Task<ApiResponse<IEnumerable<TrainDTO>>> GetAllTrainsAsync();
        Task<ApiResponse<TrainDTO>> GetTrainByIdAsync(int id);
        Task<ApiResponse<IEnumerable<TrainDTO>>> SearchTrainsAsync(TrainSearchDTO searchDto);
    }
}


