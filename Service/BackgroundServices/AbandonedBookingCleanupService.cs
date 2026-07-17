using Core.Interfaces.ServicesInterface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Service.BackgroundServices
{
    /// <summary>
    /// ✅ UPDATED: Cleanup service now only handles truly abandoned bookings
    /// Since webhook immediately cancels failed payments, this is a safety net
    /// </summary>
    public class AbandonedBookingCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AbandonedBookingCleanupService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(10);
        private readonly int _timeoutMinutes = 60; // ✅ INCREASED: Cancel after 60 minutes (safety net only)

        public AbandonedBookingCleanupService(
            IServiceProvider serviceProvider,
            ILogger<AbandonedBookingCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🧹 Abandoned Booking Cleanup Service started");

            // Wait 2 minutes before first run to let app initialize
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupAbandonedBookings();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("🛑 Abandoned Booking Cleanup Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error in Abandoned Booking Cleanup Service");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("✅ Abandoned Booking Cleanup Service stopped");
        }

        private async Task CleanupAbandonedBookings()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                    _logger.LogDebug("🔍 Checking for abandoned bookings...");

                    var cancelledCount = await bookingService.CancelAbandonedBookingsAsync(_timeoutMinutes);

                    if (cancelledCount > 0)
                    {
                        _logger.LogWarning($"🧹 Cancelled {cancelledCount} abandoned booking(s) (safety net triggered)");
                    }
                    else
                    {
                        _logger.LogDebug("✅ No abandoned bookings found");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during abandoned bookings cleanup");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🛑 Abandoned Booking Cleanup Service is stopping...");
            await base.StopAsync(cancellationToken);
        }
    }
}