using RayBus.Models.DTOs;

namespace RayBus.Services
{
    public interface ICityService
    {
        Task<ApiResponse<IEnumerable<CityDTO>>> GetAllCitiesAsync();
        Task<ApiResponse<IEnumerable<StationDTO>>> GetStationsByCityIdAsync(int cityId);
        Task<ApiResponse<IEnumerable<TerminalDTO>>> GetTerminalsByCityIdAsync(int cityId);
    }
}

