using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RayBus.Data;
using RayBus.Models.DTOs;
using RayBus.Models.Entities;

namespace RayBus.Services
{
    public class NotificationService : INotificationService
    {
        private readonly RayBusDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly IEmailService _emailService;
        private readonly string _connectionString;

        public NotificationService(
            RayBusDbContext context,
            ILogger<NotificationService> logger,
            IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _connectionString = context.Database.GetConnectionString() 
                ?? throw new InvalidOperationException("Connection string not found");
        }

        public async Task<ApiResponse<object>> SendNotificationAsync(SendNotificationDTO sendDto)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Bildirim_Gonder", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@UserID", sendDto.UserID);
                command.Parameters.AddWithValue("@NotificationType", sendDto.NotificationType);
                command.Parameters.AddWithValue("@NotificationMethod", sendDto.NotificationMethod);
                command.Parameters.AddWithValue("@Subject", sendDto.Subject);
                command.Parameters.AddWithValue("@Message", sendDto.Message);
                command.Parameters.AddWithValue("@RelatedReservationID", 
                    sendDto.RelatedReservationID.HasValue ? sendDto.RelatedReservationID.Value : DBNull.Value);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var notificationId = reader.GetInt32(reader.GetOrdinal("NotificationID"));
                    var mesaj = reader.GetString(reader.GetOrdinal("Mesaj"));

                    _logger.LogInformation("✅ Bildirim kuyruğa eklendi. NotificationID: {NotificationID}", notificationId);

