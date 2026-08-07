namespace ShuttlOps.Services
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddMyAppServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthenticationservice, Authenticationservice>();
            services.AddScoped<IRequestService, RequestService>();
            services.AddScoped<IAdminservice, AdminService>();

            return services;
        }
    }
}
