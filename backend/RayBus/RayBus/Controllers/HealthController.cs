using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RayBus.Data;

namespace RayBus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly RayBusDbContext _context;
        private readonly ILogger<HealthController> _logger;

        public HealthController(RayBusDbContext context, ILogger<HealthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Veritabanı bağlantısını test eder
        /// </summary>
        [HttpGet("db")]
        public async Task<IActionResult> CheckDatabase()
        {
            try
            {
                // Veritabanı bağlantısını test et
                var canConnect = await _context.Database.CanConnectAsync();
                
                if (!canConnect)
                {
                    return StatusCode(503, new
                    {
                        success = false,
                        message = "Veritabanına bağlanılamıyor",
                        database = "RayBusDB",
                        connectionString = "Server=localhost;Database=RayBusDB;Trusted_Connection=True;TrustServerCertificate=True;"
                    });
                }

                // Basit bir sorgu çalıştır
                var cityCount = await _context.Cities.CountAsync();
                var userCount = await _context.Users.CountAsync();

                return Ok(new
                {
                    success = true,
                    message = "Veritabanı bağlantısı başarılı",
                    database = "RayBusDB",
                    stats = new
                    {
                        cities = cityCount,
                        users = userCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı bağlantı testi başarısız");
                return StatusCode(503, new
                {
                    success = false,
                    message = "Veritabanı bağlantı hatası",
                    error = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// API sağlık kontrolü
        /// </summary>
        [HttpGet]
        public IActionResult Check()
        {
            return Ok(new
            {
                success = true,
                message = "API çalışıyor",
                timestamp = DateTime.UtcNow
            });
        }
    }
}

