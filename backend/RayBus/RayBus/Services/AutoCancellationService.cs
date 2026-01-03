using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RayBus.Data;
using RayBus.Models.DTOs;
using RayBus.Models.Entities;

namespace RayBus.Services
{
    public class AutoCancellationService : IAutoCancellationService
    {
        private readonly RayBusDbContext _context;
        private readonly ILogger<AutoCancellationService> _logger;
        private readonly string _connectionString;

        public AutoCancellationService(
            RayBusDbContext context,
            ILogger<AutoCancellationService> logger)
        {
            _context = context;
            _logger = logger;
            _connectionString = context.Database.GetConnectionString() 
                ?? throw new InvalidOperationException("Connection string not found");
        }

        public async Task<ApiResponse<AutoCancellationResultDTO>> ProcessTimeoutReservationsAsync(int timeoutMinutes = 15)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Zaman_Asimi_Rezervasyonlar", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@TimeoutMinutes", timeoutMinutes);
                command.Parameters.AddWithValue("@MaxCancellations", 100);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var result = new AutoCancellationResultDTO
                    {
                        IptalEdilenSayisi = reader.IsDBNull(reader.GetOrdinal("IptalEdilenSayisi"))
                            ? 0
                            : reader.GetInt32(reader.GetOrdinal("IptalEdilenSayisi")),
                        Durum = reader.IsDBNull(reader.GetOrdinal("Durum"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("Durum")),
                        IslemTarihi = reader.IsDBNull(reader.GetOrdinal("IslemTarihi"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("IslemTarihi"))
                    };

                    _logger.LogInformation("✅ Otomatik iptal işlemi tamamlandı. İptal edilen: {Count}", result.IptalEdilenSayisi);

                    return ApiResponse<AutoCancellationResultDTO>.SuccessResponse(
                        result,
                        $"Otomatik iptal işlemi tamamlandı. {result.IptalEdilenSayisi} rezervasyon iptal edildi."
                    );
                }

                return ApiResponse<AutoCancellationResultDTO>.ErrorResponse("Otomatik iptal işlemi sonuç döndürmedi");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "❌ Otomatik iptal işlemi sırasında SQL hatası");
                return ApiResponse<AutoCancellationResultDTO>.ErrorResponse(
                    $"Veritabanı hatası: {ex.Message}",
                    new List<string> { ex.Message }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Otomatik iptal işlemi sırasında hata");
                return ApiResponse<AutoCancellationResultDTO>.ErrorResponse(
                    "Otomatik iptal işlemi sırasında bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<AutoCancellationSettingsDTO>> GetSettingsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Otomatik_Iptal_Ayarlari", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@IslemTipi", "GET");
                command.Parameters.AddWithValue("@TimeoutMinutes", DBNull.Value);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var settings = new AutoCancellationSettingsDTO
                    {
                        TimeoutMinutes = reader.IsDBNull(reader.GetOrdinal("TimeoutMinutes"))
                            ? 15
                            : reader.GetInt32(reader.GetOrdinal("TimeoutMinutes")),
                        Durum = reader.IsDBNull(reader.GetOrdinal("Durum"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("Durum")),
                        Aciklama = reader.IsDBNull(reader.GetOrdinal("Aciklama"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("Aciklama"))
                    };

                    return ApiResponse<AutoCancellationSettingsDTO>.SuccessResponse(
                        settings,
                        "Ayarlar başarıyla getirildi"
                    );
                }

                return ApiResponse<AutoCancellationSettingsDTO>.ErrorResponse("Ayarlar getirilemedi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ayarlar getirilirken hata");
                return ApiResponse<AutoCancellationSettingsDTO>.ErrorResponse(
                    "Ayarlar getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<AutoCancellationSettingsDTO>> UpdateSettingsAsync(int timeoutMinutes)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Otomatik_Iptal_Ayarlari", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@IslemTipi", "SET");
                command.Parameters.AddWithValue("@TimeoutMinutes", timeoutMinutes);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var settings = new AutoCancellationSettingsDTO
                    {
                        TimeoutMinutes = reader.IsDBNull(reader.GetOrdinal("TimeoutMinutes"))
                            ? timeoutMinutes
                            : reader.GetInt32(reader.GetOrdinal("TimeoutMinutes")),
                        Durum = reader.IsDBNull(reader.GetOrdinal("Durum"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("Durum")),
                        Aciklama = reader.IsDBNull(reader.GetOrdinal("Aciklama"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("Aciklama"))
                    };

                    return ApiResponse<AutoCancellationSettingsDTO>.SuccessResponse(
                        settings,
                        "Ayarlar başarıyla güncellendi"
                    );
                }

                return ApiResponse<AutoCancellationSettingsDTO>.ErrorResponse("Ayarlar güncellenemedi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ayarlar güncellenirken hata");
                return ApiResponse<AutoCancellationSettingsDTO>.ErrorResponse(
                    "Ayarlar güncellenirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<IEnumerable<AutoCancellationLogDTO>>> GetCancellationLogsAsync(int? userId = null)
        {
            try
            {
                var query = _context.AutoCancellationLogs
                    .Include(log => log.User)
                    .Include(log => log.Reservation)
                    .AsQueryable();

                if (userId.HasValue)
                {
                    query = query.Where(log => log.UserID == userId.Value);
                }

                var logs = await query
                    .OrderByDescending(log => log.CancelledAt)
                    .Take(100)
                    .ToListAsync();

                var logDtos = logs.Select(log => 
                {
                    var userName = log.User?.FullName ?? "Bilinmiyor";
                    if (string.IsNullOrEmpty(userName) || userName == "Bilinmiyor")
                    {
                        _logger.LogWarning("⚠️ Log {LogID} için kullanıcı bilgisi bulunamadı. UserID: {UserID}", log.LogID, log.UserID);
                    }
                    
                    return new AutoCancellationLogDTO
                    {
                        LogID = log.LogID,
                        ReservationID = log.ReservationID,
                        UserID = log.UserID,
                        UserName = userName,
                        CancelledAt = log.CancelledAt,
                        Reason = log.Reason,
                        OriginalReservationDate = log.OriginalReservationDate,
                        TimeoutMinutes = log.TimeoutMinutes
                    };
                }).ToList();
                
                _logger.LogInformation("📋 {Count} adet iptal logu getirildi", logDtos.Count);

                return ApiResponse<IEnumerable<AutoCancellationLogDTO>>.SuccessResponse(
                    logDtos,
                    "İptal logları başarıyla getirildi"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ İptal logları getirilirken hata");
                return ApiResponse<IEnumerable<AutoCancellationLogDTO>>.ErrorResponse(
                    "İptal logları getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }
    }
}

