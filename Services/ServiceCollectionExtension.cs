namespace ShuttlOps.Services
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddMyAppServices(this IServiceCollection services)
        {
            //services.AddScoped<AuthenticationServices>();
            //services.AddScoped<UserManagementService>();
            //services.AddScoped<AdminManagementService>();

            return services;
        }
    }
}
