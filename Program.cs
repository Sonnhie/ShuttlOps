using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ShuttlOps.Hubs;
using ShuttlOps.Models;
using ShuttlOps.Services;
using ShuttlOps.Services.MainServices;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddRazorPages()
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.AddAuthentication("Login")
    .AddCookie("Login", options =>
    {
        options.Cookie.Name = "Login";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddDbContext<ShuttlOpsDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();
builder.Services.AddMyAppServices();
//builder.Services.AddHostedService<TransitResolveService>();
builder.Services.AddControllersWithViews();

builder.Services.AddAntiforgery(options =>
{
    // ⚡ Tells ASP.NET Core to look for the token in HTTP Headers
    options.HeaderName = "RequestVerificationToken";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>("/Hubs/NotificationsHubs");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
