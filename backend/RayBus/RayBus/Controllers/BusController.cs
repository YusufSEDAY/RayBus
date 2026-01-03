using Microsoft.AspNetCore.Mvc;
using RayBus.Models.DTOs;
using RayBus.Services;

namespace RayBus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusController : ControllerBase
    {
        private readonly IBusService _busService;
        private readonly ILogger<BusController> _logger;

        public BusController(IBusService busService, ILogger<BusController> logger)
        {
            _busService = busService;
            _logger = logger;
        }

        /// <summary>
        /// Tüm otobüs seferlerini getirir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllBuses()
        {
            var response = await _busService.GetAllBusesAsync();
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Belirli bir otobüs seferini getirir
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBusById(int id)
        {
            var response = await _busService.GetBusByIdAsync(id);
            
            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Tarih ve rota bazlı otobüs seferlerini getirir
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchBuses([FromQuery] string from, [FromQuery] string to, [FromQuery] DateTime date)
        {
            var searchDto = new BusSearchDTO
            {
                From = from,
                To = to,
                Date = date
            };

            var response = await _busService.SearchBusesAsync(searchDto);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}

