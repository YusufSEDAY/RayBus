using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RayBus.Data;
using RayBus.Models.DTOs;
using System.Text.Json;

namespace RayBus.Services
{
    public class UserStatisticsService : IUserStatisticsService
    {
        private readonly RayBusDbContext _context;
        private readonly ILogger<UserStatisticsService> _logger;
        private readonly string _connectionString;

        public UserStatisticsService(
            RayBusDbContext context,
            ILogger<UserStatisticsService> logger)
        {
            _context = context;
            _logger = logger;
            _connectionString = context.Database.GetConnectionString() 
                ?? throw new InvalidOperationException("Connection string not found");
        }

        public async Task<ApiResponse<UserStatisticsDTO>> GetUserStatisticsAsync(int userId)
        {
            try
            {
                _logger.LogInformation("📊 Kullanıcı istatistikleri getiriliyor. UserID: {UserID}", userId);

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(
                    "SELECT * FROM report.vw_Kullanici_Istatistikleri WHERE UserID = @UserID",
                    connection);

                command.Parameters.AddWithValue("@UserID", userId);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var stats = new UserStatisticsDTO
                    {
                        UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                        KullaniciAdi = reader.IsDBNull(reader.GetOrdinal("KullaniciAdi"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("KullaniciAdi")),
                        KullaniciEmail = reader.IsDBNull(reader.GetOrdinal("KullaniciEmail"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("KullaniciEmail")),
                        ToplamHarcama = reader.IsDBNull(reader.GetOrdinal("ToplamHarcama"))
                            ? 0
                            : reader.GetDecimal(reader.GetOrdinal("ToplamHarcama")),
                        ToplamSeyahatSayisi = reader.IsDBNull(reader.GetOrdinal("ToplamSeyahatSayisi"))
                            ? 0
                            : reader.GetInt32(reader.GetOrdinal("ToplamSeyahatSayisi")),
                        GelecekSeyahatSayisi = reader.IsDBNull(reader.GetOrdinal("GelecekSeyahatSayisi"))
                            ? 0
                            : reader.GetInt32(reader.GetOrdinal("GelecekSeyahatSayisi")),
                        GecmisSeyahatSayisi = reader.IsDBNull(reader.GetOrdinal("GecmisSeyahatSayisi"))
                            ? 0
                            : reader.GetInt32(reader.GetOrdinal("GecmisSeyahatSayisi")),
                        OrtalamaSeyahatFiyati = reader.IsDBNull(reader.GetOrdinal("OrtalamaSeyahatFiyati"))
                            ? 0
                            : reader.GetDecimal(reader.GetOrdinal("OrtalamaSeyahatFiyati")),
                        EnCokGidilenSehir = reader.IsDBNull(reader.GetOrdinal("EnCokGidilenSehir"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("EnCokGidilenSehir")),
                        SonSeyahatTarihi = reader.IsDBNull(reader.GetOrdinal("SonSeyahatTarihi"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("SonSeyahatTarihi")),
                        ToplamRezervasyonSayisi = reader.IsDBNull(reader.GetOrdinal("ToplamRezervasyonSayisi"))
                            ? 0
                            : reader.GetInt32(reader.GetOrdinal("ToplamRezervasyonSayisi")),
                        IptalEdilenRezervasyonSayisi = reader.IsDBNull(reader.GetOrdinal("IptalEdilenRezervasyonSayisi"))
                            ? 0
                            : reader.GetInt32(reader.GetOrdinal("IptalEdilenRezervasyonSayisi")),
                        KayitTarihi = reader.IsDBNull(reader.GetOrdinal("KayitTarihi"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("KayitTarihi"))
                    };

                    _logger.LogInformation("✅ View'den alınan istatistikler: ToplamHarcama={ToplamHarcama}, ToplamSeyahat={ToplamSeyahat}, ToplamRezervasyon={ToplamRezervasyon}",
                        stats.ToplamHarcama, stats.ToplamSeyahatSayisi, stats.ToplamRezervasyonSayisi);

                    if ((stats.ToplamHarcama == 0 || stats.OrtalamaSeyahatFiyati == 0) && stats.ToplamRezervasyonSayisi > 0)
                    {
                        _logger.LogInformation("⚠️ View'den gelen istatistikler 0 veya eksik, ancak rezervasyon var. Stored procedure fallback mekanizması devreye giriyor...");
                        
                        using var spConnection = new SqlConnection(_connectionString);
                        await spConnection.OpenAsync();

                        using var spCommand = new SqlCommand("[proc].sp_Kullanici_Istatistikleri_Getir", spConnection)
                        {
                            CommandType = System.Data.CommandType.StoredProcedure
                        };
                        spCommand.Parameters.AddWithValue("@UserID", userId);

                        using var spReader = await spCommand.ExecuteReaderAsync();
                        if (await spReader.ReadAsync())
                        {
                            stats.ToplamHarcama = spReader.IsDBNull(spReader.GetOrdinal("ToplamHarcama"))
                                ? 0
                                : spReader.GetDecimal(spReader.GetOrdinal("ToplamHarcama"));
                            stats.OrtalamaSeyahatFiyati = spReader.IsDBNull(spReader.GetOrdinal("OrtalamaSeyahatFiyati"))
                                ? 0
                                : spReader.GetDecimal(spReader.GetOrdinal("OrtalamaSeyahatFiyati"));
                            stats.ToplamSeyahatSayisi = spReader.IsDBNull(spReader.GetOrdinal("ToplamSeyahatSayisi"))
                                ? 0
                                : spReader.GetInt32(spReader.GetOrdinal("ToplamSeyahatSayisi"));
                            stats.GelecekSeyahatSayisi = spReader.IsDBNull(spReader.GetOrdinal("GelecekSeyahatSayisi"))
                                ? 0
                                : spReader.GetInt32(spReader.GetOrdinal("GelecekSeyahatSayisi"));
                            stats.GecmisSeyahatSayisi = spReader.IsDBNull(spReader.GetOrdinal("GecmisSeyahatSayisi"))
                                ? 0
                                : spReader.GetInt32(spReader.GetOrdinal("GecmisSeyahatSayisi"));
                            
                            _logger.LogInformation("✅ Stored procedure ile hesaplanan istatistikler: ToplamHarcama={ToplamHarcama}, ToplamSeyahat={ToplamSeyahat}, GelecekSeyahat={GelecekSeyahat}, GecmisSeyahat={GecmisSeyahat}, OrtalamaFiyat={OrtalamaFiyat}",
                                stats.ToplamHarcama, stats.ToplamSeyahatSayisi, stats.GelecekSeyahatSayisi, stats.GecmisSeyahatSayisi, stats.OrtalamaSeyahatFiyati);
                        }
                    }

                    _logger.LogInformation("📤 Response gönderiliyor: UserID={UserID}, ToplamHarcama={ToplamHarcama}, ToplamSeyahat={ToplamSeyahat}, ToplamRezervasyon={ToplamRezervasyon}",
                        stats.UserID, stats.ToplamHarcama, stats.ToplamSeyahatSayisi, stats.ToplamRezervasyonSayisi);

                    var response = ApiResponse<UserStatisticsDTO>.SuccessResponse(
                        stats,
                        "İstatistikler başarıyla getirildi"
                    );

                    _logger.LogInformation("📤 Response JSON: {Json}", JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));

                    return response;
                }

                // Eğer view'den veri gelmediyse, boş istatistik döndür
                var emptyStats = new UserStatisticsDTO
                {
                    UserID = userId,
                    ToplamHarcama = 0,
                    ToplamSeyahatSayisi = 0,
                    GelecekSeyahatSayisi = 0,
                    GecmisSeyahatSayisi = 0,
                    OrtalamaSeyahatFiyati = 0,
                    ToplamRezervasyonSayisi = 0,
                    IptalEdilenRezervasyonSayisi = 0
                };

                return ApiResponse<UserStatisticsDTO>.SuccessResponse(
                    emptyStats,
                    "Kullanıcı için henüz istatistik bulunmuyor"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Kullanıcı istatistikleri getirilirken hata. UserID: {UserID}", userId);
                return ApiResponse<UserStatisticsDTO>.ErrorResponse(
                    "İstatistikler getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<object>> GetUserReportAsync(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Kullanici_Raporu", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@UserID", userId);

                using var reader = await command.ExecuteReaderAsync();

                var generalStats = new UserReportGeneralDTO();
                if (await reader.ReadAsync())
                {
                    generalStats.RaporTipi = reader.IsDBNull(reader.GetOrdinal("RaporTipi"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("RaporTipi"));
                    generalStats.KullaniciAdi = reader.IsDBNull(reader.GetOrdinal("KullaniciAdi"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("KullaniciAdi"));
                    generalStats.ToplamHarcama = reader.IsDBNull(reader.GetOrdinal("ToplamHarcama"))
                        ? 0
                        : reader.GetDecimal(reader.GetOrdinal("ToplamHarcama"));
                    generalStats.ToplamSeyahatSayisi = reader.IsDBNull(reader.GetOrdinal("ToplamSeyahatSayisi"))
                        ? 0
                        : reader.GetInt32(reader.GetOrdinal("ToplamSeyahatSayisi"));
                    generalStats.GelecekSeyahatSayisi = reader.IsDBNull(reader.GetOrdinal("GelecekSeyahatSayisi"))
                        ? 0
                        : reader.GetInt32(reader.GetOrdinal("GelecekSeyahatSayisi"));
                    generalStats.GecmisSeyahatSayisi = reader.IsDBNull(reader.GetOrdinal("GecmisSeyahatSayisi"))
                        ? 0
                        : reader.GetInt32(reader.GetOrdinal("GecmisSeyahatSayisi"));
                    generalStats.OrtalamaSeyahatFiyati = reader.IsDBNull(reader.GetOrdinal("OrtalamaSeyahatFiyati"))
                        ? 0
                        : reader.GetDecimal(reader.GetOrdinal("OrtalamaSeyahatFiyati"));
                    generalStats.EnCokGidilenSehir = reader.IsDBNull(reader.GetOrdinal("EnCokGidilenSehir"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("EnCokGidilenSehir"));
                    generalStats.SonSeyahatTarihi = reader.IsDBNull(reader.GetOrdinal("SonSeyahatTarihi"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("SonSeyahatTarihi"));
                    generalStats.ToplamRezervasyonSayisi = reader.IsDBNull(reader.GetOrdinal("ToplamRezervasyonSayisi"))
                        ? 0
                        : reader.GetInt32(reader.GetOrdinal("ToplamRezervasyonSayisi"));
                    generalStats.IptalEdilenRezervasyonSayisi = reader.IsDBNull(reader.GetOrdinal("IptalEdilenRezervasyonSayisi"))
                        ? 0
                        : reader.GetInt32(reader.GetOrdinal("IptalEdilenRezervasyonSayisi"));
                }

                var trips = new List<UserReportTripDTO>();
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        trips.Add(new UserReportTripDTO
                        {
                            ReservationID = reader.GetInt32(reader.GetOrdinal("ReservationID")),
                            SeferTarihi = reader.GetDateTime(reader.GetOrdinal("SeferTarihi")),
                            SeferSaati = reader.IsDBNull(reader.GetOrdinal("SeferSaati"))
                                ? TimeSpan.Zero
                                : TimeSpan.Parse(reader.GetString(reader.GetOrdinal("SeferSaati"))),
                            KalkisSehri = reader.IsDBNull(reader.GetOrdinal("KalkisSehri"))
                                ? string.Empty
                                : reader.GetString(reader.GetOrdinal("KalkisSehri")),
                            VarisSehri = reader.IsDBNull(reader.GetOrdinal("VarisSehri"))
                                ? string.Empty
                                : reader.GetString(reader.GetOrdinal("VarisSehri")),
                            OdenenTutar = reader.IsDBNull(reader.GetOrdinal("OdenenTutar"))
                                ? 0
                                : reader.GetDecimal(reader.GetOrdinal("OdenenTutar")),
                            RezervasyonDurumu = reader.IsDBNull(reader.GetOrdinal("RezervasyonDurumu"))
                                ? string.Empty
                                : reader.GetString(reader.GetOrdinal("RezervasyonDurumu")),
                            RezervasyonTarihi = reader.GetDateTime(reader.GetOrdinal("RezervasyonTarihi"))
                        });
                    }
                }

                var monthlyStats = new List<UserReportMonthlyDTO>();
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        monthlyStats.Add(new UserReportMonthlyDTO
                        {
                            Yil = reader.GetInt32(reader.GetOrdinal("Yil")),
                            Ay = reader.GetInt32(reader.GetOrdinal("Ay")),
                            AylikHarcama = reader.IsDBNull(reader.GetOrdinal("AylikHarcama"))
                                ? 0
                                : reader.GetDecimal(reader.GetOrdinal("AylikHarcama")),
                            AylikSeyahatSayisi = reader.IsDBNull(reader.GetOrdinal("AylikSeyahatSayisi"))
                                ? 0
                                : reader.GetInt32(reader.GetOrdinal("AylikSeyahatSayisi"))
                        });
                    }
                }

                var report = new
                {
                    General = generalStats,
                    RecentTrips = trips,
                    MonthlyStats = monthlyStats
                };

                return ApiResponse<object>.SuccessResponse(
                    report,
                    "Kullanıcı raporu başarıyla getirildi"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Kullanıcı raporu getirilirken hata. UserID: {UserID}", userId);
                return ApiResponse<object>.ErrorResponse(
                    "Kullanıcı raporu getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }
    }
}

