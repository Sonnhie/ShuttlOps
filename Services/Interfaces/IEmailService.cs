using ShuttlOps.DTOs;

namespace ShuttlOps.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendAutoEmailNotification(EmailDTO email);
    }
}
