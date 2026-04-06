using System;
using System.Threading;
using System.Threading.Tasks;
using APICore.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace APICore.API.HostedServices
{
    /// <summary>
    /// Runs <see cref="ISubscriptionService.CheckAndExpireSubscriptionsAsync"/> on a timer so paid
    /// subscriptions move to <c>expired</c> and organizations deactivate when <c>EndDate</c> passes.
    /// </summary>
    public sealed class SubscriptionExpirationHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionExpirationHostedService> _logger;
        private readonly IConfiguration _configuration;

        public SubscriptionExpirationHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionExpirationHostedService> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_configuration.GetValue("SubscriptionExpiration:Enabled", true))
            {
                _logger.LogInformation("Subscription expiration background check is disabled (SubscriptionExpiration:Enabled).");
                return;
            }

            var minutes = _configuration.GetValue("SubscriptionExpiration:CheckIntervalMinutes", 60);
            if (minutes < 1)
                minutes = 60;

            var interval = TimeSpan.FromMinutes(minutes);
            _logger.LogInformation("Subscription expiration check runs every {Interval} minutes.", minutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                    await subscriptionService.CheckAndExpireSubscriptionsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Subscription expiration check failed.");
                }

                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
    }
}
