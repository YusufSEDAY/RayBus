using Microsoft.AspNetCore.Mvc;
using RayBus.Models.DTOs;
using RayBus.Services;

namespace RayBus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrainController : ControllerBase
    {
        private readonly ITrainService _trainService;
        private readonly ILogger<TrainController> _logger;

        public TrainController(ITrainService trainService, ILogger<TrainController> logger)
        {
            _trainService = trainService;
            _logger = logger;
        }

        /// <summary>
        /// Tüm tren seferlerini getirir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllTrains()
        {
            var response = await _trainService.GetAllTrainsAsync();
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Belirli bir tren seferini getirir
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTrainById(int id)
        {
            var response = await _trainService.GetTrainByIdAsync(id);
            
            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Tarih ve rota bazlı tren seferlerini getirir
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchTrains([FromQuery] string from, [FromQuery] string to, [FromQuery] DateTime date)
        {
            var searchDto = new TrainSearchDTO
            {
                From = from,
                To = to,
                Date = date
            };

            var response = await _trainService.SearchTrainsAsync(searchDto);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}