                    return ApiResponse<object>.SuccessResponse(
                        new { NotificationID = notificationId, Message = mesaj },
                        mesaj
                    );
                }

                return ApiResponse<object>.ErrorResponse("Bildirim gönderilemedi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Bildirim gönderilirken hata");
                return ApiResponse<object>.ErrorResponse(
                    "Bildirim gönderilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<IEnumerable<NotificationQueueDTO>>> GetPendingNotificationsAsync(int maxCount = 100)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Bildirim_Kuyrugu_Isle", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@MaxProcessCount", maxCount);

                using var reader = await command.ExecuteReaderAsync();

                var notifications = new List<NotificationQueueDTO>();

                while (await reader.ReadAsync())
                {
                    notifications.Add(new NotificationQueueDTO
                    {
                        NotificationID = reader.GetInt32(reader.GetOrdinal("NotificationID")),
                        UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                        NotificationType = reader.IsDBNull(reader.GetOrdinal("NotificationType"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("NotificationType")),
                        NotificationMethod = reader.IsDBNull(reader.GetOrdinal("NotificationMethod"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("NotificationMethod")),
                        Subject = reader.IsDBNull(reader.GetOrdinal("Subject"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Subject")),
                        Message = reader.IsDBNull(reader.GetOrdinal("Message"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("Message")),
                        RelatedReservationID = reader.IsDBNull(reader.GetOrdinal("RelatedReservationID"))
                            ? null
                            : reader.GetInt32(reader.GetOrdinal("RelatedReservationID"))
                    });
                }

                foreach (var notification in notifications)
                {
                    var user = await _context.Users.FindAsync(notification.UserID);
                    notification.UserName = user?.FullName ?? "Bilinmiyor";
                }

                return ApiResponse<IEnumerable<NotificationQueueDTO>>.SuccessResponse(
                    notifications,
                    "Bekleyen bildirimler başarıyla getirildi"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Bekleyen bildirimler getirilirken hata");
                return ApiResponse<IEnumerable<NotificationQueueDTO>>.ErrorResponse(
                    "Bekleyen bildirimler getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<bool>> UpdateNotificationStatusAsync(UpdateNotificationStatusDTO updateDto)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Bildirim_Durum_Guncelle", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@NotificationID", updateDto.NotificationID);
                command.Parameters.AddWithValue("@Status", updateDto.Status);
                command.Parameters.AddWithValue("@ErrorMessage", 
                    string.IsNullOrEmpty(updateDto.ErrorMessage) ? DBNull.Value : updateDto.ErrorMessage);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return ApiResponse<bool>.SuccessResponse(
                        true,
                        "Bildirim durumu başarıyla güncellendi"
                    );
                }

                return ApiResponse<bool>.ErrorResponse("Bildirim durumu güncellenemedi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Bildirim durumu güncellenirken hata");
                return ApiResponse<bool>.ErrorResponse(
                    "Bildirim durumu güncellenirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<UserNotificationPreferencesDTO>> GetUserPreferencesAsync(int userId)
        {
            try
            {
                var preferences = await _context.UserNotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserID == userId);

                    if (preferences == null)
                    {
                        preferences = new UserNotificationPreferences
                    {
                        UserID = userId,
                        EmailNotifications = true,
                        SMSNotifications = false, // SMS artık desteklenmiyor
                        ReservationNotifications = true,
                        PaymentNotifications = true,
                        CancellationNotifications = true,
                        ReminderNotifications = true
                    };

                    _context.UserNotificationPreferences.Add(preferences);
                    await _context.SaveChangesAsync();
                }

                var dto = new UserNotificationPreferencesDTO
                {
                    PreferenceID = preferences.PreferenceID,
                    UserID = preferences.UserID,
                    EmailNotifications = preferences.EmailNotifications,
                    SMSNotifications = preferences.SMSNotifications,
                    ReservationNotifications = preferences.ReservationNotifications,
                    PaymentNotifications = preferences.PaymentNotifications,
                    CancellationNotifications = preferences.CancellationNotifications,
                    ReminderNotifications = preferences.ReminderNotifications,
                    UpdatedAt = preferences.UpdatedAt
                };

                return ApiResponse<UserNotificationPreferencesDTO>.SuccessResponse(
                    dto,
                    "Bildirim tercihleri başarıyla getirildi"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Bildirim tercihleri getirilirken hata. UserID: {UserID}", userId);
                return ApiResponse<UserNotificationPreferencesDTO>.ErrorResponse(
                    "Bildirim tercihleri getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<UserNotificationPreferencesDTO>> UpdateUserPreferencesAsync(int userId, UserNotificationPreferencesDTO preferences)
        {
            try
            {
                var existingPreferences = await _context.UserNotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserID == userId);

                if (existingPreferences == null)
                {
                    existingPreferences = new UserNotificationPreferences
                    {
                        UserID = userId
                    };
                    _context.UserNotificationPreferences.Add(existingPreferences);
                }

                existingPreferences.EmailNotifications = preferences.EmailNotifications;
                existingPreferences.SMSNotifications = false; // SMS artık desteklenmiyor, her zaman false
                existingPreferences.ReservationNotifications = preferences.ReservationNotifications;
                existingPreferences.PaymentNotifications = preferences.PaymentNotifications;
                existingPreferences.CancellationNotifications = preferences.CancellationNotifications;
                existingPreferences.ReminderNotifications = preferences.ReminderNotifications;
                existingPreferences.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("💾 Bildirim tercihleri güncelleniyor. UserID: {UserID}, Email: {Email}, Reservation: {Reservation}, Payment: {Payment}, Cancellation: {Cancellation}, Reminder: {Reminder}",
                    userId,
                    existingPreferences.EmailNotifications,
                    existingPreferences.ReservationNotifications,
                    existingPreferences.PaymentNotifications,
                    existingPreferences.CancellationNotifications,
                    existingPreferences.ReminderNotifications);

                await _context.SaveChangesAsync();

                var dto = new UserNotificationPreferencesDTO
                {
                    PreferenceID = existingPreferences.PreferenceID,
                    UserID = existingPreferences.UserID,
                    EmailNotifications = existingPreferences.EmailNotifications,
                    SMSNotifications = existingPreferences.SMSNotifications,
                    ReservationNotifications = existingPreferences.ReservationNotifications,
                    PaymentNotifications = existingPreferences.PaymentNotifications,
                    CancellationNotifications = existingPreferences.CancellationNotifications,
                    ReminderNotifications = existingPreferences.ReminderNotifications,
                    UpdatedAt = existingPreferences.UpdatedAt
                };

                _logger.LogInformation("✅ Bildirim tercihleri güncellendi ve döndürülüyor. UserID: {UserID}, Email: {Email}, Reservation: {Reservation}, Payment: {Payment}, Cancellation: {Cancellation}, Reminder: {Reminder}",
                    userId,
                    dto.EmailNotifications,
                    dto.ReservationNotifications,
                    dto.PaymentNotifications,
                    dto.CancellationNotifications,
                    dto.ReminderNotifications);

                return ApiResponse<UserNotificationPreferencesDTO>.SuccessResponse(
                    dto,
                    "Bildirim tercihleri başarıyla güncellendi"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Bildirim tercihleri güncellenirken hata. UserID: {UserID}", userId);
                return ApiResponse<UserNotificationPreferencesDTO>.ErrorResponse(
                    "Bildirim tercihleri güncellenirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<IEnumerable<NotificationQueueDTO>>> GetUserNotificationsAsync(int userId, int? limit = null)
        {
            try
            {
                var query = _context.NotificationQueues
                    .Include(n => n.User)
                    .Where(n => n.UserID == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .AsQueryable();

                if (limit.HasValue)
                {
                    query = query.Take(limit.Value);
                }

                var notifications = await query.ToListAsync();

                var notificationDtos = notifications.Select(n => new NotificationQueueDTO
                {
                    NotificationID = n.NotificationID,
                    UserID = n.UserID,
                    UserName = n.User?.FullName ?? "Bilinmiyor",
                    NotificationType = n.NotificationType,
                    NotificationMethod = n.NotificationMethod,
                    Subject = n.Subject,
                    Message = n.Message,
                    Status = n.Status,
                    CreatedAt = n.CreatedAt,
                    SentAt = n.SentAt,
                    RetryCount = n.RetryCount,
                    ErrorMessage = n.ErrorMessage,
                    RelatedReservationID = n.RelatedReservationID
                }).ToList();

                return ApiResponse<IEnumerable<NotificationQueueDTO>>.SuccessResponse(
                    notificationDtos,
                    "Kullanıcı bildirimleri başarıyla getirildi"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Kullanıcı bildirimleri getirilirken hata. UserID: {UserID}", userId);
                return ApiResponse<IEnumerable<NotificationQueueDTO>>.ErrorResponse(
                    "Kullanıcı bildirimleri getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }
    }
}

