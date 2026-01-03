using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RayBus.Data;
using RayBus.Models.Entities;
using System.Text;

namespace RayBus.Services
{
    public class NotificationQueueProcessor : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationQueueProcessor> _logger;
        private readonly int _processIntervalSeconds;
        private readonly int _maxRetryCount;

        public NotificationQueueProcessor(
            IServiceProvider serviceProvider,
            ILogger<NotificationQueueProcessor> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _processIntervalSeconds = configuration.GetValue<int>("Notifications:ProcessQueueIntervalSeconds", 30);
            _maxRetryCount = configuration.GetValue<int>("Notifications:MaxRetryCount", 3);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔄 Bildirim kuyruğu işleyici başlatıldı. İşlem aralığı: {Interval} saniye", _processIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessNotificationQueueAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Bildirim kuyruğu işlenirken hata");
                }

                await Task.Delay(TimeSpan.FromSeconds(_processIntervalSeconds), stoppingToken);
            }
        }

        private async Task ProcessNotificationQueueAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<RayBusDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var ticketService = scope.ServiceProvider.GetRequiredService<ITicketService>();

            var pendingNotifications = await context.NotificationQueues
                .Include(n => n.User)
                .Where(n => n.Status == "Pending" && n.RetryCount < _maxRetryCount)
                .OrderBy(n => n.CreatedAt)
                .Take(20)
                .ToListAsync();

            if (!pendingNotifications.Any())
            {
                return;
            }

            _logger.LogInformation("📬 {Count} adet bekleyen bildirim bulundu, işleniyor...", pendingNotifications.Count);

            foreach (var notification in pendingNotifications)
            {
                try
                {
                    bool success = false;
                    string errorMessage = string.Empty;

                    var preferences = await context.UserNotificationPreferences
                        .FirstOrDefaultAsync(p => p.UserID == notification.UserID);

                    if (notification.NotificationMethod == "Email" || notification.NotificationMethod == "Both")
                    {
                        bool shouldSendEmail = (preferences == null || preferences.EmailNotifications != false) && notification.User?.Email != null;
                        
                        if (!shouldSendEmail)
                        {
                            _logger.LogWarning("⚠️ Email gönderilmeyecek. NotificationID: {NotificationID}, UserID: {UserID}, Email: {Email}, Preferences: {Preferences}", 
                                notification.NotificationID, 
                                notification.UserID, 
                                notification.User?.Email ?? "NULL",
                                preferences == null ? "NULL" : $"EmailNotifications={preferences.EmailNotifications}");
                        }
                        
                        if (shouldSendEmail)
                        {
                            if (notification.NotificationType == "Payment" && notification.RelatedReservationID.HasValue)
                            {
                                try
                                {
                                    var reservation = await context.Reservations
                                        .Include(r => r.TripSeat)
                                            .ThenInclude(ts => ts!.Trip)
                                                .ThenInclude(t => t!.FromCity)
                                        .Include(r => r.TripSeat)
                                            .ThenInclude(ts => ts!.Trip)
                                                .ThenInclude(t => t!.ToCity)
                                        .Include(r => r.TripSeat)
                                            .ThenInclude(ts => ts!.Seat)
                                        .FirstOrDefaultAsync(r => r.ReservationID == notification.RelatedReservationID.Value);

                                    if (reservation?.TripSeat?.Trip != null)
                                    {
                                        var trip = reservation.TripSeat.Trip;
                                        var fromCity = trip.FromCity?.CityName ?? "Bilinmiyor";
                                        var toCity = trip.ToCity?.CityName ?? "Bilinmiyor";
                                        var departureDate = trip.DepartureDate.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
                                        var departureTime = trip.DepartureTime.ToString(@"hh\:mm");
                                        var ticketNumber = reservation.TicketNumber ?? $"RB-{reservation.ReservationID}";
                                        var seatNumber = reservation.TripSeat.Seat?.SeatNo ?? "Bilinmiyor";
                                        var vehicleType = trip.Vehicle?.VehicleType == "Train" ? "🚄 Tren" : "🚌 Otobüs";

                                        var emailBody = GeneratePaymentEmailHtml(
                                            notification.User.FullName ?? "Değerli Müşterimiz",
                                            fromCity,
                                            toCity,
                                            departureDate,
                                            departureTime,
                                            ticketNumber,
                                            seatNumber,
                                            vehicleType,
                                            trip.Price
                                        );

                                        var pdfResponse = await ticketService.GenerateTicketPDFAsync(notification.RelatedReservationID.Value);
                                        byte[]? pdfData = null;
                                        
                                        if (pdfResponse.Success && pdfResponse.Data != null)
                                        {
                                            pdfData = pdfResponse.Data;
                                        }

                                        success = await emailService.SendEmailWithAttachmentAsync(
                                            notification.User.Email,
                                            notification.User.FullName ?? "Kullanıcı",
                                            "🎫 Biletiniz Hazır! - RayBus",
                                            emailBody,
                                            pdfData ?? Array.Empty<byte>(),
                                            $"Bilet_{ticketNumber}.pdf",
                                            "application/pdf",
                                            true
                                        );

                                        if (!success)
                                        {
                                            errorMessage = "Email gönderilemedi";
                                        }
                                    }
                                    else
                                    {
                                        success = await emailService.SendEmailAsync(
                                            notification.User.Email,
                                            notification.User.FullName ?? "Kullanıcı",
                                            notification.Subject ?? "RayBus Bildirimi",
                                            notification.Message
                                        );

                                        if (!success)
                                        {
                                            errorMessage = "Email gönderilemedi";
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "❌ Payment email gönderilirken hata. NotificationID: {NotificationID}", notification.NotificationID);
                                    errorMessage = $"Email gönderilirken hata: {ex.Message}";
                                }
                            }
                            else if (notification.RelatedReservationID.HasValue)
                            {
                                try
                                {
                                    var reservation = await context.Reservations
                                        .Include(r => r.TripSeat)
                                            .ThenInclude(ts => ts!.Trip)
                                                .ThenInclude(t => t!.FromCity)
                                        .Include(r => r.TripSeat)
                                            .ThenInclude(ts => ts!.Trip)
                                                .ThenInclude(t => t!.ToCity)
                                        .Include(r => r.TripSeat)
                                            .ThenInclude(ts => ts!.Seat)
                                        .Include(r => r.TripSeat)
                                            .ThenInclude(ts => ts!.Trip)
                                                .ThenInclude(t => t!.Vehicle)
                                        .FirstOrDefaultAsync(r => r.ReservationID == notification.RelatedReservationID.Value);

                                    if (reservation?.TripSeat?.Trip != null)
                                    {
                                        var trip = reservation.TripSeat.Trip;
                                        var fromCity = trip.FromCity?.CityName ?? "Bilinmiyor";
                                        var toCity = trip.ToCity?.CityName ?? "Bilinmiyor";
                                        var departureDate = trip.DepartureDate.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
                                        var departureTime = trip.DepartureTime.ToString(@"hh\:mm");
                                        var ticketNumber = reservation.TicketNumber ?? $"RB-{reservation.ReservationID}";
                                        var seatNumber = reservation.TripSeat.Seat?.SeatNo ?? "Bilinmiyor";
                                        var vehicleType = trip.Vehicle?.VehicleType == "Train" ? "🚄 Tren" : "🚌 Otobüs";
                                        var price = trip.Price;

                                        string emailBody;
                                        string emailSubject;
                                        byte[]? pdfData = null;
                                        string? pdfFileName = null;

                                        if (notification.NotificationType == "Reservation")
                                        {
                                            emailSubject = "✅ Rezervasyonunuz Oluşturuldu - RayBus";
                                            emailBody = GenerateReservationEmailHtml(
                                                notification.User.FullName ?? "Değerli Müşterimiz",
                                                fromCity,
                                                toCity,
                                                departureDate,
                                                departureTime,
                                                ticketNumber,
                                                seatNumber,
                                                vehicleType,
                                                price,
                                                reservation.ReservationID
                                            );

                                            if (reservation.PaymentStatus == "Paid")
                                            {
                                                var pdfResponse = await ticketService.GenerateTicketPDFAsync(notification.RelatedReservationID.Value);
                                                if (pdfResponse.Success && pdfResponse.Data != null)
                                                {
                                                    pdfData = pdfResponse.Data;
                                                    pdfFileName = $"Bilet_{ticketNumber}.pdf";
                                                }
                                            }
                                        }
                                        else if (notification.NotificationType == "Cancellation")
                                        {
                                            emailSubject = "❌ Rezervasyonunuz İptal Edildi - RayBus";
                                            var cancelReason = reservation.CancelReason?.ReasonText ?? "Belirtilmedi";
                                            emailBody = GenerateCancellationEmailHtml(
                                                notification.User.FullName ?? "Değerli Müşterimiz",
                                                fromCity,
                                                toCity,
                                                departureDate,
                                                departureTime,
                                                ticketNumber,
                                                seatNumber,
                                                vehicleType,
                                                price,
                                                cancelReason,
                                                reservation.ReservationID
                                            );
                                        }
                                        else
                                        {
                                            emailSubject = notification.Subject ?? "RayBus Bildirimi";
                                            emailBody = GenerateGeneralEmailHtml(
                                                notification.User.FullName ?? "Değerli Müşterimiz",
                                                notification.Subject ?? "RayBus Bildirimi",
                                                notification.Message,
                                                fromCity,
                                                toCity,
                                                departureDate,
                                                departureTime,
                                                ticketNumber,
                                                seatNumber,
                                                vehicleType,
                                                price
                                            );
                                        }

                                        if (pdfData != null && !string.IsNullOrEmpty(pdfFileName))
                                        {
                                            success = await emailService.SendEmailWithAttachmentAsync(
                                                notification.User.Email,
                                                notification.User.FullName ?? "Kullanıcı",
                                                emailSubject,
                                                emailBody,
                                                pdfData,
                                                pdfFileName,
                                                "application/pdf",
                                                true
                                            );
                                        }
                                        else
                                        {
                                            success = await emailService.SendEmailAsync(
                                                notification.User.Email,
                                                notification.User.FullName ?? "Kullanıcı",
                                                emailSubject,
                                                emailBody,
                                                true
                                            );
                                        }

                                        if (!success)
                                        {
                                            errorMessage = "Email gönderilemedi";
                                        }
                                    }
                                    else
                                    {
                                        success = await emailService.SendEmailAsync(
                                            notification.User.Email,
                                            notification.User.FullName ?? "Kullanıcı",
                                            notification.Subject ?? "RayBus Bildirimi",
                                            notification.Message,
                                            true
                                        );

                                        if (!success)
                                        {
                                            errorMessage = "Email gönderilemedi";
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "❌ Email gönderilirken hata. NotificationID: {NotificationID}", notification.NotificationID);
                                    errorMessage = $"Email gönderilirken hata: {ex.Message}";
                                }
                            }
                            else
                            {
                                success = await emailService.SendEmailAsync(
                                    notification.User.Email,
                                    notification.User.FullName ?? "Kullanıcı",
                                    notification.Subject ?? "RayBus Bildirimi",
                                    GenerateSimpleEmailHtml(
                                        notification.User.FullName ?? "Değerli Müşterimiz",
                                        notification.Subject ?? "RayBus Bildirimi",
                                        notification.Message
                                    ),
                                    true
                                );

                                if (!success)
                                {
                                    errorMessage = "Email gönderilemedi";
                                }
                            }
                        }
                    }
                    else if (notification.NotificationMethod == "SMS")
                    {
                        _logger.LogWarning("⚠️ SMS bildirimi artık desteklenmiyor. NotificationID: {NotificationID}, Method: {Method}", 
                            notification.NotificationID, notification.NotificationMethod);
                        notification.NotificationMethod = "Email";
                        bool shouldSendEmail = (preferences == null || preferences.EmailNotifications != false) && notification.User?.Email != null;
                        
                        if (shouldSendEmail)
                        {
                            success = await emailService.SendEmailAsync(
                                notification.User.Email,
                                notification.User.FullName ?? "Kullanıcı",
                                notification.Subject ?? "RayBus Bildirimi",
                                notification.Message
                            );

                            if (!success)
                            {
                                errorMessage = "Email gönderilemedi";
                            }
                        }
                    }

                    if (success)
                    {
                        notification.Status = "Sent";
                        notification.SentAt = DateTime.UtcNow;
                        _logger.LogInformation("✅ Bildirim gönderildi. NotificationID: {NotificationID}, Method: {Method}", 
                            notification.NotificationID, notification.NotificationMethod);
                    }
                    else
                    {
                        notification.RetryCount++;
                        if (notification.RetryCount >= _maxRetryCount)
                        {
                            notification.Status = "Failed";
                            notification.ErrorMessage = errorMessage ?? "Maksimum deneme sayısına ulaşıldı";
                            _logger.LogWarning("❌ Bildirim başarısız. NotificationID: {NotificationID}, RetryCount: {RetryCount}", 
                                notification.NotificationID, notification.RetryCount);
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Bildirim gönderilemedi, tekrar denenecek. NotificationID: {NotificationID}, RetryCount: {RetryCount}", 
                                notification.NotificationID, notification.RetryCount);
                        }
                    }

                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Bildirim işlenirken hata. NotificationID: {NotificationID}", notification.NotificationID);
                    
                    notification.RetryCount++;
                    if (notification.RetryCount >= _maxRetryCount)
                    {
                        notification.Status = "Failed";
                        notification.ErrorMessage = ex.Message;
                    }
                    
                    await context.SaveChangesAsync();
                }
            }
        }

        private string GeneratePaymentEmailHtml(string userName, string fromCity, string toCity, string departureDate, string departureTime, string ticketNumber, string seatNumber, string vehicleType, decimal price)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang='tr'>");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset='UTF-8'>");
            html.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            html.AppendLine("    <title>Biletiniz Hazır - RayBus</title>");
            html.AppendLine("</head>");
            html.AppendLine("<body style='margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, \"Helvetica Neue\", Arial, sans-serif; background-color: #f5f5f5;'>");
            html.AppendLine("    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f5f5f5; padding: 40px 20px;'>");
            html.AppendLine("        <tr>");
            html.AppendLine("            <td align='center'>");
            html.AppendLine("                <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>");
            
            html.AppendLine("                    <tr>");
            html.AppendLine("                        <td style='background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%); padding: 40px 30px; text-align: center;'>");
            html.AppendLine("                            <h1 style='color: #ffffff; margin: 0; font-size: 28px; font-weight: 700;'>🎫 Biletiniz Hazır!</h1>");
            html.AppendLine("                            <p style='color: #e0e7ff; margin: 10px 0 0 0; font-size: 16px;'>RayBus ile güvenli yolculuklar</p>");
            html.AppendLine("                        </td>");
            html.AppendLine("                    </tr>");
            
            html.AppendLine("                    <tr>");
            html.AppendLine("                        <td style='padding: 40px 30px;'>");
            html.AppendLine($"                            <p style='color: #1f2937; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;'>Sayın <strong>{userName}</strong>,</p>");
            html.AppendLine("                            <p style='color: #4b5563; font-size: 15px; line-height: 1.6; margin: 0 0 30px 0;'>Biletiniz başarıyla alındı! Aşağıda sefer bilgileriniz ve bilet PDF'iniz bulunmaktadır.</p>");
            
            html.AppendLine("                            <div style='background: linear-gradient(135deg, #f0f9ff 0%, #e0e7ff 100%); border-radius: 12px; padding: 30px; margin: 30px 0; border-left: 4px solid #6366f1;'>");
            html.AppendLine("                                <h2 style='color: #1e40af; margin: 0 0 20px 0; font-size: 20px; font-weight: 600;'>📋 Sefer Bilgileri</h2>");
            html.AppendLine("                                <table width='100%' cellpadding='8' cellspacing='0'>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600; width: 140px;'>Araç Tipi:</td><td style='color: #1f2937;'>{vehicleType}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Kalkış:</td><td style='color: #1f2937;'><strong>{fromCity}</strong></td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Varış:</td><td style='color: #1f2937;'><strong>{toCity}</strong></td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Tarih:</td><td style='color: #1f2937;'>{departureDate}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Saat:</td><td style='color: #1f2937;'>{departureTime}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Koltuk:</td><td style='color: #1f2937;'>{seatNumber}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Bilet No:</td><td style='color: #6366f1; font-weight: 700; font-size: 16px;'>{ticketNumber}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Tutar:</td><td style='color: #059669; font-weight: 700; font-size: 18px;'>{price:N2} ₺</td></tr>");
            html.AppendLine("                                </table>");
            html.AppendLine("                            </div>");
            
            html.AppendLine("                            <div style='background-color: #f9fafb; border-radius: 8px; padding: 20px; margin: 30px 0; border: 1px solid #e5e7eb;'>");
            html.AppendLine("                                <p style='color: #4b5563; font-size: 14px; margin: 0 0 10px 0;'>📎 <strong>Bilet PDF'iniz</strong> bu email'in ekinde bulunmaktadır.</p>");
            html.AppendLine("                                <p style='color: #6b7280; font-size: 13px; margin: 0;'>PDF'i cihazınıza indirip seyahat gününde yanınızda bulundurmanızı öneririz.</p>");
            html.AppendLine("                            </div>");
            
            html.AppendLine("                            <p style='color: #6b7280; font-size: 14px; line-height: 1.6; margin: 30px 0 0 0;'>İyi yolculuklar dileriz! 🚌🚄</p>");
            html.AppendLine("                            <p style='color: #9ca3af; font-size: 12px; margin: 20px 0 0 0;'>Bu email otomatik olarak gönderilmiştir. Lütfen yanıtlamayınız.</p>");
            html.AppendLine("                        </td>");
            html.AppendLine("                    </tr>");
            
            html.AppendLine("                    <tr>");
            html.AppendLine("                        <td style='background-color: #1f2937; padding: 20px 30px; text-align: center;'>");
            html.AppendLine("                            <p style='color: #9ca3af; font-size: 12px; margin: 0;'>© 2024 RayBus - Tüm hakları saklıdır.</p>");
            html.AppendLine("                        </td>");
            html.AppendLine("                    </tr>");
            
            html.AppendLine("                </table>");
            html.AppendLine("            </td>");
            html.AppendLine("        </tr>");
            html.AppendLine("    </table>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private string GenerateReservationEmailHtml(string userName, string fromCity, string toCity, string departureDate, string departureTime, string ticketNumber, string seatNumber, string vehicleType, decimal price, int reservationId)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang='tr'>");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset='UTF-8'>");
            html.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            html.AppendLine("    <title>Rezervasyonunuz Oluşturuldu - RayBus</title>");
            html.AppendLine("</head>");
            html.AppendLine("<body style='margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, \"Helvetica Neue\", Arial, sans-serif; background-color: #f5f5f5;'>");
            html.AppendLine("    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f5f5f5; padding: 40px 20px;'>");
            html.AppendLine("        <tr>");
            html.AppendLine("            <td align='center'>");
            html.AppendLine("                <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>");
            
            html.AppendLine("                    <tr>");
            html.AppendLine("                        <td style='background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 40px 30px; text-align: center;'>");
            html.AppendLine("                            <h1 style='color: #ffffff; margin: 0; font-size: 28px; font-weight: 700;'>✅ Rezervasyonunuz Oluşturuldu!</h1>");
            html.AppendLine("                            <p style='color: #d1fae5; margin: 10px 0 0 0; font-size: 16px;'>RayBus ile güvenli yolculuklar</p>");
            html.AppendLine("                        </td>");
            html.AppendLine("                    </tr>");
            
            html.AppendLine("                    <tr>");
            html.AppendLine("                        <td style='padding: 40px 30px;'>");
            html.AppendLine($"                            <p style='color: #1f2937; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;'>Sayın <strong>{userName}</strong>,</p>");
            html.AppendLine("                            <p style='color: #4b5563; font-size: 15px; line-height: 1.6; margin: 0 0 30px 0;'>Rezervasyonunuz başarıyla oluşturuldu! Aşağıda sefer bilgileriniz bulunmaktadır.</p>");
            
            html.AppendLine("                            <div style='background: linear-gradient(135deg, #f0fdf4 0%, #dcfce7 100%); border-radius: 12px; padding: 30px; margin: 30px 0; border-left: 4px solid #10b981;'>");
            html.AppendLine("                                <h2 style='color: #065f46; margin: 0 0 20px 0; font-size: 20px; font-weight: 600;'>📋 Sefer Bilgileri</h2>");
            html.AppendLine("                                <table width='100%' cellpadding='8' cellspacing='0'>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600; width: 140px;'>Araç Tipi:</td><td style='color: #1f2937;'>{vehicleType}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Kalkış:</td><td style='color: #1f2937;'><strong>{fromCity}</strong></td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Varış:</td><td style='color: #1f2937;'><strong>{toCity}</strong></td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Tarih:</td><td style='color: #1f2937;'>{departureDate}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Saat:</td><td style='color: #1f2937;'>{departureTime}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Koltuk:</td><td style='color: #1f2937;'>{seatNumber}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Rezervasyon No:</td><td style='color: #10b981; font-weight: 700; font-size: 16px;'>RB-{reservationId}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Tutar:</td><td style='color: #059669; font-weight: 700; font-size: 18px;'>{price:N2} ₺</td></tr>");
            html.AppendLine("                                </table>");
            html.AppendLine("                            </div>");
            
            html.AppendLine("                            <div style='background-color: #fef3c7; border-radius: 8px; padding: 20px; margin: 30px 0; border: 1px solid #fcd34d;'>");
            html.AppendLine("                                <p style='color: #92400e; font-size: 14px; margin: 0; font-weight: 600;'>💳 Ödeme yapmak için rezervasyonlarınız sayfasını ziyaret edin.</p>");
            html.AppendLine("                            </div>");
            
            html.AppendLine("                            <p style='color: #6b7280; font-size: 14px; line-height: 1.6; margin: 30px 0 0 0;'>İyi yolculuklar dileriz! 🚌🚄</p>");
            html.AppendLine("                            <p style='color: #9ca3af; font-size: 12px; margin: 20px 0 0 0;'>Bu email otomatik olarak gönderilmiştir. Lütfen yanıtlamayınız.</p>");
            html.AppendLine("                        </td>");
            html.AppendLine("                    </tr>");
            
            html.AppendLine("                    <tr>");
            html.AppendLine("                        <td style='background-color: #1f2937; padding: 20px 30px; text-align: center;'>");
            html.AppendLine("                            <p style='color: #9ca3af; font-size: 12px; margin: 0;'>© 2024 RayBus - Tüm hakları saklıdır.</p>");
            html.AppendLine("                        </td>");
            html.AppendLine("                    </tr>");
            
            html.AppendLine("                </table>");
            html.AppendLine("            </td>");
            html.AppendLine("        </tr>");
            html.AppendLine("    </table>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private string GenerateCancellationEmailHtml(string userName, string fromCity, string toCity, string departureDate, string departureTime, string ticketNumber, string seatNumber, string vehicleType, decimal price, string cancelReason, int reservationId)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang='tr'>");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset='UTF-8'>");
            html.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            html.AppendLine("    <title>Rezervasyonunuz İptal Edildi - RayBus</title>");
            html.AppendLine("</head>");
            html.AppendLine("<body style='margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, \"Helvetica Neue\", Arial, sans-serif; background-color: #f5f5f5;'>");
            html.AppendLine("    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f5f5f5; padding: 40px 20px;'>");
            html.AppendLine("        <tr>");
            html.AppendLine("            <td align='center'>");
            html.AppendLine("                <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>");
            
            html.AppendLine("                    <tr>");
            html.AppendLine("                        <td style='background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); padding: 40px 30px; text-align: center;'>");
            html.AppendLine("                            <h1 style='color: #ffffff; margin: 0; font-size: 28px; font-weight: 700;'>❌ Rezervasyonunuz İptal Edildi</h1>");
            html.AppendLine("                            <p style='color: #fee2e2; margin: 10px 0 0 0; font-size: 16px;'>RayBus</p>");
            html.AppendLine("                        </td>");
            html.AppendLine("                    </tr>");
            
            html.AppendLine("                    <tr>");
            html.AppendLine("                        <td style='padding: 40px 30px;'>");
            html.AppendLine($"                            <p style='color: #1f2937; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;'>Sayın <strong>{userName}</strong>,</p>");
            html.AppendLine("                            <p style='color: #4b5563; font-size: 15px; line-height: 1.6; margin: 0 0 30px 0;'>Rezervasyonunuz iptal edilmiştir. Aşağıda iptal edilen sefer bilgileri bulunmaktadır.</p>");
            
            html.AppendLine("                            <div style='background: linear-gradient(135deg, #fef2f2 0%, #fee2e2 100%); border-radius: 12px; padding: 30px; margin: 30px 0; border-left: 4px solid #ef4444;'>");
            html.AppendLine("                                <h2 style='color: #991b1b; margin: 0 0 20px 0; font-size: 20px; font-weight: 600;'>📋 İptal Edilen Sefer Bilgileri</h2>");
            html.AppendLine("                                <table width='100%' cellpadding='8' cellspacing='0'>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600; width: 140px;'>Araç Tipi:</td><td style='color: #1f2937;'>{vehicleType}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Kalkış:</td><td style='color: #1f2937;'><strong>{fromCity}</strong></td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Varış:</td><td style='color: #1f2937;'><strong>{toCity}</strong></td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Tarih:</td><td style='color: #1f2937;'>{departureDate}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Saat:</td><td style='color: #1f2937;'>{departureTime}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Koltuk:</td><td style='color: #1f2937;'>{seatNumber}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Rezervasyon No:</td><td style='color: #ef4444; font-weight: 700; font-size: 16px;'>RB-{reservationId}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>İptal Nedeni:</td><td style='color: #991b1b; font-weight: 600;'>{cancelReason}</td></tr>");
            html.AppendLine("                                </table>");
            html.AppendLine("                            </div>");
            
            html.AppendLine("                            <div style='background-color: #fef3c7; border-radius: 8px; padding: 20px; margin: 30px 0; border: 1px solid #fcd34d;'>");
            html.AppendLine("                                <p style='color: #92400e; font-size: 14px; margin: 0 0 10px 0; font-weight: 600;'>💰 İade İşlemleri</p>");
            html.AppendLine("                                <p style='color: #78350f; font-size: 13px; margin: 0;'>İade işlemleri hakkında detaylı bilgi için lütfen bizimle iletişime geçin.</p>");
            html.AppendLine("                            </div>");
            
            html.AppendLine("                            <p style='color: #6b7280; font-size: 14px; line-height: 1.6; margin: 30px 0 0 0;'>Sorularınız için bizimle iletişime geçebilirsiniz.</p>");
            html.AppendLine("                            <p style='color: #9ca3af; font-size: 12px; margin: 20px 0 0 0;'>Bu email otomatik olarak gönderilmiştir. Lütfen yanıtlamayınız.</p>");
            html.AppendLine("                        </td>");
            html.AppendLine("                    </tr>");
            
            html.AppendLine("                    <tr>");
            html.AppendLine("                        <td style='background-color: #1f2937; padding: 20px 30px; text-align: center;'>");
            html.AppendLine("                            <p style='color: #9ca3af; font-size: 12px; margin: 0;'>© 2024 RayBus - Tüm hakları saklıdır.</p>");
            html.AppendLine("                        </td>");
            html.AppendLine("                    </tr>");
            
            html.AppendLine("                </table>");
            html.AppendLine("            </td>");
            html.AppendLine("        </tr>");
            html.AppendLine("    </table>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private string GenerateGeneralEmailHtml(string userName, string subject, string message, string fromCity, string toCity, string departureDate, string departureTime, string ticketNumber, string seatNumber, string vehicleType, decimal price)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang='tr'>");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset='UTF-8'>");
            html.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            html.AppendLine($"    <title>{subject} - RayBus</title>");
            html.AppendLine("</head>");
            html.AppendLine("<body style='margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, \"Helvetica Neue\", Arial, sans-serif; background-color: #f5f5f5;'>");
            html.AppendLine("    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f5f5f5; padding: 40px 20px;'>");
            html.AppendLine("        <tr>");
            html.AppendLine("            <td align='center'>");
            html.AppendLine("                <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>");
            
            html.AppendLine("                    <tr>");
            html.AppendLine("                        <td style='background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%); padding: 40px 30px; text-align: center;'>");
            html.AppendLine($"                            <h1 style='color: #ffffff; margin: 0; font-size: 28px; font-weight: 700;'>{subject}</h1>");
            html.AppendLine("                            <p style='color: #e0e7ff; margin: 10px 0 0 0; font-size: 16px;'>RayBus</p>");
            html.AppendLine("                        </td>");
            html.AppendLine("                    </tr>");
            
            html.AppendLine("                    <tr>");
            html.AppendLine("                        <td style='padding: 40px 30px;'>");
            html.AppendLine($"                            <p style='color: #1f2937; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;'>Sayın <strong>{userName}</strong>,</p>");
            html.AppendLine($"                            <p style='color: #4b5563; font-size: 15px; line-height: 1.6; margin: 0 0 30px 0; white-space: pre-line;'>{message}</p>");
            
            html.AppendLine("                            <div style='background: linear-gradient(135deg, #f0f9ff 0%, #e0e7ff 100%); border-radius: 12px; padding: 30px; margin: 30px 0; border-left: 4px solid #6366f1;'>");
            html.AppendLine("                                <h2 style='color: #1e40af; margin: 0 0 20px 0; font-size: 20px; font-weight: 600;'>📋 Sefer Bilgileri</h2>");
            html.AppendLine("                                <table width='100%' cellpadding='8' cellspacing='0'>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600; width: 140px;'>Araç Tipi:</td><td style='color: #1f2937;'>{vehicleType}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Kalkış:</td><td style='color: #1f2937;'><strong>{fromCity}</strong></td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Varış:</td><td style='color: #1f2937;'><strong>{toCity}</strong></td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Tarih:</td><td style='color: #1f2937;'>{departureDate}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Saat:</td><td style='color: #1f2937;'>{departureTime}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Koltuk:</td><td style='color: #1f2937;'>{seatNumber}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Bilet No:</td><td style='color: #6366f1; font-weight: 700; font-size: 16px;'>{ticketNumber}</td></tr>");
            html.AppendLine($"                                    <tr><td style='color: #4b5563; font-weight: 600;'>Tutar:</td><td style='color: #059669; font-weight: 700; font-size: 18px;'>{price:N2} ₺</td></tr>");
            html.AppendLine("                                </table>");
            html.AppendLine("                            </div>");
            
            html.AppendLine("                            <p style='color: #6b7280; font-size: 14px; line-height: 1.6; margin: 30px 0 0 0;'>İyi yolculuklar dileriz! 🚌🚄</p>");
            html.AppendLine("                            <p style='color: #9ca3af; font-size: 12px; margin: 20px 0 0 0;'>Bu email otomatik olarak gönderilmiştir. Lütfen yanıtlamayınız.</p>");
            html.AppendLine("                        </td>");
            html.AppendLine("                    </tr>");
            
            html.AppendLine("                    <tr>");
            html.AppendLine("                        <td style='background-color: #1f2937; padding: 20px 30px; text-align: center;'>");
            html.AppendLine("                            <p style='color: #9ca3af; font-size: 12px; margin: 0;'>© 2024 RayBus - Tüm hakları saklıdır.</p>");
            html.AppendLine("                        </td>");
            html.AppendLine("                    </tr>");
            
            html.AppendLine("                </table>");
            html.AppendLine("            </td>");
            html.AppendLine("        </tr>");
            html.AppendLine("    </table>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private string GenerateSimpleEmailHtml(string userName, string subject, string message)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang='tr'>");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset='UTF-8'>");
            html.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            html.AppendLine($"    <title>{subject} - RayBus</title>");
            html.AppendLine("</head>");
            html.AppendLine("<body style='margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, \"Helvetica Neue\", Arial, sans-serif; background-color: #f5f5f5;'>");
            html.AppendLine("    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f5f5f5; padding: 40px 20px;'>");
            html.AppendLine("        <tr>");
            html.AppendLine("            <td align='center'>");
            html.AppendLine("                <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>");
            
            html.AppendLine("                    <tr>");
            html.AppendLine("                        <td style='background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%); padding: 40px 30px; text-align: center;'>");
            html.AppendLine($"                            <h1 style='color: #ffffff; margin: 0; font-size: 28px; font-weight: 700;'>{subject}</h1>");
            html.AppendLine("                            <p style='color: #e0e7ff; margin: 10px 0 0 0; font-size: 16px;'>RayBus</p>");
            html.AppendLine("                        </td>");
            html.AppendLine("                    </tr>");
            
            html.AppendLine("                    <tr>");
            html.AppendLine("                        <td style='padding: 40px 30px;'>");
            html.AppendLine($"                            <p style='color: #1f2937; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;'>Sayın <strong>{userName}</strong>,</p>");
            html.AppendLine($"                            <p style='color: #4b5563; font-size: 15px; line-height: 1.6; margin: 0 0 30px 0; white-space: pre-line;'>{message}</p>");
            
            // Footer
            html.AppendLine("                            <p style='color: #6b7280; font-size: 14px; line-height: 1.6; margin: 30px 0 0 0;'>İyi yolculuklar dileriz! 🚌🚄</p>");
            html.AppendLine("                            <p style='color: #9ca3af; font-size: 12px; margin: 20px 0 0 0;'>Bu email otomatik olarak gönderilmiştir. Lütfen yanıtlamayınız.</p>");
            html.AppendLine("                        </td>");
            html.AppendLine("                    </tr>");
            
            html.AppendLine("                    <tr>");
            html.AppendLine("                        <td style='background-color: #1f2937; padding: 20px 30px; text-align: center;'>");
            html.AppendLine("                            <p style='color: #9ca3af; font-size: 12px; margin: 0;'>© 2024 RayBus - Tüm hakları saklıdır.</p>");
            html.AppendLine("                        </td>");
            html.AppendLine("                    </tr>");
            
            html.AppendLine("                </table>");
            html.AppendLine("            </td>");
            html.AppendLine("        </tr>");
            html.AppendLine("    </table>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }
    }
}

