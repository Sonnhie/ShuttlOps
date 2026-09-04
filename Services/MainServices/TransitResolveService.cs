using Microsoft.EntityFrameworkCore;
using ShuttlOps.Models;

namespace ShuttlOps.Services.MainServices
{
    public class TransitResolveService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TransitResolveService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

        public TransitResolveService(IServiceProvider serviceProvider, ILogger<TransitResolveService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ShuttlOpsDbContext>();

                    var now = DateTime.Now;
                    var today = DateOnly.FromDateTime(now);
                    var currentTime = TimeOnly.FromDateTime(now);

                    var dispatchesToTransit = await dbContext.DispatchDetails
                        .Include(d => d.Driver)
                        .Include(d => d.Vehicle)
                        .Include(d => d.Ticket)
                        .Where(d => d.Driver != null && d.Driver.Status == "Assigned"
                                 && d.Vehicle != null && d.Vehicle.Status == "Assigned"
                                 && d.Ticket.ApprovalStatus == "Ready for Dispatch"
                                 && d.Ticket.DateOfTrip <= today
                                 && d.Ticket.EstDepartureTime <= currentTime)
                        .ToListAsync(stoppingToken);

                    if (dispatchesToTransit.Count > 0)
                    {
                        foreach (var d in dispatchesToTransit)
                        {
                            d.Driver!.Status = "In Transit";
                            d.Vehicle!.Status = "In Transit";
                            d.VehicleStatus = "In Transit";
                            d.Ticket.ApprovalStatus = "In Transit";
                            d.Ticket.UpdatedAt = now;
                        }

                        await dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Transitioned {Count} dispatches to In Transit", dispatchesToTransit.Count);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error in TransitStatusResolver");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
