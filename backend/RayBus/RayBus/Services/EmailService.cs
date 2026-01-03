using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RayBus.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly bool _enabled;
        private readonly bool _testMode;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly bool _useSSL;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            _enabled = _configuration.GetValue<bool>("Email:Enabled", true);
            _testMode = _configuration.GetValue<bool>("Email:TestMode", false);
            _smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
            _smtpUsername = _configuration["Email:SmtpUsername"] ?? string.Empty;
            _smtpPassword = _configuration["Email:SmtpPassword"] ?? string.Empty;
            _fromEmail = _configuration["Email:FromEmail"] ?? "noreply@raybus.com";
            _fromName = _configuration["Email:FromName"] ?? "RayBus";
            _useSSL = _configuration.GetValue<bool>("Email:UseSSL", true);

            // Başlangıç log'u
            if (_enabled)
            {
                if (_testMode)
                {
                    _logger.LogWarning("⚠️ Email Service: TEST MODE aktif. Email'ler gönderilmeyecek, sadece log'a yazılacak.");
                }
                else if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
                {
                    _logger.LogError("❌ Email Service: ENABLED ama SMTP bilgileri eksik! Email gönderilmeyecek.");
                }
                else
                {
                    _logger.LogInformation("✅ Email Service: Aktif ve hazır. SMTP: {Server}:{Port}, From: {FromEmail}", 
                        _smtpServer, _smtpPort, _fromEmail);
                }
            }
            else
            {
                _logger.LogInformation("📧 Email Service: Devre dışı.");
            }
        }

        public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string body, bool isHtml = true)
        {
            if (!_enabled)
            {
                _logger.LogInformation("📧 Email servisi devre dışı, email gönderilmedi. To: {ToEmail}, Subject: {Subject}", toEmail, subject);
                return true; // Servis devre dışı ama başarılı sayıyoruz
            }

            // Test modu: API key yoksa sadece loglama yap
            if (_testMode || string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
            {
                _logger.LogInformation("📧 [TEST MODE] Email gönderilecekti. To: {ToEmail}, ToName: {ToName}, Subject: {Subject}", 
                    toEmail, toName, subject);
                _logger.LogInformation("📧 [TEST MODE] Email Body: {Body}", body);
                return true; // Test modunda başarılı sayıyoruz
            }

            try
            {
                using var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(_fromEmail, _fromName);
                mailMessage.To.Add(new MailAddress(toEmail, toName));
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = isHtml;

                using var smtpClient = new SmtpClient(_smtpServer, _smtpPort);
                smtpClient.EnableSsl = _useSSL;
                smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                smtpClient.Timeout = 30000; // 30 saniye

                await smtpClient.SendMailAsync(mailMessage);
                
                _logger.LogInformation("✅ Email başarıyla gönderildi. To: {ToEmail}, Subject: {Subject}", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Email gönderilirken hata. To: {ToEmail}, Subject: {Subject}", toEmail, subject);
                return false;
            }
        }

        public async Task<bool> SendEmailWithAttachmentAsync(string toEmail, string toName, string subject, string body, byte[] attachmentData, string attachmentFileName, string attachmentContentType = "application/pdf", bool isHtml = true)
        {
            if (!_enabled)
            {
                _logger.LogInformation("📧 Email servisi devre dışı, email gönderilmedi. To: {ToEmail}, Subject: {Subject}, Attachment: {FileName}", 
                    toEmail, subject, attachmentFileName);
                return true;
            }

            // Test modu: API key yoksa sadece loglama yap
            if (_testMode || string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
            {
                _logger.LogInformation("📧 [TEST MODE] Email gönderilecekti (PDF eklentili). To: {ToEmail}, ToName: {ToName}, Subject: {Subject}, Attachment: {FileName}, Size: {Size} bytes", 
                    toEmail, toName, subject, attachmentFileName, attachmentData?.Length ?? 0);
                _logger.LogInformation("📧 [TEST MODE] Email Body: {Body}", body);
                return true;
            }

            Attachment? attachment = null;
            try
            {
                using var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(_fromEmail, _fromName);
                mailMessage.To.Add(new MailAddress(toEmail, toName));
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = isHtml;

                // PDF eklentisi ekle
                if (attachmentData != null && attachmentData.Length > 0)
                {
                    // MemoryStream'i Attachment'a ver, Attachment stream'i yönetecek
                    var attachmentStream = new MemoryStream(attachmentData);
                    attachment = new Attachment(attachmentStream, attachmentFileName, attachmentContentType);
                    mailMessage.Attachments.Add(attachment);
                }

                using var smtpClient = new SmtpClient(_smtpServer, _smtpPort);
                smtpClient.EnableSsl = _useSSL;
                smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                smtpClient.Timeout = 60000; // 60 saniye (PDF için daha uzun)

                await smtpClient.SendMailAsync(mailMessage);
                
                _logger.LogInformation("✅ Email başarıyla gönderildi (PDF eklentili). To: {ToEmail}, Subject: {Subject}, Attachment: {FileName}", 
                    toEmail, subject, attachmentFileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Email gönderilirken hata (PDF eklentili). To: {ToEmail}, Subject: {Subject}", toEmail, subject);
                return false;
            }
            finally
            {
                // Attachment'ı dispose et (içindeki MemoryStream de dispose edilecek)
                attachment?.Dispose();
            }
        }
    }
}

