using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RayBus.Data;
using RayBus.Models.DTOs;
using System;

namespace RayBus.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly RayBusDbContext _context;
        private readonly string _connectionString;
        private readonly ILogger<UserRepository>? _logger;

        public UserRepository(RayBusDbContext context, ILogger<UserRepository>? logger = null)
        {
            _context = context;
            _connectionString = context.Database.GetConnectionString() ?? throw new InvalidOperationException("Connection string not found");
            _logger = logger;
        }

        public async Task<(bool Success, int UserID, string ErrorMessage)> RegisterUserUsingStoredProcedureAsync(
            string fullName, string email, string passwordHash, string phone, string roleName)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Kullanici_Kayit", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@AdSoyad", fullName);
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                command.Parameters.AddWithValue("@Telefon", phone);
                command.Parameters.AddWithValue("@RolAdi", roleName);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var mesaj = reader.IsDBNull(reader.GetOrdinal("Mesaj")) 
                        ? string.Empty 
                        : reader.GetString(reader.GetOrdinal("Mesaj"));
                    
                    var yeniUserIDValue = reader.IsDBNull(reader.GetOrdinal("YeniUserID"))
                        ? 0
                        : Convert.ToInt32(reader.GetValue(reader.GetOrdinal("YeniUserID")));
                    
                    var yeniUserID = yeniUserIDValue;

                    if (yeniUserID > 0)
                    {
                        return (true, yeniUserID, mesaj);
                    }
                }

                return (false, 0, "Kayıt işlemi başarısız");
            }
            catch (SqlException ex)
            {
                return (false, 0, ex.Message);
            }
            catch (Exception ex)
            {
                return (false, 0, $"Kayıt işlemi sırasında bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<(bool Success, int? UserID, string FullName, int? RoleID, string? RoleName, string? PasswordHash, string? Phone, DateTime? CreatedAt, string ErrorMessage)> LoginUserUsingStoredProcedureAsync(
            string email)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Kullanici_Giris", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@Email", email);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var basariliValue = reader.IsDBNull(reader.GetOrdinal("Basarili"))
                        ? 0
                        : Convert.ToInt32(reader.GetValue(reader.GetOrdinal("Basarili")));
                    var basarili = basariliValue == 1;
                    
                    var mesaj = reader.IsDBNull(reader.GetOrdinal("Mesaj")) 
                        ? string.Empty 
                        : reader.GetString(reader.GetOrdinal("Mesaj"));

                    if (basarili)
                    {
                        var userID = reader.IsDBNull(reader.GetOrdinal("UserID")) 
                            ? (int?)null 
                            : reader.GetInt32(reader.GetOrdinal("UserID"));
                        
                        var adSoyad = reader.IsDBNull(reader.GetOrdinal("AdSoyad")) 
                            ? string.Empty 
                            : reader.GetString(reader.GetOrdinal("AdSoyad"));
                        
                        var roleID = reader.IsDBNull(reader.GetOrdinal("RoleID")) 
                            ? (int?)null 
                            : reader.GetInt32(reader.GetOrdinal("RoleID"));
                        
                        var rolAdi = reader.IsDBNull(reader.GetOrdinal("RolAdi")) 
                            ? null 
                            : reader.GetString(reader.GetOrdinal("RolAdi"));
                        
                        var passwordHash = reader.IsDBNull(reader.GetOrdinal("PasswordHash")) 
                            ? null 
                            : reader.GetString(reader.GetOrdinal("PasswordHash"));
                        
                        var telefon = reader.IsDBNull(reader.GetOrdinal("Telefon")) 
                            ? null 
                            : reader.GetString(reader.GetOrdinal("Telefon"));
                        
                        var createdAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) 
                            ? (DateTime?)null 
                            : reader.GetDateTime(reader.GetOrdinal("CreatedAt"));

                        return (true, userID, adSoyad, roleID, rolAdi, passwordHash, telefon, createdAt, mesaj);
                    }
                    else
                    {
                        return (false, null, string.Empty, null, null, null, null, null, mesaj);
                    }
                }

                return (false, null, string.Empty, null, null, null, null, null, "Kullanıcı bulunamadı");
            }
            catch (SqlException ex)
            {
                return (false, null, string.Empty, null, null, null, null, null, ex.Message);
            }
            catch (Exception ex)
            {
                return (false, null, string.Empty, null, null, null, null, null, $"Giriş işlemi sırasında bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<List<AdminUserDTO>> GetAdminUsersUsingStoredProcedureAsync(string? aramaMetni = null, int? rolID = null)
        {
            var users = new List<AdminUserDTO>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Admin_Kullanicilari_Getir", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                if (string.IsNullOrWhiteSpace(aramaMetni))
                {
                    command.Parameters.AddWithValue("@AramaMetni", DBNull.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("@AramaMetni", aramaMetni);
                }

                if (rolID.HasValue)
                {
                    command.Parameters.AddWithValue("@RolID", rolID.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("@RolID", DBNull.Value);
                }

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var user = new AdminUserDTO
                    {
                        UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                        FullName = reader.IsDBNull(reader.GetOrdinal("FullName")) 
                            ? string.Empty 
                            : reader.GetString(reader.GetOrdinal("FullName")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) 
                            ? string.Empty 
                            : reader.GetString(reader.GetOrdinal("Email")),
                        Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) 
                            ? string.Empty 
                            : reader.GetString(reader.GetOrdinal("Phone")),
                        RoleName = reader.IsDBNull(reader.GetOrdinal("RoleName")) 
                            ? string.Empty 
                            : reader.GetString(reader.GetOrdinal("RoleName")),
                        Status = reader.IsDBNull(reader.GetOrdinal("Durum")) 
                            ? (byte)0 
                            : reader.GetByte(reader.GetOrdinal("Durum")),
                        Durum = reader.IsDBNull(reader.GetOrdinal("Durum")) 
                            ? (byte)0 
                            : reader.GetByte(reader.GetOrdinal("Durum")),
                        CreatedAt = reader.IsDBNull(reader.GetOrdinal("KayitTarihi")) 
                            ? DateTime.UtcNow 
                            : reader.GetDateTime(reader.GetOrdinal("KayitTarihi")),
                        KayitTarihi = reader.IsDBNull(reader.GetOrdinal("KayitTarihi")) 
                            ? DateTime.UtcNow 
                            : reader.GetDateTime(reader.GetOrdinal("KayitTarihi")),
                        ToplamHarcama = reader.IsDBNull(reader.GetOrdinal("ToplamHarcama")) 
                            ? 0 
                            : reader.GetDecimal(reader.GetOrdinal("ToplamHarcama"))
                    };

                    users.Add(user);
                }

                return users;
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "Kullanıcılar getirilirken SQL hatası oluştu");
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Kullanıcılar getirilirken hata oluştu");
                throw;
            }
        }

        public async Task<(bool Success, string Message)> UpdateUserStatusUsingStoredProcedureAsync(int userID, bool yeniDurum, string? sebep = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Admin_Kullanici_Durum_Degistir", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@UserID", userID);
                command.Parameters.AddWithValue("@YeniDurum", yeniDurum ? 1 : 0);
                command.Parameters.AddWithValue("@Sebep", string.IsNullOrWhiteSpace(sebep) ? DBNull.Value : (object)sebep);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var mesaj = reader.IsDBNull(reader.GetOrdinal("Mesaj"))
                        ? "Kullanıcı durumu güncellendi."
                        : reader.GetString(reader.GetOrdinal("Mesaj"));

                    return (true, mesaj);
                }

                return (false, "Durum güncellenemedi");
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "Kullanıcı durumu güncellenirken SQL hatası oluştu. UserID: {UserID}", userID);
                return (false, ex.Message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Kullanıcı durumu güncellenirken hata oluştu. UserID: {UserID}", userID);
                return (false, $"Durum güncellenirken bir hata oluştu: {ex.Message}");
            }
        }
    }
}

