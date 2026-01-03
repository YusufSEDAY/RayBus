using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RayBus.Attributes;
using RayBus.Data;
using RayBus.Models.DTOs;
using RayBus.Services;

namespace RayBus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyController : ControllerBase
    {
        private readonly RayBusDbContext _context;
        private readonly ITripService _tripService;
        private readonly ILogger<CompanyController> _logger;

        public CompanyController(
            RayBusDbContext context,
            ITripService tripService,
            ILogger<CompanyController> logger)
        {
            _context = context;
            _tripService = tripService;
            _logger = logger;
        }

        private async Task<int> GetCompanyIDAsync()
        {
            if (Request.Query.ContainsKey("sirketID") && int.TryParse(Request.Query["sirketID"], out int querySirketID))
            {
                _logger.LogInformation("🔍 Query parameter'dan şirket ID alındı: {SirketID}", querySirketID);
                return querySirketID;
            }

            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                           ?? User?.FindFirst("sub")?.Value 
                           ?? User?.FindFirst("userId")?.Value;
            
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserID == userId && u.Status == 1);
                
                if (user != null && user.Role != null && user.Role.RoleName == "Şirket")
                {
                    _logger.LogInformation("🔍 JWT Token'dan şirket ID alındı. Şirket ID: {SirketID}, Şirket Adı: {CompanyName}", 
                        user.UserID, user.FullName);
                    return user.UserID;
                }
            }

            if (Request.Query.ContainsKey("sirketAdi") && !string.IsNullOrEmpty(Request.Query["sirketAdi"]))
            {
                var companyName = Request.Query["sirketAdi"].ToString();
                var companyRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName == "Şirket");
                
                if (companyRole != null)
                {
                    var company = await _context.Users
                        .Where(u => u.RoleID == companyRole.RoleID 
                                 && u.Status == 1 
                                 && (u.FullName.Contains(companyName) || companyName.Contains(u.FullName)))
                        .FirstOrDefaultAsync();
                    
                    if (company != null)
                    {
                        _logger.LogInformation("🔍 Şirket adına göre şirket bulundu. Şirket ID: {SirketID}, Şirket Adı: {CompanyName}", 
                            company.UserID, company.FullName);
                        return company.UserID;
                    }
                }
            }

            var testCompanyRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "Şirket");
            
            if (testCompanyRole != null)
            {
                var testCompany = await _context.Users
                    .Where(u => u.RoleID == testCompanyRole.RoleID 
                             && u.Status == 1 
                             && (u.FullName.ToLower().Contains("test") || u.FullName.ToLower().Contains("testsirket")))
                    .OrderBy(u => u.UserID)
                    .FirstOrDefaultAsync();
                
                if (testCompany != null)
                {
                    _logger.LogInformation("🔍 'test' içeren şirket bulundu. Şirket ID: {SirketID}, Şirket Adı: {CompanyName}", 
                        testCompany.UserID, testCompany.FullName);
                    return testCompany.UserID;
                }
            }

            if (testCompanyRole != null)
            {
                var firstCompany = await _context.Users
                    .Where(u => u.RoleID == testCompanyRole.RoleID && u.Status == 1)
                    .OrderBy(u => u.UserID)
                    .FirstOrDefaultAsync();
                
                if (firstCompany != null)
                {
                    _logger.LogWarning("⚠️ JWT token veya şirket adı bulunamadı, ilk şirket kullanılıyor. Şirket ID: {SirketID}, Şirket Adı: {CompanyName}", 
                        firstCompany.UserID, firstCompany.FullName);
                    return firstCompany.UserID;
                }
            }

            var vehicleWithCompany = await _context.Vehicles
                .Where(v => v.CompanyID != null)
                .Select(v => v.CompanyID)
                .Distinct()
                .FirstOrDefaultAsync();
            
            if (vehicleWithCompany.HasValue)
            {
                _logger.LogInformation("🔍 Şirket kullanıcısı bulunamadı, araçlardan şirket ID bulundu: {SirketID}", vehicleWithCompany.Value);
                return vehicleWithCompany.Value;
            }

            _logger.LogWarning("⚠️ Hiç şirket kullanıcısı veya araç bulunamadı. Varsayılan olarak 2 kullanılıyor.");
            return 2; // Son çare olarak varsayılan değer
        }

        /// <summary>
        /// Şirkete ait seferleri getirir (Stored Procedure ile - dolu koltuk sayısı ve kapasite ile)
        /// </summary>
        [HttpGet("trips")]
        public async Task<IActionResult> GetMyTrips()
        {
            try
            {
                int sirketID = await GetCompanyIDAsync();
                _logger.LogInformation("🔍 Şirket seferleri getiriliyor. SirketID: {SirketID}", sirketID);

                var directTrips = await _context.Trips
                    .Include(t => t.Vehicle)
                    .Include(t => t.FromCity)
                    .Include(t => t.ToCity)
                    .Where(t => t.Vehicle != null && t.Vehicle.CompanyID == sirketID)
                    .OrderByDescending(t => t.DepartureDate)
                    .ThenByDescending(t => t.DepartureTime)
                    .ToListAsync();

                _logger.LogInformation("🔍 Entity Framework ile direkt sorgu: {Count} sefer bulundu", directTrips.Count);
                
                if (directTrips.Any())
                {
                    _logger.LogInformation("🔍 Direkt sorgu sefer örnekleri: {Trips}", 
                        string.Join(", ", directTrips.Take(5).Select(t => 
                            $"TripID:{t.TripID} VehicleID:{t.VehicleID} VehicleCompanyID:{t.Vehicle?.CompanyID} Status:{t.Status}")));
                }

                var connectionString = _context.Database.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("❌ Veritabanı bağlantı string'i boş");
                    return BadRequest(new ApiResponse<IEnumerable<CompanyTripDTO>>
                    {
                        Success = false,
                        Message = "Veritabanı bağlantı hatası"
                    });
                }

                var trips = new List<CompanyTripDTO>();

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("SELECT * FROM report.vw_Sirket_Seferleri WHERE CompanyID = @SirketID ORDER BY Tarih DESC, Saat DESC", connection);
                command.Parameters.AddWithValue("@SirketID", sirketID);

                _logger.LogInformation("🔍 Stored procedure çağrılıyor: [proc].sp_Sirket_Seferleri_Getir, @SirketID = {SirketID}", sirketID);

                using var reader = await command.ExecuteReaderAsync();
                
                int rowCount = 0;

                while (await reader.ReadAsync())
                {
                    rowCount++;
                    var trip = new CompanyTripDTO
                    {
                        TripID = reader.GetInt32(reader.GetOrdinal("TripID")),
                        AracPlaka = reader.IsDBNull(reader.GetOrdinal("AracPlaka"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("AracPlaka")),
                        Guzergah = reader.IsDBNull(reader.GetOrdinal("Guzergah"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Guzergah")),
                        Tarih = reader.IsDBNull(reader.GetOrdinal("Tarih"))
                            ? (DateTime?)null
                            : reader.GetDateTime(reader.GetOrdinal("Tarih")),
                        Saat = reader.IsDBNull(reader.GetOrdinal("Saat"))
                            ? (TimeSpan?)null
                            : reader.GetTimeSpan(reader.GetOrdinal("Saat")),
                        Price = reader.IsDBNull(reader.GetOrdinal("Fiyat"))
                            ? 0
                            : reader.GetDecimal(reader.GetOrdinal("Fiyat")),
                        Durum = reader.IsDBNull(reader.GetOrdinal("Durum"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Durum")),
                        DoluKoltukSayisi = reader.IsDBNull(reader.GetOrdinal("DoluKoltukSayisi"))
                            ? 0
                            : reader.GetInt32(reader.GetOrdinal("DoluKoltukSayisi")),
                        ToplamKoltuk = reader.IsDBNull(reader.GetOrdinal("ToplamKoltuk"))
                            ? 0
                            : reader.GetInt32(reader.GetOrdinal("ToplamKoltuk"))
                    };

                    if (trip.Tarih.HasValue)
                    {
                        trip.DepartureDate = trip.Tarih.Value;
                    }
                    if (trip.Saat.HasValue)
                    {
                        trip.DepartureTime = trip.Saat.Value;
                    }

                    trip.Status = trip.Durum == "Aktif" ? (byte)1 : (byte)0;

                    trips.Add(trip);
                }

                _logger.LogInformation("✅ Stored procedure tamamlandı. {RowCount} sefer bulundu.", rowCount);
                
                if (rowCount < directTrips.Count)
                {
                    _logger.LogWarning("⚠️ Stored procedure {StoredCount} sefer döndü ama Entity Framework {DirectCount} sefer buldu. Direkt sorgu kullanılıyor.", 
                        rowCount, directTrips.Count);
                    
                    var tripIds = directTrips.Select(t => t.TripID).ToList();
                    var vehicleIds = directTrips.Where(t => t.Vehicle != null).Select(t => t.Vehicle!.VehicleID).Distinct().ToList();
                    
                    var tripSeatCounts = await _context.TripSeats
                        .Where(ts => tripIds.Contains(ts.TripID) && ts.IsReserved)
                        .GroupBy(ts => ts.TripID)
                        .Select(g => new { TripID = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.TripID, x => x.Count);
                    
                    var vehicleSeatCounts = await _context.Seats
                        .Where(s => vehicleIds.Contains(s.VehicleID))
                        .GroupBy(s => s.VehicleID)
                        .Select(g => new { VehicleID = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.VehicleID, x => x.Count);
                    
                    trips = directTrips.Select(t => new CompanyTripDTO
                    {
                        TripID = t.TripID,
                        AracPlaka = t.Vehicle?.PlateOrCode ?? "",
                        Guzergah = $"{t.FromCity?.CityName ?? ""} > {t.ToCity?.CityName ?? ""}",
                        Tarih = t.DepartureDate,
                        Saat = t.DepartureTime,
                        Price = t.Price,
                        Durum = t.Status == 1 ? "Aktif" : "İptal",
                        DepartureDate = t.DepartureDate,
                        DepartureTime = t.DepartureTime,
                        Status = t.Status,
                        DoluKoltukSayisi = tripSeatCounts.GetValueOrDefault(t.TripID, 0),
                        ToplamKoltuk = t.Vehicle != null ? vehicleSeatCounts.GetValueOrDefault(t.Vehicle.VehicleID, 0) : 0
                    }).ToList();
                    
                    rowCount = trips.Count;
                }
                
                if (rowCount == 0)
                {
                    var vehicleCount = await _context.Vehicles
                        .Where(v => v.CompanyID == sirketID)
                        .CountAsync();
                    
                    var totalTrips = await _context.Trips
                        .Include(t => t.Vehicle)
                        .Where(t => t.Vehicle != null && t.Vehicle.CompanyID == sirketID)
                        .CountAsync();
                    
                    var companyVehicles = await _context.Vehicles
                        .Where(v => v.CompanyID == sirketID)
                        .Select(v => new { v.VehicleID, v.PlateOrCode, v.VehicleType, v.CompanyID })
                        .ToListAsync();
                    
                    _logger.LogWarning("⚠️ Şirket ID {SirketID} için sefer bulunamadı. Araç sayısı: {VehicleCount}, Toplam aktif sefer sayısı: {TotalTrips}", 
                        sirketID, vehicleCount, totalTrips);
                    
                    if (vehicleCount > 0 && totalTrips == 0)
                    {
                        _logger.LogInformation("ℹ️ Şirket ID {SirketID} için {VehicleCount} araç var ama henüz sefer eklenmemiş.", sirketID, vehicleCount);
                    }
                    else if (vehicleCount == 0)
                    {
                        _logger.LogWarning("⚠️ Şirket ID {SirketID} için hiç araç bulunamadı. Araçların CompanyID değerini kontrol edin.", sirketID);
                    }
                }

                return Ok(new ApiResponse<IEnumerable<CompanyTripDTO>>
                {
                    Success = true,
                    Message = trips.Count > 0 
                        ? $"Seferler başarıyla getirildi ({trips.Count} adet)" 
                        : "Sefer bulunamadı. Lütfen yeni sefer ekleyin.",
                    Data = trips
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Seferler getirilirken SQL hatası oluştu");
                return BadRequest(new ApiResponse<IEnumerable<CompanyTripDTO>>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Seferler getirilirken hata oluştu");
                return BadRequest(new ApiResponse<IEnumerable<CompanyTripDTO>>
                {
                    Success = false,
                    Message = "Seferler getirilirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Yeni sefer oluşturur (Stored Procedure ile - güvenlik ve çakışma kontrolü ile)
        /// </summary>
        [HttpPost("trips")]
        public async Task<IActionResult> CreateTrip([FromBody] CreateTripDTO createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Şirket ID'sini dinamik olarak bul
                int sirketID = await GetCompanyIDAsync();

                // Validasyon
                if (createDto.FromCityID <= 0 || createDto.ToCityID <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Geçerli şehir ID'leri gereklidir"
                    });
                }

                if (createDto.FromCityID == createDto.ToCityID)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Kalkış ve varış şehirleri aynı olamaz"
                    });
                }

                if (createDto.VehicleID <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Geçerli araç ID'si gereklidir"
                    });
                }

                if (createDto.Price <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Fiyat 0'dan büyük olmalıdır"
                    });
                }

                var connectionString = _context.Database.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Veritabanı bağlantı hatası"
                    });
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Sirket_Sefer_Ekle", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@SirketID", sirketID);
                command.Parameters.AddWithValue("@NeredenID", createDto.FromCityID);
                command.Parameters.AddWithValue("@NereyeID", createDto.ToCityID);
                command.Parameters.AddWithValue("@AracID", createDto.VehicleID);
                command.Parameters.AddWithValue("@Tarih", createDto.DepartureDate.Date);
                command.Parameters.AddWithValue("@Saat", createDto.DepartureTime);
                command.Parameters.AddWithValue("@Fiyat", createDto.Price);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var mesaj = reader.IsDBNull(reader.GetOrdinal("Mesaj"))
                        ? "Sefer başarıyla oluşturuldu"
                        : reader.GetString(reader.GetOrdinal("Mesaj"));

                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = mesaj
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Sefer başarıyla oluşturuldu"
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Sefer eklenirken SQL hatası oluştu");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sefer eklenirken hata oluştu");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Sefer eklenirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Belirli bir seferi getirir
        /// </summary>
        [HttpGet("trips/{id}")]
        public async Task<IActionResult> GetTripById(int id)
        {
            try
            {
                var trip = await _context.Trips
                    .Include(t => t.Vehicle)
                    .Include(t => t.FromCity)
                    .Include(t => t.ToCity)
                    .Include(t => t.DepartureTerminal)
                    .Include(t => t.ArrivalTerminal)
                    .Include(t => t.DepartureStation)
                    .Include(t => t.ArrivalStation)
                    .Where(t => t.TripID == id)
                    .Select(t => new CompanyTripDTO
                    {
                        TripID = t.TripID,
                        VehicleID = t.VehicleID,
                        VehicleType = t.Vehicle!.VehicleType,
                        FromCityID = t.FromCityID,
                        FromCity = t.FromCity!.CityName,
                        ToCityID = t.ToCityID,
                        ToCity = t.ToCity!.CityName,
                        DepartureTerminalID = t.DepartureTerminalID,
                        DepartureTerminal = t.DepartureTerminal != null ? t.DepartureTerminal.TerminalName : null,
                        ArrivalTerminalID = t.ArrivalTerminalID,
                        ArrivalTerminal = t.ArrivalTerminal != null ? t.ArrivalTerminal.TerminalName : null,
                        DepartureStationID = t.DepartureStationID,
                        DepartureStation = t.DepartureStation != null ? t.DepartureStation.StationName : null,
                        ArrivalStationID = t.ArrivalStationID,
                        ArrivalStation = t.ArrivalStation != null ? t.ArrivalStation.StationName : null,
                        DepartureDate = t.DepartureDate,
                        DepartureTime = t.DepartureTime,
                        ArrivalDate = t.ArrivalDate,
                        ArrivalTime = t.ArrivalTime,
                        Price = t.Price,
                        Status = t.Status
                    })
                    .FirstOrDefaultAsync();

                if (trip == null)
                {
                    return NotFound(new ApiResponse<CompanyTripDTO>
                    {
                        Success = false,
                        Message = "Sefer bulunamadı"
                    });
                }

                return Ok(new ApiResponse<CompanyTripDTO>
                {
                    Success = true,
                    Message = "Sefer başarıyla getirildi",
                    Data = trip
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sefer getirilirken hata oluştu");
                return BadRequest(new ApiResponse<CompanyTripDTO>
                {
                    Success = false,
                    Message = "Sefer getirilirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Seferi günceller (Stored Procedure ile - sadece fiyat, tarih ve saat)
        /// </summary>
        [HttpPut("trips/{id}")]
        public async Task<IActionResult> UpdateTrip(int id, [FromBody] CreateTripDTO updateDto)
        {
            try
            {
                var sirketID = await GetCompanyIDAsync();
                var connectionString = _context.Database.GetConnectionString() 
                    ?? throw new InvalidOperationException("Connection string not found");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Sirket_Sefer_Guncelle", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@SirketID", sirketID);
                command.Parameters.AddWithValue("@SeferID", id);
                command.Parameters.AddWithValue("@Fiyat", updateDto.Price);
                command.Parameters.AddWithValue("@Tarih", updateDto.DepartureDate.Date);
                command.Parameters.AddWithValue("@Saat", updateDto.DepartureTime);

                _logger.LogInformation("🔍 Stored procedure çağrılıyor: [proc].sp_Sirket_Sefer_Guncelle, @SirketID = {SirketID}, @SeferID = {SeferID}", 
                    sirketID, id);

                using var reader = await command.ExecuteReaderAsync();
                
                string? mesaj = null;
                if (await reader.ReadAsync())
                {
                    mesaj = reader["Mesaj"]?.ToString();
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = mesaj ?? "Sefer başarıyla güncellendi"
                });
            }
            catch (SqlException sqlEx) when (sqlEx.Number == 50001)
            {
                _logger.LogWarning("⚠️ Yetki hatası: {Message}", sqlEx.Message);
                return Forbid($"Bu sefere müdahale yetkiniz yok: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sefer güncellenirken hata oluştu");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Sefer güncellenirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Seferi iptal eder (Stored Procedure ile - güvenlik ve geçmiş sefer kontrolü ile)
        /// </summary>
        [HttpPost("trips/{id}/cancel")]
        public async Task<IActionResult> CancelTrip(int id, [FromBody] CancelCompanyTripDTO? cancelDto = null)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Geçerli sefer ID'si gereklidir"
                    });
                }

                // Şirket ID'sini dinamik olarak bul
                int sirketID = await GetCompanyIDAsync();

                // Stored Procedure çağır
                var connectionString = _context.Database.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Veritabanı bağlantı hatası"
                    });
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Sirket_Sefer_Iptal", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@SirketID", sirketID);
                command.Parameters.AddWithValue("@SeferID", id);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var mesaj = reader.IsDBNull(reader.GetOrdinal("Mesaj"))
                        ? "Sefer başarıyla iptal edildi"
                        : reader.GetString(reader.GetOrdinal("Mesaj"));

                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = mesaj
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Sefer başarıyla iptal edildi"
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Sefer iptal edilirken SQL hatası oluştu. TripID: {TripID}", id);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sefer iptal edilirken hata oluştu. TripID: {TripID}", id);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Sefer iptal edilirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Seferi siler (Eski metod - geriye dönük uyumluluk için)
        /// </summary>
        [HttpDelete("trips/{id}")]
        public async Task<IActionResult> DeleteTrip(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Geçerli sefer ID'si gereklidir"
                    });
                }

                // Sefer var mı kontrol et
                var tripExists = await _context.Trips.AnyAsync(t => t.TripID == id);
                if (!tripExists)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Sefer bulunamadı"
                    });
                }

                var connectionString = _context.Database.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Veritabanı bağlantı hatası"
                    });
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("UPDATE app.Trips SET Status = 0 WHERE TripID = @TripID", connection);
                command.Parameters.AddWithValue("@TripID", id);

                var rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Sefer başarıyla silindi"
                    });
                }
                else
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Sefer bulunamadı veya güncellenemedi"
                    });
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Sefer silinirken SQL hatası oluştu. TripID: {TripID}", id);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sefer silinirken hata oluştu");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Sefer silinirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Şirket istatistiklerini getirir
        /// </summary>
        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetCompanyStats()
        {
            try
            {
                // Şirket ID'sini dinamik olarak bul
                int sirketID = await GetCompanyIDAsync();
                _logger.LogInformation("🔍 Şirket istatistikleri getiriliyor. SirketID: {SirketID}", sirketID);

                // Önce şirkete ait araçları kontrol et (debug için)
                var debugCompanyVehicles = await _context.Vehicles
                    .Where(v => v.CompanyID == sirketID)
                    .Select(v => new { v.VehicleID, v.PlateOrCode, v.VehicleType, v.CompanyID })
                    .ToListAsync();
                
                _logger.LogInformation("🔍 DEBUG - Şirket ID: {SirketID} için araç sayısı: {VehicleCount}", sirketID, debugCompanyVehicles.Count);
                if (debugCompanyVehicles.Any())
                {
                    _logger.LogInformation("🔍 DEBUG - Araçlar: {Vehicles}", 
                        string.Join(", ", debugCompanyVehicles.Select(v => $"VehicleID:{v.VehicleID} Plate:{v.PlateOrCode} Type:{v.VehicleType} CompanyID:{v.CompanyID}")));
                }

                var debugCompanyTrips = await _context.Trips
                    .Where(t => debugCompanyVehicles.Select(v => v.VehicleID).Contains(t.VehicleID))
                    .Select(t => new { t.TripID, t.VehicleID, t.Status })
                    .Take(5)
                    .ToListAsync();
                
                _logger.LogInformation("🔍 DEBUG - Şirket ID: {SirketID} için sefer sayısı: {TripCount} (ilk 5 gösteriliyor)", 
                    sirketID, await _context.Trips.CountAsync(t => debugCompanyVehicles.Select(v => v.VehicleID).Contains(t.VehicleID)));
                if (debugCompanyTrips.Any())
                {
                    _logger.LogInformation("🔍 DEBUG - Seferler: {Trips}", 
                        string.Join(", ", debugCompanyTrips.Select(t => $"TripID:{t.TripID} VehicleID:{t.VehicleID} Status:{t.Status}")));
                }

                var connectionString = _context.Database.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("❌ Veritabanı bağlantı string'i boş");
                    return BadRequest(new ApiResponse<CompanyStatsDTO>
                    {
                        Success = false,
                        Message = "Veritabanı bağlantı hatası"
                    });
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using (var checkCommand = new SqlCommand(
                    "SELECT COUNT(*) FROM report.vw_Sirket_Istatistikleri WHERE SirketID = @SirketID",
                    connection))
                {
                    checkCommand.Parameters.AddWithValue("@SirketID", sirketID);
                    var viewCountResult = await checkCommand.ExecuteScalarAsync();
                    var viewCount = viewCountResult != null ? (int)viewCountResult : 0;
                    _logger.LogInformation("🔍 DEBUG - View'de şirket kayıt sayısı: {Count}", viewCount);
                }
                
                var allCompanies = new List<string>();
                using (var allCompaniesCommand = new SqlCommand(
                    "SELECT SirketID, SirketAdi FROM report.vw_Sirket_Istatistikleri",
                    connection))
                {
                    using var allCompaniesReader = await allCompaniesCommand.ExecuteReaderAsync();
                    while (await allCompaniesReader.ReadAsync())
                    {
                        var companyId = allCompaniesReader.GetInt32(0);
                        var companyName = allCompaniesReader.IsDBNull(1) ? "NULL" : allCompaniesReader.GetString(1);
                        allCompanies.Add($"ID:{companyId} Ad:{companyName}");
                    }
                }
                _logger.LogInformation("🔍 DEBUG - View'deki tüm şirketler: {Companies}", string.Join(", ", allCompanies));

                using var command = new SqlCommand(
                    "SELECT * FROM report.vw_Sirket_Istatistikleri WHERE SirketID = @SirketID",
                    connection);

                command.Parameters.AddWithValue("@SirketID", sirketID);

                _logger.LogInformation("🔍 View sorgusu çalıştırılıyor: vw_Sirket_Istatistikleri, @SirketID = {SirketID}", sirketID);

                using var reader = await command.ExecuteReaderAsync();

                CompanyStatsDTO? stats = null;

                if (await reader.ReadAsync())
                {
                    _logger.LogInformation("✅ View'den veri geldi. SirketID: {SirketID}", sirketID);
                    
                    stats = new CompanyStatsDTO
                    {
                        SirketID = reader.GetInt32(reader.GetOrdinal("SirketID")),
                        SirketAdi = reader.IsDBNull(reader.GetOrdinal("SirketAdi")) 
                            ? null 
                            : reader.GetString(reader.GetOrdinal("SirketAdi")),
                        SirketEmail = reader.IsDBNull(reader.GetOrdinal("SirketEmail")) 
                            ? null 
                            : reader.GetString(reader.GetOrdinal("SirketEmail")),
                        TotalTrips = reader.IsDBNull(reader.GetOrdinal("ToplamSefer")) 
                            ? 0 
                            : reader.GetInt32(reader.GetOrdinal("ToplamSefer")),
                        ActiveTrips = reader.IsDBNull(reader.GetOrdinal("AktifSefer")) 
                            ? 0 
                            : reader.GetInt32(reader.GetOrdinal("AktifSefer")),
                        IptalSefer = reader.IsDBNull(reader.GetOrdinal("IptalSefer")) 
                            ? 0 
                            : reader.GetInt32(reader.GetOrdinal("IptalSefer")),
                        TotalReservations = reader.IsDBNull(reader.GetOrdinal("ToplamRezervasyon")) 
                            ? 0 
                            : reader.GetInt32(reader.GetOrdinal("ToplamRezervasyon")),
                        ActiveReservations = reader.IsDBNull(reader.GetOrdinal("AktifRezervasyon")) 
                            ? 0 
                            : reader.GetInt32(reader.GetOrdinal("AktifRezervasyon")),
                        IptalRezervasyon = reader.IsDBNull(reader.GetOrdinal("IptalRezervasyon")) 
                            ? 0 
                            : reader.GetInt32(reader.GetOrdinal("IptalRezervasyon")),
                        ToplamGelir = reader.IsDBNull(reader.GetOrdinal("ToplamGelir")) 
                            ? 0 
                            : reader.GetDecimal(reader.GetOrdinal("ToplamGelir")),
                        SonBirAyGelir = reader.IsDBNull(reader.GetOrdinal("SonBirAyGelir")) 
                            ? 0 
                            : reader.GetDecimal(reader.GetOrdinal("SonBirAyGelir")),
                        ToplamArac = reader.IsDBNull(reader.GetOrdinal("ToplamArac")) 
                            ? 0 
                            : reader.GetInt32(reader.GetOrdinal("ToplamArac")),
                        OtobusSayisi = reader.IsDBNull(reader.GetOrdinal("OtobusSayisi")) 
                            ? 0 
                            : reader.GetInt32(reader.GetOrdinal("OtobusSayisi")),
                        TrenSayisi = reader.IsDBNull(reader.GetOrdinal("TrenSayisi")) 
                            ? 0 
                            : reader.GetInt32(reader.GetOrdinal("TrenSayisi")),
                        OrtalamaDoluKoltukOrani = reader.IsDBNull(reader.GetOrdinal("OrtalamaDoluKoltukOrani")) 
                            ? 0 
                            : (decimal)reader.GetDouble(reader.GetOrdinal("OrtalamaDoluKoltukOrani")),
                        BuAyEklenenSefer = reader.IsDBNull(reader.GetOrdinal("BuAyEklenenSefer")) 
                            ? 0 
                            : reader.GetInt32(reader.GetOrdinal("BuAyEklenenSefer")),
                        SonGuncellemeTarihi = reader.IsDBNull(reader.GetOrdinal("SonGuncellemeTarihi")) 
                            ? null 
                            : reader.GetDateTime(reader.GetOrdinal("SonGuncellemeTarihi"))
                    };
                    
                    _logger.LogInformation("📊 View'den alınan istatistikler: ToplamSefer={TotalTrips}, AktifSefer={ActiveTrips}, ToplamRezervasyon={TotalReservations}, ToplamGelir={ToplamGelir}, ToplamArac={ToplamArac}, OtobusSayisi={OtobusSayisi}, TrenSayisi={TrenSayisi}", 
                        stats.TotalTrips, stats.ActiveTrips, stats.TotalReservations, stats.ToplamGelir, stats.ToplamArac, stats.OtobusSayisi, stats.TrenSayisi);
                    
                    // View'den veri geldi ama tüm önemli değerler 0 ise, fallback mekanizmasını kullan
                    if (stats.TotalTrips == 0 && stats.TotalReservations == 0 && stats.ToplamArac == 0)
                    {
                        _logger.LogWarning("⚠️ View'den veri geldi ancak tüm değerler 0. Fallback mekanizması kullanılacak. SirketID: {SirketID}", sirketID);
                        stats = null; // Fallback mekanizmasını tetiklemek için null yap
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ View'den veri gelmedi. SirketID: {SirketID} için view'de kayıt yok. View'de tüm şirketleri kontrol edin: SELECT SirketID, SirketAdi FROM report.vw_Sirket_Istatistikleri", sirketID);
                }

                // Eğer view'den veri gelmediyse veya tüm değerler 0 ise, fallback olarak stored procedure kullan
                if (stats == null)
                {
                    _logger.LogWarning("⚠️ View'den veri gelmedi, stored procedure fallback yöntemi kullanılıyor. SirketID: {SirketID}", sirketID);
                    
                    var spConnectionString = _context.Database.GetConnectionString();
                    if (!string.IsNullOrEmpty(spConnectionString))
                    {
                        using var spConnection = new SqlConnection(spConnectionString);
                        await spConnection.OpenAsync();

                        using var spCommand = new SqlCommand("[proc].sp_Sirket_Istatistikleri_Getir", spConnection);
                        spCommand.CommandType = System.Data.CommandType.StoredProcedure;
                        spCommand.Parameters.AddWithValue("@SirketID", sirketID);

                        using var spReader = await spCommand.ExecuteReaderAsync();
                        if (await spReader.ReadAsync())
                        {
                            stats = new CompanyStatsDTO
                            {
                                SirketID = spReader.GetInt32(spReader.GetOrdinal("SirketID")),
                                SirketAdi = spReader.IsDBNull(spReader.GetOrdinal("SirketAdi"))
                                    ? null
                                    : spReader.GetString(spReader.GetOrdinal("SirketAdi")),
                                SirketEmail = spReader.IsDBNull(spReader.GetOrdinal("SirketEmail"))
                                    ? null
                                    : spReader.GetString(spReader.GetOrdinal("SirketEmail")),
                                TotalTrips = spReader.IsDBNull(spReader.GetOrdinal("TotalTrips"))
                                    ? 0
                                    : spReader.GetInt32(spReader.GetOrdinal("TotalTrips")),
                                ActiveTrips = spReader.IsDBNull(spReader.GetOrdinal("ActiveTrips"))
                                    ? 0
                                    : spReader.GetInt32(spReader.GetOrdinal("ActiveTrips")),
                                IptalSefer = spReader.IsDBNull(spReader.GetOrdinal("IptalSefer"))
                                    ? 0
                                    : spReader.GetInt32(spReader.GetOrdinal("IptalSefer")),
                                TotalReservations = spReader.IsDBNull(spReader.GetOrdinal("TotalReservations"))
                                    ? 0
                                    : spReader.GetInt32(spReader.GetOrdinal("TotalReservations")),
                                ActiveReservations = spReader.IsDBNull(spReader.GetOrdinal("ActiveReservations"))
                                    ? 0
                                    : spReader.GetInt32(spReader.GetOrdinal("ActiveReservations")),
                                IptalRezervasyon = spReader.IsDBNull(spReader.GetOrdinal("IptalRezervasyon"))
                                    ? 0
                                    : spReader.GetInt32(spReader.GetOrdinal("IptalRezervasyon")),
                                ToplamGelir = spReader.IsDBNull(spReader.GetOrdinal("ToplamGelir"))
                                    ? 0
                                    : spReader.GetDecimal(spReader.GetOrdinal("ToplamGelir")),
                                SonBirAyGelir = spReader.IsDBNull(spReader.GetOrdinal("SonBirAyGelir"))
                                    ? 0
                                    : spReader.GetDecimal(spReader.GetOrdinal("SonBirAyGelir")),
                                ToplamArac = spReader.IsDBNull(spReader.GetOrdinal("ToplamArac"))
                                    ? 0
                                    : spReader.GetInt32(spReader.GetOrdinal("ToplamArac")),
                                OtobusSayisi = spReader.IsDBNull(spReader.GetOrdinal("OtobusSayisi"))
                                    ? 0
                                    : spReader.GetInt32(spReader.GetOrdinal("OtobusSayisi")),
                                TrenSayisi = spReader.IsDBNull(spReader.GetOrdinal("TrenSayisi"))
                                    ? 0
                                    : spReader.GetInt32(spReader.GetOrdinal("TrenSayisi")),
                                OrtalamaDoluKoltukOrani = spReader.IsDBNull(spReader.GetOrdinal("OrtalamaDoluKoltukOrani"))
                                    ? 0
                                    : spReader.GetDecimal(spReader.GetOrdinal("OrtalamaDoluKoltukOrani")),
                                BuAyEklenenSefer = spReader.IsDBNull(spReader.GetOrdinal("BuAyEklenenSefer"))
                                    ? 0
                                    : spReader.GetInt32(spReader.GetOrdinal("BuAyEklenenSefer")),
                                SonGuncellemeTarihi = spReader.IsDBNull(spReader.GetOrdinal("SonGuncellemeTarihi"))
                                    ? DateTime.UtcNow
                                    : spReader.GetDateTime(spReader.GetOrdinal("SonGuncellemeTarihi"))
                            };
                            
                            _logger.LogInformation("📊 Stored procedure ile hesaplanan istatistikler: ToplamSefer={TotalTrips}, AktifSefer={ActiveTrips}, ToplamRezervasyon={TotalReservations}, ToplamGelir={ToplamGelir}, ToplamArac={ToplamArac}", 
                                stats.TotalTrips, stats.ActiveTrips, stats.TotalReservations, stats.ToplamGelir, stats.ToplamArac);
                        }
                    }
                }
                
                // Eğer stored procedure de çalışmazsa, son fallback olarak EF Core kullan
                if (stats == null)
                {
                    _logger.LogWarning("⚠️ Stored procedure de çalışmadı, EF Core fallback yöntemi kullanılıyor. SirketID: {SirketID}", sirketID);
                    
                    // Önce şirkete ait araçları kontrol et
                    var companyVehicles = await _context.Vehicles
                        .Where(v => v.CompanyID == sirketID)
                        .Select(v => v.VehicleID)
                        .ToListAsync();
                    
                    var companyTripIds = await _context.Trips
                        .Where(t => companyVehicles.Contains(t.VehicleID))
                        .Select(t => t.TripID)
                        .ToListAsync();
                    
                    var totalTrips = companyTripIds.Count;
                    var activeTrips = await _context.Trips
                        .Where(t => companyVehicles.Contains(t.VehicleID) && t.Status == 1)
                        .CountAsync();
                    
                    var totalReservations = await _context.Reservations
                        .Where(r => companyTripIds.Contains(r.TripID) && r.Status != "Cancelled")
                        .CountAsync();
                    var activeReservations = await _context.Reservations
                        .Where(r => companyTripIds.Contains(r.TripID) && (r.Status == "Reserved" || r.Status == "Confirmed"))
                        .CountAsync();
                    
                    var totalGelir = await _context.Payments
                        .Include(p => p.Reservation)
                        .Where(p => p.Reservation != null 
                                 && companyTripIds.Contains(p.Reservation.TripID)
                                 && p.Status == "Completed")
                        .SumAsync(p => (decimal?)p.Amount) ?? 0;
                    
                    var sonBirAyGelir = await _context.Payments
                        .Include(p => p.Reservation)
                        .Where(p => p.Reservation != null 
                                 && companyTripIds.Contains(p.Reservation.TripID)
                                 && p.Status == "Completed"
                                 && p.PaymentDate >= DateTime.UtcNow.AddMonths(-1))
                        .SumAsync(p => (decimal?)p.Amount) ?? 0;
                    
                    var toplamArac = await _context.Vehicles
                        .Where(v => v.CompanyID == sirketID && v.Active)
                        .CountAsync();
                    var otobusSayisi = await _context.Vehicles
                        .Where(v => v.CompanyID == sirketID && v.Active && v.VehicleType == "Bus")
                        .CountAsync();
                    var trenSayisi = await _context.Vehicles
                        .Where(v => v.CompanyID == sirketID && v.Active && v.VehicleType == "Train")
                        .CountAsync();
                    
                    var doluKoltukSayisi = await _context.TripSeats
                        .Where(ts => companyTripIds.Contains(ts.TripID) && ts.IsReserved)
                        .CountAsync();
                    var toplamKoltukSayisi = await _context.TripSeats
                        .Where(ts => companyTripIds.Contains(ts.TripID))
                        .CountAsync();
                    var ortalamaDoluKoltukOrani = toplamKoltukSayisi > 0 
                        ? (decimal)((double)doluKoltukSayisi / toplamKoltukSayisi * 100) 
                        : 0;
                    
                    var buAyEklenenSefer = await _context.Trips
                        .Where(t => companyVehicles.Contains(t.VehicleID) 
                                 && t.CreatedAt >= DateTime.UtcNow.AddMonths(-1))
                        .CountAsync();

                    var companyUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserID == sirketID);
                    
                    stats = new CompanyStatsDTO
                    {
                        SirketID = sirketID,
                        SirketAdi = companyUser?.FullName,
                        SirketEmail = companyUser?.Email,
                        TotalTrips = totalTrips,
                        ActiveTrips = activeTrips,
                        IptalSefer = totalTrips - activeTrips,
                        TotalReservations = totalReservations,
                        ActiveReservations = activeReservations,
                        IptalRezervasyon = await _context.Reservations
                            .Where(r => companyTripIds.Contains(r.TripID) && r.Status == "Cancelled")
                            .CountAsync(),
                        ToplamGelir = totalGelir,
                        SonBirAyGelir = sonBirAyGelir,
                        ToplamArac = toplamArac,
                        OtobusSayisi = otobusSayisi,
                        TrenSayisi = trenSayisi,
                        OrtalamaDoluKoltukOrani = ortalamaDoluKoltukOrani,
                        BuAyEklenenSefer = buAyEklenenSefer,
                        SonGuncellemeTarihi = DateTime.UtcNow
                    };
                }

                _logger.LogInformation("✅ İstatistikler başarıyla getirildi. Toplam Sefer: {TotalTrips}, Aktif Sefer: {ActiveTrips}", 
                    stats.TotalTrips, stats.ActiveTrips);
                
                _logger.LogInformation("📤 Response gönderiliyor - SirketID: {SirketID}, SirketAdi: {SirketAdi}, TotalTrips: {TotalTrips}, TotalReservations: {TotalReservations}, ToplamArac: {ToplamArac}, ToplamGelir: {ToplamGelir}", 
                    stats.SirketID, stats.SirketAdi, stats.TotalTrips, stats.TotalReservations, stats.ToplamArac, stats.ToplamGelir);

                var response = new ApiResponse<CompanyStatsDTO>
                {
                    Success = true,
                    Message = "İstatistikler başarıyla getirildi",
                    Data = stats
                };
                
                _logger.LogInformation("📤 Response Data içeriği - TotalTrips: {TotalTrips}, ActiveTrips: {ActiveTrips}, TotalReservations: {TotalReservations}", 
                    response.Data?.TotalTrips, response.Data?.ActiveTrips, response.Data?.TotalReservations);
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İstatistikler getirilirken hata oluştu");
                return BadRequest(new ApiResponse<CompanyStatsDTO>
                {
                    Success = false,
                    Message = "İstatistikler getirilirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Araçları getirir (tip bazlı filtreleme ile)
        /// </summary>
        [HttpGet("vehicles")]
        public async Task<IActionResult> GetVehicles([FromQuery] string? vehicleType = null)
        {
            try
            {
                // Şirket ID'sini dinamik olarak bul
                int sirketID = await GetCompanyIDAsync();
                _logger.LogInformation("🔍 Şirket araçları getiriliyor. SirketID: {SirketID}", sirketID);

                var query = _context.Vehicles.AsQueryable();

                // Sadece şirkete ait araçları getir
                query = query.Where(v => v.CompanyID == sirketID);

                if (!string.IsNullOrEmpty(vehicleType))
                {
                    query = query.Where(v => v.VehicleType == vehicleType && v.Active);
                }
                else
                {
                    query = query.Where(v => v.Active);
                }

                var vehicles = await query
                    .Select(v => new VehicleDTO
                    {
                        VehicleID = v.VehicleID,
                        VehicleType = v.VehicleType,
                        PlateOrCode = v.PlateOrCode,
                        SeatCount = _context.Seats.Count(s => s.VehicleID == v.VehicleID), // Koltuk sayısını hesapla
                        Active = v.Active
                    })
                    .OrderBy(v => v.VehicleType)
                    .ThenBy(v => v.PlateOrCode)
                    .ToListAsync();

                return Ok(new ApiResponse<IEnumerable<VehicleDTO>>
                {
                    Success = true,
                    Message = "Araçlar başarıyla getirildi",
                    Data = vehicles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Araçlar getirilirken hata oluştu");
                return BadRequest(new ApiResponse<IEnumerable<VehicleDTO>>
                {
                    Success = false,
                    Message = "Araçlar getirilirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}

