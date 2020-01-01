using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Services.Application;

namespace Ticketbooth.Scanner.Services.Background
{
    public class HealthMonitor : IHostedService, IDisposable
    {
        private readonly IHealthChecker _healthChecker;
        private readonly ILogger<HealthMonitor> _logger;

        private Timer _timer;

        public HealthMonitor(IHealthChecker healthChecker, ILogger<HealthMonitor> logger)
        {
            _healthChecker = healthChecker;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Health monitor started.");

            _timer = new Timer(PollNodeHealthAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(3));

            return Task.CompletedTask;
        }

        private async void PollNodeHealthAsync(object state)
        {
            await _healthChecker.UpdateNodeHealthAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Health monitor stopped.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
