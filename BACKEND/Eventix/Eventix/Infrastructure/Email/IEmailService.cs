namespace Eventix.Infrastructure.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendEmailWithInlineImagesAsync(
            string to,
            string subject,
            string body,
            IReadOnlyDictionary<string, byte[]> inlineImages);
        Task SendOtpEmailAsync(string to, string otpCode);
    }
}
