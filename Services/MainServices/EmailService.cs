using Microsoft.AspNetCore.Authorization;
using ShuttlOps.DTOs;
using ShuttlOps.Models;
using ShuttlOps.Services.Interfaces;
using System.Net;
using System.Net.Mail;
using DotNetEnv;

namespace ShuttlOps.Services.MainServices
{
    public class EmailService(
        ShuttlOpsDbContext dbContext,
        ILogger<EmailService> logger,
        IConfiguration configuration) : IEmailService
    {
        public async Task SendAutoEmailNotification(EmailDTO email)
        {
            Env.Load();

            string? _smtpHost = Environment.GetEnvironmentVariable("Email__SmtpHost");
            string? _smtpUser = Environment.GetEnvironmentVariable("Email__SmtpUser");
            int _smtpPort = int.Parse(Environment.GetEnvironmentVariable("Email__SmtpPort") ?? "587");
            string? _smtpPass = Environment.GetEnvironmentVariable("Email__SmtpPassword");
            string? _fromAddress = Environment.GetEnvironmentVariable("Email__FromAddress");
            string? _fromName = Environment.GetEnvironmentVariable("Email__FromName");


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
