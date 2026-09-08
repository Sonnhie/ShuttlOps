using Microsoft.AspNetCore.Authorization;
using ShuttlOps.DTOs;
using ShuttlOps.Models;
using ShuttlOps.Services.Interfaces;
using System.Net;
using System.Net.Mail;

namespace ShuttlOps.Services.MainServices
{
    public class EmailService(
        ShuttlOpsDbContext dbContext,
        ILogger<EmailService> logger,
        IConfiguration configuration) : IEmailService
    {
        private readonly string _smtpHost = configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        private readonly int _smtpPort = int.TryParse(configuration["Email:SmtpPort"], out var port) ? port : 587;
        private readonly string _smtpUser = configuration["Email:SmtpUser"] ?? "";
        private readonly string _smtpPass = configuration["Email:SmtpPassword"] ?? "";
        private readonly string _fromAddress = configuration["Email:FromAddress"] ?? "noreply@shuttlops.com";
        private readonly string _fromName = configuration["Email:FromName"] ?? "ShuttlOps";

        public async Task SendAutoEmailNotification(EmailDTO email)
        {
            if (email == null || email.EmailRecipients == null || !email.EmailRecipients.Any())
            {
                logger.LogWarning("SendAutoEmailNotification called with null or empty recipients.");
                return;
            }

            try
            {
                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromAddress, _fromName),
                    Subject = email.Subject,
                    Body = email.Body,
                    IsBodyHtml = email.IsHtml
                };

                foreach (var recipient in email.EmailRecipients)
                {
                    if (!string.IsNullOrWhiteSpace(recipient))
                    {
                        mailMessage.To.Add(recipient.Trim());
                    }
                }

                if (!mailMessage.To.Any())
                {
                    logger.LogWarning("No valid recipients after filtering.");
                    return;
                }


                using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
                {
                    UseDefaultCredentials =false,
                    Credentials = new NetworkCredential(_smtpUser, _smtpPass),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 30000
                };

                await smtpClient.SendMailAsync(mailMessage);
                logger.LogInformation("Email sent successfully to {Count} recipients.", mailMessage.To.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send email notification.");
            }
        }
    }
}
