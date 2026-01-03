using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RayBus.Data;
using RayBus.Models.DTOs;
using RayBus.Services;

namespace RayBus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationController : ControllerBase
    {
        private readonly IReservationService _reservationService;
        private readonly RayBusDbContext _context;
        private readonly ILogger<ReservationController> _logger;

        public ReservationController(
            IReservationService reservationService, 
            RayBusDbContext context,
            ILogger<ReservationController> logger)
        {
            _reservationService = reservationService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Tüm rezervasyonları getirir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllReservations()
        {
            var response = await _reservationService.GetAllReservationsAsync();
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Kullanıcıya ait rezervasyonları getirir
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetReservationsByUserId(int userId)
        {
            var response = await _reservationService.GetReservationsByUserIdAsync(userId);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Yeni bir rezervasyon oluşturur
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationDTO createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _reservationService.CreateReservationAsync(createDto);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return CreatedAtAction(nameof(GetReservationById), new { id = response.Data?.ReservationID }, response);
        }

        /// <summary>
        /// Rezervasyon ödemesini tamamlar (Senaryo B: "Rezerve Et, Sonra Öderim")
        /// </summary>
        [HttpPost("complete-payment")]
        public async Task<IActionResult> CompletePayment([FromBody] CompletePaymentDTO completePaymentDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _reservationService.CompletePaymentAsync(completePaymentDto);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Belirli bir rezervasyonu getirir
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReservationById(int id)
        {
            var response = await _reservationService.GetReservationByIdAsync(id);
            
            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Rezervasyonu iptal eder
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelReservation(int id, [FromBody] CancelReservationDTO? cancelDto = null)
        {
            var response = await _reservationService.CancelReservationAsync(id, cancelDto);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Tüm iptal nedenlerini getirir
        /// </summary>
        [HttpGet("cancellation-reasons")]
        public async Task<IActionResult> GetCancellationReasons()
        {
            try
            {
                var reasons = await _context.CancellationReasons
                    .OrderBy(r => r.ReasonID)
                    .Select(r => new CancellationReasonDTO
                    {
                        ReasonID = r.ReasonID,
                        ReasonText = r.ReasonText
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<IEnumerable<CancellationReasonDTO>>
                {
                    Success = true,
                    Message = "İptal nedenleri başarıyla getirildi",
                    Data = reasons
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İptal nedenleri getirilirken hata oluştu");
                return StatusCode(500, new ApiResponse<IEnumerable<CancellationReasonDTO>>
                {
                    Success = false,
                    Message = "İptal nedenleri getirilirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Özel iptal nedeni ekler
        /// </summary>
        [HttpPost("cancellation-reasons")]
        public async Task<IActionResult> CreateCancellationReason([FromBody] CreateCancellationReasonDTO createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(createDto.ReasonText))
            {
                return BadRequest(new ApiResponse<CancellationReasonDTO>
                {
                    Success = false,
                    Message = "İptal nedeni boş olamaz",
                    Errors = new List<string> { "ReasonText alanı gereklidir" }
                });
            }

            try
            {
                var cancellationReason = new Models.Entities.CancellationReason
                {
                    ReasonText = createDto.ReasonText.Trim()
                };

                _context.CancellationReasons.Add(cancellationReason);
                await _context.SaveChangesAsync();

                var dto = new CancellationReasonDTO
                {
                    ReasonID = cancellationReason.ReasonID,
                    ReasonText = cancellationReason.ReasonText
                };

                return CreatedAtAction(nameof(GetCancellationReasons), null, new ApiResponse<CancellationReasonDTO>
                {
                    Success = true,
                    Message = "İptal nedeni başarıyla eklendi",
                    Data = dto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İptal nedeni eklenirken hata oluştu");
                return StatusCode(500, new ApiResponse<CancellationReasonDTO>
                {
                    Success = false,
                    Message = "İptal nedeni eklenirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}

