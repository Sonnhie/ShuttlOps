using ShuttlOps.Services.Interfaces;
using ShuttlOps.Services.MainServices;

namespace ShuttlOps.Services
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddMyAppServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthenticationservice, Authenticationservice>();
            services.AddScoped<IRequestService, RequestService>();
            services.AddScoped<IAdminservice, AdminService>();
            services.AddScoped<IGAService, GAServices>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IEmailService, EmailService>();
            return services;
        }
    }
}
