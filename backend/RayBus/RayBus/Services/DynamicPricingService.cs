using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RayBus.Data;

namespace RayBus.Services
{
    /// <summary>
    /// Dinamik fiyatlandırma servisi - Doluluk oranına göre otomatik zam yapar
    /// </summary>
    public class DynamicPricingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DynamicPricingService> _logger;
        private readonly int _processIntervalHours;

        public DynamicPricingService(
            IServiceProvider serviceProvider,
            ILogger<DynamicPricingService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _processIntervalHours = configuration.GetValue<int>("DynamicPricing:ProcessIntervalHours", 6); // Varsayılan: 6 saatte bir
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("💰 Dinamik fiyatlandırma servisi başlatıldı. İşlem aralığı: {Interval} saat", _processIntervalHours);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessDynamicPricingAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Dinamik fiyatlandırma işlenirken hata");
                }

                await Task.Delay(TimeSpan.FromHours(_processIntervalHours), stoppingToken);
            }
        }

        private async Task ProcessDynamicPricingAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<RayBusDbContext>();
            var connectionString = context.Database.GetConnectionString();

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("⚠️ Veritabanı bağlantı bilgisi bulunamadı");
                return;
            }

            try
            {
                _logger.LogInformation("🔄 Dinamik fiyatlandırma işlemi başlatılıyor...");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                    using var command = new SqlCommand("[proc].sp_Otomatik_Zam_Cursor", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    
                    await command.ExecuteNonQueryAsync();

                _logger.LogInformation("✅ Dinamik fiyatlandırma işlemi tamamlandı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Dinamik fiyatlandırma stored procedure çalıştırılırken hata");
                throw;
            }
        }
    }
}

