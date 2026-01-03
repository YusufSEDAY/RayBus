using Microsoft.AspNetCore.Mvc;
using RayBus.Services;

namespace RayBus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CityController : ControllerBase
    {
        private readonly ICityService _cityService;
        private readonly ILogger<CityController> _logger;

        public CityController(ICityService cityService, ILogger<CityController> logger)
        {
            _cityService = cityService;
            _logger = logger;
        }

        /// <summary>
        /// Tüm şehirleri getirir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllCities()
        {
            try
            {
                _logger.LogInformation("GetAllCities endpoint çağrıldı");
                var response = await _cityService.GetAllCitiesAsync();
                
                _logger.LogInformation($"GetAllCities yanıtı - Success: {response.Success}, Data Count: {response.Data?.Count() ?? 0}");
                
                if (!response.Success)
                {
                    _logger.LogWarning($"GetAllCities başarısız: {response.Message}");
                    return BadRequest(response);
                }

                _logger.LogInformation($"GetAllCities başarılı - {response.Data?.Count() ?? 0} şehir döndürülüyor");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllCities endpoint'inde hata oluştu");
                return StatusCode(500, new { success = false, message = "Sunucu hatası", errors = new[] { ex.Message } });
            }
        }

        /// <summary>
        /// Belirli bir şehre ait istasyonları getirir
        /// </summary>
        [HttpGet("{cityId}/stations")]
        public async Task<IActionResult> GetStationsByCity(int cityId)
        {
            var response = await _cityService.GetStationsByCityIdAsync(cityId);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Belirli bir şehre ait terminalleri getirir
        /// </summary>
        [HttpGet("{cityId}/terminals")]
        public async Task<IActionResult> GetTerminalsByCity(int cityId)
        {
            var response = await _cityService.GetTerminalsByCityIdAsync(cityId);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}

