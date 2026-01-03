namespace RayBus.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string body, bool isHtml = true);
        Task<bool> SendEmailWithAttachmentAsync(string toEmail, string toName, string subject, string body, byte[] attachmentData, string attachmentFileName, string attachmentContentType = "application/pdf", bool isHtml = true);
    }
}

