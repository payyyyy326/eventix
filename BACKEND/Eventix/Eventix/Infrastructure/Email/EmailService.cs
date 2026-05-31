using Eventix.Common.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Eventix.Infrastructure.Email
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = body
            };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Log exception in real scenario
                Console.WriteLine($"[EMAIL ERROR] Failed to send email to {to}: {ex.Message}");
                throw;
            }
        }

        public async Task SendOtpEmailAsync(string to, string otpCode)
        {
            var subject = "Your Eventix Verification Code";
            var body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 5px;'>
                    <h2 style='color: #007bff;'>Welcome to Eventix!</h2>
                    <p>Thank you for registering. Please use the following code to verify your email address:</p>
                    <div style='font-size: 24px; font-weight: bold; letter-spacing: 5px; padding: 10px; background: #f8f9fa; text-align: center; border-radius: 5px; margin: 20px 0;'>
                        {otpCode}
                    </div>
                    <p>This code will expire in 5 minutes.</p>
                    <p>If you didn't request this, please ignore this email.</p>
                </div>";

            await SendEmailAsync(to, subject, body);
        }
    }
}
