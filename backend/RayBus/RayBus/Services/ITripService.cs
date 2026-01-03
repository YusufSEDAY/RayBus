using RayBus.Models.DTOs;

namespace RayBus.Services
{
    public interface ITripService
    {
        Task<ApiResponse<TripDetailDTO>> GetTripDetailAsync(int tripId);
        Task<ApiResponse<TripDetailDTO>> CreateTripAsync(CreateTripDTO createDto);
    }
}

