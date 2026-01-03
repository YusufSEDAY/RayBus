using Microsoft.AspNetCore.Mvc;
using RayBus.Models.DTOs;
using RayBus.Services;

namespace RayBus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketController : ControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly ILogger<TicketController> _logger;

        public TicketController(
            ITicketService ticketService,
            ILogger<TicketController> logger)
        {
            _ticketService = ticketService;
            _logger = logger;
        }

        /// <summary>
        /// Bilet bilgilerini getirir (ReservationID ile)
        /// </summary>
        [HttpGet("reservation/{reservationId}")]
        public async Task<IActionResult> GetTicketByReservationId(int reservationId)
        {
            if (reservationId <= 0)
            {
                return BadRequest(new ApiResponse<TicketDetailDTO>
                {
                    Success = false,
                    Message = "Geçersiz rezervasyon ID'si"
                });
            }

            var response = await _ticketService.GetTicketDetailsAsync(reservationId);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Bilet bilgilerini getirir (TicketNumber ile)
        /// </summary>
        [HttpGet("ticket-number/{ticketNumber}")]
        public async Task<IActionResult> GetTicketByTicketNumber(string ticketNumber)
        {
            if (string.IsNullOrWhiteSpace(ticketNumber))
            {
                return BadRequest(new ApiResponse<TicketDetailDTO>
                {
                    Success = false,
                    Message = "Bilet numarası gereklidir"
                });
            }

            var response = await _ticketService.GetTicketDetailsByTicketNumberAsync(ticketNumber);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Bilet PDF'ini oluşturur ve döndürür
        /// </summary>
        [HttpGet("pdf/{reservationId}")]
        public async Task<IActionResult> GenerateTicketPDF(int reservationId)
        {
            if (reservationId <= 0)
            {
                return BadRequest(new ApiResponse<byte[]>
                {
                    Success = false,
                    Message = "Geçersiz rezervasyon ID'si"
                });
            }

            var response = await _ticketService.GenerateTicketPDFAsync(reservationId);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            if (response.Data == null || response.Data.Length == 0)
            {
                return BadRequest(new ApiResponse<byte[]>
                {
                    Success = false,
                    Message = "PDF oluşturulamadı"
                });
            }

            return File(response.Data, "application/pdf", $"Bilet_{reservationId}.pdf");
        }
    }
}

