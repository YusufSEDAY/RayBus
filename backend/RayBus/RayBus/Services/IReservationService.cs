using RayBus.Models.DTOs;

namespace RayBus.Services
{
    /// <summary>
    /// Rezervasyon servis interface'i
    /// </summary>
    public interface IReservationService
    {
        Task<ApiResponse<IEnumerable<ReservationDTO>>> GetAllReservationsAsync();
        Task<ApiResponse<IEnumerable<ReservationDTO>>> GetReservationsByUserIdAsync(int userId);
        Task<ApiResponse<ReservationDTO>> GetReservationByIdAsync(int id);
        Task<ApiResponse<ReservationDTO>> CreateReservationAsync(CreateReservationDTO createDto);
        Task<ApiResponse<bool>> CompletePaymentAsync(CompletePaymentDTO completePaymentDto);
        Task<ApiResponse<bool>> CancelReservationAsync(int id, CancelReservationDTO? cancelDto = null);
    }
}


