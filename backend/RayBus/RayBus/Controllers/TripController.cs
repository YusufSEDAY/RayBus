using Microsoft.AspNetCore.Mvc;
using RayBus.Models.DTOs;
using RayBus.Services;

namespace RayBus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TripController : ControllerBase
    {
        private readonly ITripService _tripService;
        private readonly ILogger<TripController> _logger;

        public TripController(ITripService tripService, ILogger<TripController> logger)
        {
            _tripService = tripService;
            _logger = logger;
        }

        /// <summary>
        /// Sefer detayını getirir (koltuk bilgileri ile)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTripDetail(int id)
        {
            var response = await _tripService.GetTripDetailAsync(id);
            
            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Yeni sefer oluşturur (Admin için)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateTrip([FromBody] CreateTripDTO createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _tripService.CreateTripAsync(createDto);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return CreatedAtAction(nameof(GetTripDetail), new { id = response.Data?.TripID }, response);
        }
    }
}

