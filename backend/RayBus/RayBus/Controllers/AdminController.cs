using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RayBus.Attributes;
using RayBus.Data;
using RayBus.Models.DTOs;
using RayBus.Models.Entities;
using RayBus.Repositories;

namespace RayBus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    
    public class AdminController : ControllerBase
    {
        private readonly RayBusDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            RayBusDbContext context, 
            IUserRepository userRepository,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userRepository = userRepository;
            _logger = logger;
        }

        /// <summary>
        /// Tüm kullanıcıları getirir (Stored Procedure ile - arama ve filtreleme desteği)
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] string? aramaMetni = null, [FromQuery] int? rolID = null)
        {
            try
            {
                var users = await _userRepository.GetAdminUsersUsingStoredProcedureAsync(aramaMetni, rolID);

                return Ok(new ApiResponse<IEnumerable<AdminUserDTO>>
                {
                    Success = true,
                    Message = "Kullanıcılar başarıyla getirildi",
                    Data = users
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcılar getirilirken hata oluştu");
                return BadRequest(new ApiResponse<IEnumerable<AdminUserDTO>>
                {
                    Success = false,
                    Message = "Kullanıcılar getirilirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Belirli bir kullanıcıyı getirir
        /// </summary>
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.UserID == id)
                    .Select(u => new AdminUserDTO
                    {
                        UserID = u.UserID,
                        FullName = u.FullName,
                        Email = u.Email,
                        Phone = u.Phone ?? string.Empty,
                        RoleName = u.Role!.RoleName,
                        Status = u.Status,
                        CreatedAt = u.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new ApiResponse<AdminUserDTO>
                    {
                        Success = false,
                        Message = "Kullanıcı bulunamadı"
                    });
                }

                return Ok(new ApiResponse<AdminUserDTO>
                {
                    Success = true,
                    Message = "Kullanıcı başarıyla getirildi",
                    Data = user
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı getirilirken hata oluştu");
                return BadRequest(new ApiResponse<AdminUserDTO>
                {
                    Success = false,
                    Message = "Kullanıcı getirilirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Kullanıcı durumunu günceller (Stored Procedure ile)
        /// </summary>
        [HttpPut("users/{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusDTO statusDto)
        {
            try
            {
                // Status byte (0 veya 1) -> bool'a çevir
                bool yeniDurum = statusDto.Status == 1;

                var (success, message) = await _userRepository.UpdateUserStatusUsingStoredProcedureAsync(
                    id, 
                    yeniDurum, 
                    statusDto.Sebep
                );

                if (!success)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = message
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı durumu güncellenirken hata oluştu");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Kullanıcı durumu güncellenirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Kullanıcı bilgilerini günceller (Stored Procedure ile)
        /// </summary>
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDTO updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

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

                using var command = new SqlCommand("[proc].sp_Admin_Kullanici_Guncelle", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@UserID", id);
                command.Parameters.AddWithValue("@FullName", 
                    string.IsNullOrWhiteSpace(updateDto.FullName) ? DBNull.Value : (object)updateDto.FullName.Trim());
                command.Parameters.AddWithValue("@Email", 
                    string.IsNullOrWhiteSpace(updateDto.Email) ? DBNull.Value : (object)updateDto.Email.Trim().ToLower());
                command.Parameters.AddWithValue("@Phone", 
                    string.IsNullOrWhiteSpace(updateDto.Phone) ? DBNull.Value : (object)updateDto.Phone.Trim());

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var mesaj = reader.IsDBNull(reader.GetOrdinal("Mesaj"))
                        ? "Kullanıcı bilgileri başarıyla güncellendi"
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
                    Message = "Kullanıcı bilgileri başarıyla güncellendi"
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Kullanıcı güncellenirken SQL hatası oluştu");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı güncellenirken hata oluştu");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Kullanıcı güncellenirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Kullanıcıyı siler
        /// </summary>
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Kullanıcı bulunamadı"
                    });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Kullanıcı başarıyla silindi"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı silinirken hata oluştu");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Kullanıcı silinirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Tüm rezervasyonları getirir
        /// </summary>
        [HttpGet("reservations")]
        public async Task<IActionResult> GetAllReservations()
        {
            try
            {
                // report.vw_Admin_Rezervasyonlar view'ını kullan
                var connectionString = _context.Database.GetConnectionString();
                var reservations = new List<AdminReservationDTO>();

                if (!string.IsNullOrEmpty(connectionString))
                {
                    using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();

                    using var command = new SqlCommand("SELECT * FROM report.vw_Admin_Rezervasyonlar ORDER BY ReservationDate DESC", connection);
                    using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        reservations.Add(new AdminReservationDTO
                        {
                            ReservationID = reader.GetInt32(reader.GetOrdinal("ReservationID")),
                            UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                            UserName = reader.IsDBNull(reader.GetOrdinal("UserName"))
                                ? "Bilinmeyen Kullanıcı"
                                : reader.GetString(reader.GetOrdinal("UserName")),
                            TripID = reader.GetInt32(reader.GetOrdinal("TripID")),
                            TripRoute = reader.IsDBNull(reader.GetOrdinal("TripRoute"))
                                ? $"Sefer #{reader.GetInt32(reader.GetOrdinal("TripID"))}"
                                : reader.GetString(reader.GetOrdinal("TripRoute")),
                            SeatID = reader.GetInt32(reader.GetOrdinal("SeatID")),
                            SeatNo = reader.IsDBNull(reader.GetOrdinal("SeatNo"))
                                ? $"Koltuk #{reader.GetInt32(reader.GetOrdinal("SeatID"))}"
                                : reader.GetString(reader.GetOrdinal("SeatNo")),
                            Status = reader.IsDBNull(reader.GetOrdinal("Status"))
                                ? string.Empty
                                : reader.GetString(reader.GetOrdinal("Status")),
                            PaymentStatus = reader.IsDBNull(reader.GetOrdinal("PaymentStatus"))
                                ? string.Empty
                                : reader.GetString(reader.GetOrdinal("PaymentStatus")),
                            ReservationDate = reader.GetDateTime(reader.GetOrdinal("ReservationDate"))
                        });
                    }
                }
                else
                {
                    // Fallback: EF Core kullan
                    reservations = await _context.Reservations
                        .Include(r => r.User)
                        .Include(r => r.TripSeat)
                            .ThenInclude(ts => ts!.Trip)
                                .ThenInclude(t => t!.FromCity)
                        .Include(r => r.TripSeat)
                            .ThenInclude(ts => ts!.Trip)
                                .ThenInclude(t => t!.ToCity)
                        .Include(r => r.TripSeat)
                            .ThenInclude(ts => ts!.Seat)
                        .Select(r => new AdminReservationDTO
                        {
                            ReservationID = r.ReservationID,
                            UserID = r.UserID,
                            UserName = r.User != null ? r.User.FullName : "Bilinmeyen Kullanıcı",
                            TripID = r.TripID,
                            TripRoute = r.TripSeat != null && r.TripSeat.Trip != null && r.TripSeat.Trip.FromCity != null && r.TripSeat.Trip.ToCity != null
                                ? $"{r.TripSeat.Trip.FromCity.CityName} - {r.TripSeat.Trip.ToCity.CityName}"
                                : $"Sefer #{r.TripID}",
                            SeatID = r.SeatID,
                            SeatNo = r.TripSeat != null && r.TripSeat.Seat != null ? r.TripSeat.Seat.SeatNo : $"Koltuk #{r.SeatID}",
                            Status = r.Status,
                            PaymentStatus = r.PaymentStatus,
                            ReservationDate = r.ReservationDate
                        })
                        .ToListAsync();
                }

                return Ok(new ApiResponse<IEnumerable<AdminReservationDTO>>
                {
                    Success = true,
                    Message = "Rezervasyonlar başarıyla getirildi",
                    Data = reservations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rezervasyonlar getirilirken hata oluştu");
                return BadRequest(new ApiResponse<IEnumerable<AdminReservationDTO>>
                {
                    Success = false,
                    Message = "Rezervasyonlar getirilirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Rezervasyon durumunu günceller
        /// </summary>
        [HttpPut("reservations/{id}/status")]
        public async Task<IActionResult> UpdateReservationStatus(int id, [FromBody] UpdateReservationStatusDTO statusDto)
        {
            try
            {
                var reservation = await _context.Reservations.FindAsync(id);
                if (reservation == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Rezervasyon bulunamadı"
                    });
                }

                reservation.Status = statusDto.Status;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Rezervasyon durumu başarıyla güncellendi"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rezervasyon durumu güncellenirken hata oluştu");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Rezervasyon durumu güncellenirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Rezervasyonu iptal eder
        /// </summary>
        [HttpPost("reservations/{id}/cancel")]
        public async Task<IActionResult> CancelReservation(int id, [FromBody] CancelReservationDTO cancelDto)
        {
            try
            {
                var reservation = await _context.Reservations.FindAsync(id);
                if (reservation == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Rezervasyon bulunamadı"
                    });
                }

                reservation.Status = "Cancelled";
                reservation.CancelReasonID = cancelDto.CancelReasonID;

                var tripSeat = await _context.TripSeats
                    .FirstOrDefaultAsync(ts => ts.TripID == reservation.TripID && ts.SeatID == reservation.SeatID);
                
                if (tripSeat != null)
                {
                    tripSeat.IsReserved = false;
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Rezervasyon başarıyla iptal edildi"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rezervasyon iptal edilirken hata oluştu");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Rezervasyon iptal edilirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Yeni sefer ekler (Stored Procedure ile - çakışma kontrolü ile)
        /// </summary>
        [HttpPost("trips")]
        public async Task<IActionResult> AddTrip([FromBody] CreateTripDTO createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

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

                // Stored Procedure çağır (Güncellenmiş - tüm alanlar dahil)
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

                using var command = new SqlCommand("[proc].sp_Admin_Sefer_Ekle", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                // Zorunlu parametreler
                command.Parameters.AddWithValue("@NeredenID", createDto.FromCityID);
                command.Parameters.AddWithValue("@NereyeID", createDto.ToCityID);
                command.Parameters.AddWithValue("@AracID", createDto.VehicleID);
                command.Parameters.AddWithValue("@Tarih", createDto.DepartureDate.Date);
                command.Parameters.AddWithValue("@Saat", createDto.DepartureTime);
                command.Parameters.AddWithValue("@Fiyat", createDto.Price);

                // Opsiyonel parametreler (NULL kontrolü ile)
                command.Parameters.AddWithValue("@KalkisTerminalID", 
                    createDto.DepartureTerminalID.HasValue ? (object)createDto.DepartureTerminalID.Value : DBNull.Value);
                command.Parameters.AddWithValue("@VarisTerminalID", 
                    createDto.ArrivalTerminalID.HasValue ? (object)createDto.ArrivalTerminalID.Value : DBNull.Value);
                command.Parameters.AddWithValue("@KalkisIstasyonID", 
                    createDto.DepartureStationID.HasValue ? (object)createDto.DepartureStationID.Value : DBNull.Value);
                command.Parameters.AddWithValue("@VarisIstasyonID", 
                    createDto.ArrivalStationID.HasValue ? (object)createDto.ArrivalStationID.Value : DBNull.Value);
                command.Parameters.AddWithValue("@VarisTarihi", 
                    createDto.ArrivalDate.HasValue ? (object)createDto.ArrivalDate.Value.Date : DBNull.Value);
                command.Parameters.AddWithValue("@VarisSaati", 
                    createDto.ArrivalTime.HasValue ? (object)createDto.ArrivalTime.Value : DBNull.Value);

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
        /// Tüm seferleri getirir
        /// </summary>
        [HttpGet("trips")]
        public async Task<IActionResult> GetAllTrips()
        {
            try
            {
                // report.vw_Admin_Seferler view'ını kullan
                var connectionString = _context.Database.GetConnectionString();
                var trips = new List<AdminTripDTO>();

                if (!string.IsNullOrEmpty(connectionString))
                {
                    using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();

                    using var command = new SqlCommand("SELECT * FROM report.vw_Admin_Seferler ORDER BY DepartureDate DESC", connection);
                    using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        trips.Add(new AdminTripDTO
                        {
                            TripID = reader.GetInt32(reader.GetOrdinal("TripID")),
                            VehicleID = reader.GetInt32(reader.GetOrdinal("VehicleID")),
                            VehicleType = reader.IsDBNull(reader.GetOrdinal("VehicleType"))
                                ? string.Empty
                                : reader.GetString(reader.GetOrdinal("VehicleType")),
                            VehicleCompanyID = reader.IsDBNull(reader.GetOrdinal("CompanyID"))
                                ? null
                                : reader.GetInt32(reader.GetOrdinal("CompanyID")),
                            FromCityID = reader.GetInt32(reader.GetOrdinal("FromCityID")),
                            FromCity = reader.IsDBNull(reader.GetOrdinal("FromCityName"))
                                ? string.Empty
                                : reader.GetString(reader.GetOrdinal("FromCityName")),
                            ToCityID = reader.GetInt32(reader.GetOrdinal("ToCityID")),
                            ToCity = reader.IsDBNull(reader.GetOrdinal("ToCityName"))
                                ? string.Empty
                                : reader.GetString(reader.GetOrdinal("ToCityName")),
                            DepartureTerminalID = reader.IsDBNull(reader.GetOrdinal("DepartureTerminalID"))
                                ? null
                                : reader.GetInt32(reader.GetOrdinal("DepartureTerminalID")),
                            DepartureTerminal = reader.IsDBNull(reader.GetOrdinal("DepartureTerminalName"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("DepartureTerminalName")),
                            ArrivalTerminalID = reader.IsDBNull(reader.GetOrdinal("ArrivalTerminalID"))
                                ? null
                                : reader.GetInt32(reader.GetOrdinal("ArrivalTerminalID")),
                            ArrivalTerminal = reader.IsDBNull(reader.GetOrdinal("ArrivalTerminalName"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("ArrivalTerminalName")),
                            DepartureStationID = reader.IsDBNull(reader.GetOrdinal("DepartureStationID"))
                                ? null
                                : reader.GetInt32(reader.GetOrdinal("DepartureStationID")),
                            DepartureStation = reader.IsDBNull(reader.GetOrdinal("DepartureStationName"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("DepartureStationName")),
                            ArrivalStationID = reader.IsDBNull(reader.GetOrdinal("ArrivalStationID"))
                                ? null
                                : reader.GetInt32(reader.GetOrdinal("ArrivalStationID")),
                            ArrivalStation = reader.IsDBNull(reader.GetOrdinal("ArrivalStationName"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("ArrivalStationName")),
                            DepartureDate = reader.GetDateTime(reader.GetOrdinal("DepartureDate")),
                            DepartureTime = reader.GetTimeSpan(reader.GetOrdinal("DepartureTime")),
                            ArrivalDate = reader.IsDBNull(reader.GetOrdinal("ArrivalDate"))
                                ? null
                                : reader.GetDateTime(reader.GetOrdinal("ArrivalDate")),
                            ArrivalTime = reader.IsDBNull(reader.GetOrdinal("ArrivalTime"))
                                ? null
                                : reader.GetTimeSpan(reader.GetOrdinal("ArrivalTime")),
                            Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                            Status = reader.GetByte(reader.GetOrdinal("Status"))
                        });
                    }
                }
                else
                {
                    // Fallback: EF Core kullan
                    trips = await _context.Trips
                        .Include(t => t.Vehicle)
                        .Include(t => t.FromCity)
                        .Include(t => t.ToCity)
                        .Include(t => t.DepartureTerminal)
                        .Include(t => t.ArrivalTerminal)
                        .Include(t => t.DepartureStation)
                        .Include(t => t.ArrivalStation)
                        .Select(t => new AdminTripDTO
                        {
                            TripID = t.TripID,
                            VehicleID = t.VehicleID,
                            VehicleType = t.Vehicle!.VehicleType,
                            VehicleCompanyID = t.Vehicle.CompanyID,
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
                        .ToListAsync();
                }

                return Ok(new ApiResponse<IEnumerable<AdminTripDTO>>
                {
                    Success = true,
                    Message = "Seferler başarıyla getirildi",
                    Data = trips
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Seferler getirilirken hata oluştu");
                return BadRequest(new ApiResponse<IEnumerable<AdminTripDTO>>
                {
                    Success = false,
                    Message = "Seferler getirilirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Sefer bilgilerini günceller (Stored Procedure ile)
        /// </summary>
        [HttpPut("trips/{id}")]
        public async Task<IActionResult> UpdateTrip(int id, [FromBody] UpdateTripDTO updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (id <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Geçerli sefer ID'si gereklidir"
                    });
                }

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

                using var command = new SqlCommand("[proc].sp_Admin_Sefer_Guncelle", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@SeferID", id);
                command.Parameters.AddWithValue("@NeredenID", 
                    updateDto.FromCityID.HasValue ? (object)updateDto.FromCityID.Value : DBNull.Value);
                command.Parameters.AddWithValue("@NereyeID", 
                    updateDto.ToCityID.HasValue ? (object)updateDto.ToCityID.Value : DBNull.Value);
                command.Parameters.AddWithValue("@AracID", 
                    updateDto.VehicleID.HasValue ? (object)updateDto.VehicleID.Value : DBNull.Value);
                command.Parameters.AddWithValue("@Tarih", 
                    updateDto.DepartureDate.HasValue ? (object)updateDto.DepartureDate.Value.Date : DBNull.Value);
                command.Parameters.AddWithValue("@Saat", 
                    updateDto.DepartureTime.HasValue ? (object)updateDto.DepartureTime.Value : DBNull.Value);
                command.Parameters.AddWithValue("@Fiyat", 
                    updateDto.Price.HasValue ? (object)updateDto.Price.Value : DBNull.Value);
                command.Parameters.AddWithValue("@KalkisTerminalID", 
                    updateDto.DepartureTerminalID.HasValue ? (object)updateDto.DepartureTerminalID.Value : DBNull.Value);
                command.Parameters.AddWithValue("@VarisTerminalID", 
                    updateDto.ArrivalTerminalID.HasValue ? (object)updateDto.ArrivalTerminalID.Value : DBNull.Value);
                command.Parameters.AddWithValue("@KalkisIstasyonID", 
                    updateDto.DepartureStationID.HasValue ? (object)updateDto.DepartureStationID.Value : DBNull.Value);
                command.Parameters.AddWithValue("@VarisIstasyonID", 
                    updateDto.ArrivalStationID.HasValue ? (object)updateDto.ArrivalStationID.Value : DBNull.Value);
                command.Parameters.AddWithValue("@VarisTarihi", 
                    updateDto.ArrivalDate.HasValue ? (object)updateDto.ArrivalDate.Value.Date : DBNull.Value);
                command.Parameters.AddWithValue("@VarisSaati", 
                    updateDto.ArrivalTime.HasValue ? (object)updateDto.ArrivalTime.Value : DBNull.Value);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var mesaj = reader.IsDBNull(reader.GetOrdinal("Mesaj"))
                        ? "Sefer bilgileri başarıyla güncellendi"
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
                    Message = "Sefer bilgileri başarıyla güncellendi"
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Sefer güncellenirken SQL hatası oluştu");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
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
        /// Seferi iptal eder (Stored Procedure ile - geçmiş sefer kontrolü ile)
        /// </summary>
        [HttpPost("trips/{id}/cancel")]
        public async Task<IActionResult> CancelTrip(int id, [FromBody] CancelTripDTO? cancelDto = null)
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

                using var command = new SqlCommand("[proc].sp_Admin_Sefer_Iptal", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

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
        /// Sefer durumunu günceller
        /// </summary>
        [HttpPut("trips/{id}/status")]
        public async Task<IActionResult> UpdateTripStatus(int id, [FromBody] UpdateTripStatusDTO statusDto)
        {
            try
            {
                var trip = await _context.Trips.FindAsync(id);
                if (trip == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Sefer bulunamadı"
                    });
                }

                trip.Status = statusDto.Status;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Sefer durumu başarıyla güncellendi"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sefer durumu güncellenirken hata oluştu");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Sefer durumu güncellenirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Dashboard istatistiklerini getirir (View ve EF Core kombinasyonu)
        /// </summary>
        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                // View'dan özet istatistikleri al
                DashboardStatsDTO? viewStats = null;
                var connectionString = _context.Database.GetConnectionString();
                if (!string.IsNullOrEmpty(connectionString))
                {
                    using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();

                    using var command = new SqlCommand("SELECT * FROM report.vw_Admin_Dashboard_Ozet", connection);
                    using var reader = await command.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        viewStats = new DashboardStatsDTO
                        {
                            ToplamAktifUye = reader.IsDBNull(reader.GetOrdinal("ToplamAktifUye"))
                                ? 0
                                : reader.GetInt32(reader.GetOrdinal("ToplamAktifUye")),
                            GelecekSeferler = reader.IsDBNull(reader.GetOrdinal("GelecekSeferler"))
                                ? 0
                                : reader.GetInt32(reader.GetOrdinal("GelecekSeferler")),
                            GunlukCiro = reader.IsDBNull(reader.GetOrdinal("GunlukCiro"))
                                ? 0
                                : reader.GetDecimal(reader.GetOrdinal("GunlukCiro")),
                            ToplamSatis = reader.IsDBNull(reader.GetOrdinal("ToplamSatis"))
                                ? 0
                                : reader.GetInt32(reader.GetOrdinal("ToplamSatis")),
                            SonIslemLoglari = reader.IsDBNull(reader.GetOrdinal("SonIslemLoglari"))
                                ? 0
                                : reader.GetInt32(reader.GetOrdinal("SonIslemLoglari"))
                        };
                    }
                }

                // report.vw_Admin_Dashboard_Istatistikleri view'ından tüm istatistikleri al
                DashboardStatsDTO? dashboardStats = null;
                if (!string.IsNullOrEmpty(connectionString))
                {
                    using var dashboardConnection = new SqlConnection(connectionString);
                    await dashboardConnection.OpenAsync();

                    using var dashboardCommand = new SqlCommand("SELECT * FROM report.vw_Admin_Dashboard_Istatistikleri", dashboardConnection);
                    using var dashboardReader = await dashboardCommand.ExecuteReaderAsync();

                    if (await dashboardReader.ReadAsync())
                    {
                        dashboardStats = new DashboardStatsDTO
                        {
                            TotalUsers = dashboardReader.IsDBNull(dashboardReader.GetOrdinal("TotalUsers"))
                                ? 0
                                : dashboardReader.GetInt32(dashboardReader.GetOrdinal("TotalUsers")),
                            ActiveUsers = dashboardReader.IsDBNull(dashboardReader.GetOrdinal("ActiveUsers"))
                                ? 0
                                : dashboardReader.GetInt32(dashboardReader.GetOrdinal("ActiveUsers")),
                            TotalReservations = dashboardReader.IsDBNull(dashboardReader.GetOrdinal("TotalReservations"))
                                ? 0
                                : dashboardReader.GetInt32(dashboardReader.GetOrdinal("TotalReservations")),
                            ActiveReservations = dashboardReader.IsDBNull(dashboardReader.GetOrdinal("ActiveReservations"))
                                ? 0
                                : dashboardReader.GetInt32(dashboardReader.GetOrdinal("ActiveReservations")),
                            TotalTrips = dashboardReader.IsDBNull(dashboardReader.GetOrdinal("TotalTrips"))
                                ? 0
                                : dashboardReader.GetInt32(dashboardReader.GetOrdinal("TotalTrips")),
                            ActiveTrips = dashboardReader.IsDBNull(dashboardReader.GetOrdinal("ActiveTrips"))
                                ? 0
                                : dashboardReader.GetInt32(dashboardReader.GetOrdinal("ActiveTrips")),
                            TotalRevenue = dashboardReader.IsDBNull(dashboardReader.GetOrdinal("TotalRevenue"))
                                ? 0
                                : dashboardReader.GetDecimal(dashboardReader.GetOrdinal("TotalRevenue")),
                            
                            // View'dan gelenler (eski view'dan)
                            ToplamAktifUye = viewStats?.ToplamAktifUye ?? 0,
                            GelecekSeferler = viewStats?.GelecekSeferler ?? 0,
                            GunlukCiro = viewStats?.GunlukCiro ?? 0,
                            ToplamSatis = viewStats?.ToplamSatis ?? 0,
                            SonIslemLoglari = viewStats?.SonIslemLoglari ?? 0
                        };
                    }
                }

                // Fallback: Eğer view çalışmazsa EF Core kullan
                var stats = dashboardStats ?? new DashboardStatsDTO
                {
                    TotalUsers = await _context.Users.CountAsync(),
                    ActiveUsers = await _context.Users.CountAsync(u => u.Status == 1),
                    TotalReservations = await _context.Reservations.CountAsync(),
                    ActiveReservations = await _context.Reservations.CountAsync(r => r.Status != "Cancelled"),
                    TotalTrips = await _context.Trips.CountAsync(),
                    ActiveTrips = await _context.Trips.CountAsync(t => t.Status == 1),
                    TotalRevenue = await _context.Payments
                        .Where(p => p.Status == "Completed")
                        .SumAsync(p => (decimal?)p.Amount) ?? 0,
                    
                    // View'dan gelenler
                    ToplamAktifUye = viewStats?.ToplamAktifUye ?? 0,
                    GelecekSeferler = viewStats?.GelecekSeferler ?? 0,
                    GunlukCiro = viewStats?.GunlukCiro ?? 0,
                    ToplamSatis = viewStats?.ToplamSatis ?? 0,
                    SonIslemLoglari = viewStats?.SonIslemLoglari ?? 0
                };

                return Ok(new ApiResponse<DashboardStatsDTO>
                {
                    Success = true,
                    Message = "İstatistikler başarıyla getirildi",
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İstatistikler getirilirken hata oluştu");
                return BadRequest(new ApiResponse<DashboardStatsDTO>
                {
                    Success = false,
                    Message = "İstatistikler getirilirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Yeni araç ekler (Stored Procedure ile - otomatik koltuk oluşturur)
        /// </summary>
        [HttpPost("vehicles")]
        public async Task<IActionResult> AddVehicle([FromBody] CreateVehicleDTO createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validasyon
                if (string.IsNullOrWhiteSpace(createDto.PlakaNo))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Plaka numarası gereklidir"
                    });
                }

                if (createDto.AracTipi != "Bus" && createDto.AracTipi != "Train")
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Araç tipi 'Bus' veya 'Train' olmalıdır"
                    });
                }

                if (createDto.ToplamKoltuk <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Koltuk sayısı 0'dan büyük olmalıdır"
                    });
                }

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

                using var command = new SqlCommand("[proc].sp_Admin_Arac_Ekle", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@PlakaNo", createDto.PlakaNo);
                command.Parameters.AddWithValue("@AracTipi", createDto.AracTipi);
                command.Parameters.AddWithValue("@ToplamKoltuk", createDto.ToplamKoltuk);
                
                // SirketID: Admin için NULL, şirket için createDto'dan alınır
                if (createDto.SirketID.HasValue && createDto.SirketID.Value > 0)
                {
                    command.Parameters.AddWithValue("@SirketID", createDto.SirketID.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("@SirketID", DBNull.Value);
                }

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var mesaj = reader.IsDBNull(reader.GetOrdinal("Mesaj"))
                        ? "Araç başarıyla oluşturuldu"
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
                    Message = "Araç başarıyla oluşturuldu"
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Araç eklenirken SQL hatası oluştu");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Araç eklenirken hata oluştu");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Araç eklenirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Araç bilgilerini günceller (Stored Procedure ile)
        /// </summary>
        [HttpPut("vehicles/{id}")]
        public async Task<IActionResult> UpdateVehicle(int id, [FromBody] UpdateVehicleDTO updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

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

                using var command = new SqlCommand("[proc].sp_Admin_Arac_Guncelle", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@AracID", id);
                command.Parameters.AddWithValue("@PlakaNo", 
                    string.IsNullOrWhiteSpace(updateDto.PlateOrCode) ? DBNull.Value : (object)updateDto.PlateOrCode.Trim());
                command.Parameters.AddWithValue("@AracTipi", 
                    string.IsNullOrWhiteSpace(updateDto.VehicleType) ? DBNull.Value : (object)updateDto.VehicleType.Trim());
                command.Parameters.AddWithValue("@Aktif", 
                    updateDto.Active.HasValue ? (object)updateDto.Active.Value : DBNull.Value);
                command.Parameters.AddWithValue("@SirketID", 
                    updateDto.CompanyID.HasValue ? (object)updateDto.CompanyID.Value : DBNull.Value);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var mesaj = reader.IsDBNull(reader.GetOrdinal("Mesaj"))
                        ? "Araç bilgileri başarıyla güncellendi"
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
                    Message = "Araç bilgileri başarıyla güncellendi"
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Araç güncellenirken SQL hatası oluştu");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Araç güncellenirken hata oluştu");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Araç güncellenirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Tüm araçları getirir (Admin için - tüm araçlar, şirket bilgisi ile)
        /// </summary>
        [HttpGet("vehicles")]
        public async Task<IActionResult> GetAllVehicles([FromQuery] string? vehicleType = null, [FromQuery] int? companyID = null)
        {
            try
            {
                var query = _context.Vehicles
                    .Include(v => v.Company)
                    .AsQueryable();

                // Aktif araçlar
                query = query.Where(v => v.Active);

                // VehicleType filtresi
                if (!string.IsNullOrEmpty(vehicleType))
                {
                    query = query.Where(v => v.VehicleType == vehicleType);
                }

                // CompanyID filtresi (NULL ise admin araçları, değilse şirket araçları)
                if (companyID.HasValue)
                {
                    if (companyID.Value == 0)
                    {
                        // Admin araçları (CompanyID = NULL)
                        query = query.Where(v => v.CompanyID == null);
                    }
                    else
                    {
                        // Belirli şirkete ait araçlar
                        query = query.Where(v => v.CompanyID == companyID.Value);
                    }
                }
                // companyID null ise tüm aktif araçlar (filtreleme yok)

                var vehicles = await query
                    .Select(v => new
                    {
                        VehicleID = v.VehicleID,
                        VehicleType = v.VehicleType,
                        PlateOrCode = v.PlateOrCode,
                        SeatCount = v.SeatCount, // Direkt property'den al (performans için)
                        Active = v.Active,
                        CompanyID = v.CompanyID,
                        CompanyName = v.Company != null ? v.Company.FullName : null
                    })
                    .OrderBy(v => v.VehicleType)
                    .ThenBy(v => v.PlateOrCode)
                    .ToListAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Araçlar başarıyla getirildi",
                    Data = vehicles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Araçlar getirilirken hata oluştu");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Araçlar getirilirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Tüm şirketleri getirir (Role = 3 olan kullanıcılar)
        /// </summary>
        [HttpGet("companies")]
        public async Task<IActionResult> GetAllCompanies()
        {
            try
            {
                var companies = await _context.Users
                    .Where(u => u.RoleID == 3 && u.Status == 1) // Şirket rolü ve aktif
                    .Select(u => new
                    {
                        CompanyID = u.UserID,
                        CompanyName = u.FullName
                    })
                    .OrderBy(c => c.CompanyName)
                    .ToListAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Şirketler başarıyla getirildi",
                    Data = companies
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şirketler getirilirken hata oluştu");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Şirketler getirilirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Günlük finansal raporu getirir (View ile - tarih ve ödeme yöntemi bazlı)
        /// </summary>
        [HttpGet("reports/daily-financial")]
        public async Task<IActionResult> GetDailyFinancialReport([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                {
                    return BadRequest(new ApiResponse<IEnumerable<DailyFinancialReportDTO>>
                    {
                        Success = false,
                        Message = "Veritabanı bağlantı hatası"
                    });
                }

                var reports = new List<DailyFinancialReportDTO>();

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // View'dan veri çek
                var query = "SELECT * FROM report.vw_Admin_Gunluk_Finansal_Rapor WHERE 1=1";
                
                if (startDate.HasValue)
                {
                    query += " AND IslemTarihi >= @StartDate";
                }
                
                if (endDate.HasValue)
                {
                    query += " AND IslemTarihi <= @EndDate";
                }
                
                query += " ORDER BY IslemTarihi DESC, OdemeYontemi";

                using var command = new SqlCommand(query, connection);
                
                if (startDate.HasValue)
                {
                    command.Parameters.AddWithValue("@StartDate", startDate.Value.Date);
                }
                
                if (endDate.HasValue)
                {
                    command.Parameters.AddWithValue("@EndDate", endDate.Value.Date);
                }

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var report = new DailyFinancialReportDTO
                    {
                        IslemTarihi = reader.IsDBNull(reader.GetOrdinal("IslemTarihi"))
                            ? DateTime.MinValue
                            : reader.GetDateTime(reader.GetOrdinal("IslemTarihi")),
                        OdemeYontemi = reader.IsDBNull(reader.GetOrdinal("OdemeYontemi"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("OdemeYontemi")),
                        ToplamSatisAdedi = reader.IsDBNull(reader.GetOrdinal("ToplamSatisAdedi"))
                            ? 0
                            : reader.GetInt32(reader.GetOrdinal("ToplamSatisAdedi")),
                        ToplamCiro = reader.IsDBNull(reader.GetOrdinal("ToplamCiro"))
                            ? 0
                            : reader.GetDecimal(reader.GetOrdinal("ToplamCiro"))
                    };

                    reports.Add(report);
                }

                return Ok(new ApiResponse<IEnumerable<DailyFinancialReportDTO>>
                {
                    Success = true,
                    Message = "Günlük finansal rapor başarıyla getirildi",
                    Data = reports
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Günlük finansal rapor getirilirken SQL hatası oluştu");
                return BadRequest(new ApiResponse<IEnumerable<DailyFinancialReportDTO>>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Günlük finansal rapor getirilirken hata oluştu");
                return BadRequest(new ApiResponse<IEnumerable<DailyFinancialReportDTO>>
                {
                    Success = false,
                    Message = "Günlük finansal rapor getirilirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Güzergah bazlı ciro raporunu getirir (vw_Guzergah_Ciro_Raporu view'ından)
        /// </summary>
        [HttpGet("reports/route-revenue")]
        public async Task<IActionResult> GetRouteRevenueReport()
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                {
                    return BadRequest(new ApiResponse<IEnumerable<GuzergahCiroRaporuDTO>>
                    {
                        Success = false,
                        Message = "Veritabanı bağlantı bilgisi bulunamadı"
                    });
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("SELECT * FROM report.vw_Guzergah_Ciro_Raporu ORDER BY ToplamCiro DESC", connection);
                using var reader = await command.ExecuteReaderAsync();

                var reports = new List<GuzergahCiroRaporuDTO>();

                while (await reader.ReadAsync())
                {
                    reports.Add(new GuzergahCiroRaporuDTO
                    {
                        Guzergah = reader.IsDBNull(reader.GetOrdinal("Guzergah"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("Guzergah")),
                        AracTipi = reader.IsDBNull(reader.GetOrdinal("AracTipi"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("AracTipi")),
                        ToplamSatisAdedi = reader.IsDBNull(reader.GetOrdinal("ToplamSatisAdedi"))
                            ? 0
                            : reader.GetInt32(reader.GetOrdinal("ToplamSatisAdedi")),
                        ToplamCiro = reader.IsDBNull(reader.GetOrdinal("ToplamCiro"))
                            ? 0
                            : reader.GetDecimal(reader.GetOrdinal("ToplamCiro")),
                        OrtalamaBiletFiyati = reader.IsDBNull(reader.GetOrdinal("OrtalamaBiletFiyati"))
                            ? null
                            : reader.GetDecimal(reader.GetOrdinal("OrtalamaBiletFiyati"))
                    });
                }

                return Ok(new ApiResponse<IEnumerable<GuzergahCiroRaporuDTO>>
                {
                    Success = true,
                    Message = "Güzergah ciro raporu başarıyla getirildi",
                    Data = reports
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Güzergah ciro raporu getirilirken hata oluştu");
                return BadRequest(new ApiResponse<IEnumerable<GuzergahCiroRaporuDTO>>
                {
                    Success = false,
                    Message = "Güzergah ciro raporu getirilirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Sefer detaylarını getirir (vw_Sefer_Detaylari view'ından)
        /// </summary>
        [HttpGet("trips/details")]
        public async Task<IActionResult> GetTripDetails([FromQuery] int? tripId = null)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                {
                    return BadRequest(new ApiResponse<IEnumerable<SeferDetaylariDTO>>
                    {
                        Success = false,
                        Message = "Veritabanı bağlantı bilgisi bulunamadı"
                    });
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                string query = "SELECT * FROM report.vw_Sefer_Detaylari";
                if (tripId.HasValue)
                {
                    query += " WHERE TripID = @TripID";
                }
                query += " ORDER BY DepartureDate DESC, DepartureTime DESC";

                using var command = new SqlCommand(query, connection);
                if (tripId.HasValue)
                {
                    command.Parameters.AddWithValue("@TripID", tripId.Value);
                }

                using var reader = await command.ExecuteReaderAsync();

                var tripDetails = new List<SeferDetaylariDTO>();

                while (await reader.ReadAsync())
                {
                    tripDetails.Add(new SeferDetaylariDTO
                    {
                        TripID = reader.GetInt32(reader.GetOrdinal("TripID")),
                        DepartureDate = reader.GetDateTime(reader.GetOrdinal("DepartureDate")),
                        DepartureTime = reader.GetTimeSpan(reader.GetOrdinal("DepartureTime")),
                        Nereden = reader.IsDBNull(reader.GetOrdinal("Nereden"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("Nereden")),
                        Nereye = reader.IsDBNull(reader.GetOrdinal("Nereye"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("Nereye")),
                        AracTipi = reader.IsDBNull(reader.GetOrdinal("AracTipi"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("AracTipi")),
                        PlakaNo = reader.IsDBNull(reader.GetOrdinal("PlakaNo"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("PlakaNo")),
                        BiletFiyati = reader.IsDBNull(reader.GetOrdinal("BiletFiyati"))
                            ? 0
                            : reader.GetDecimal(reader.GetOrdinal("BiletFiyati")),
                        ToplamKoltuk = reader.IsDBNull(reader.GetOrdinal("ToplamKoltuk"))
                            ? 0
                            : reader.GetInt32(reader.GetOrdinal("ToplamKoltuk")),
                        SatilanKoltuk = reader.IsDBNull(reader.GetOrdinal("SatilanKoltuk"))
                            ? 0
                            : reader.GetInt32(reader.GetOrdinal("SatilanKoltuk")),
                        SeferDurumu = reader.IsDBNull(reader.GetOrdinal("SeferDurumu"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("SeferDurumu"))
                    });
                }

                return Ok(new ApiResponse<IEnumerable<SeferDetaylariDTO>>
                {
                    Success = true,
                    Message = tripId.HasValue 
                        ? "Sefer detayı başarıyla getirildi" 
                        : "Sefer detayları başarıyla getirildi",
                    Data = tripDetails
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sefer detayları getirilirken hata oluştu");
                return BadRequest(new ApiResponse<IEnumerable<SeferDetaylariDTO>>
                {
                    Success = false,
                    Message = "Sefer detayları getirilirken bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}

