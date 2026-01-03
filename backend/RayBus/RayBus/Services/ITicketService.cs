using RayBus.Models.DTOs;

namespace RayBus.Services
{
    /// <summary>
    /// Bilet servis interface'i
    /// </summary>
    public interface ITicketService
    {
        Task<ApiResponse<TicketDetailDTO>> GetTicketDetailsAsync(int reservationId);
        Task<ApiResponse<TicketDetailDTO>> GetTicketDetailsByTicketNumberAsync(string ticketNumber);
        Task<ApiResponse<byte[]>> GenerateTicketPDFAsync(int reservationId);
    }
}

