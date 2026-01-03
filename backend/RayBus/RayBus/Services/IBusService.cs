using RayBus.Models.DTOs;

namespace RayBus.Services
{
    /// <summary>
    /// Otobüs servis interface'i
    /// </summary>
    public interface IBusService
    {
        Task<ApiResponse<IEnumerable<BusDTO>>> GetAllBusesAsync();
        Task<ApiResponse<BusDTO>> GetBusByIdAsync(int id);
        Task<ApiResponse<IEnumerable<BusDTO>>> SearchBusesAsync(BusSearchDTO searchDto);
    }
}


