using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestSignalRSSE
{
    internal class HubMessenger : BackgroundService
    {
        readonly IHubContext<TestHub, IHubMethod> hub;
        readonly ILogger<HubMessenger> logger;

        public HubMessenger(IHubContext<TestHub, IHubMethod> hub, ILogger<HubMessenger> logger)
        {
            this.hub = hub ?? throw new ArgumentNullException(nameof(hub));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Starting...");

            ulong iteration = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                var message = $"Iteration #{++iteration}";
                logger.LogInformation("Sending {message}...", message);
                await hub.Clients.All.MessageX(message, stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}