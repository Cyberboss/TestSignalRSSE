
using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestSignalRSSE
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("This is a server/client pair app:");
            bool server = false;
            do
            {
                Console.WriteLine();
                Console.Write("Running as server? (y/n):");
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Y)
                {
                    server = true;
                    break;
                }
                else if (key.Key == ConsoleKey.N)
                    break;
            } while (true);

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                cts?.Cancel();
                e.Cancel = true;
            };

            if (server)
                await new Program().RunServer(cts.Token);
            else
                await RunClient(cts.Token);
        }

        private static async Task RunClient(CancellationToken cancellationToken)
        {
            Console.WriteLine();
            Console.Write("Enter server address: ");
            var address = Console.ReadLine();
            var url = new Uri(address.TrimEnd('/') + "/hub");

            await using var hub = new HubConnectionBuilder()
                .WithAutomaticReconnect()
                .WithUrl(url, HttpTransportType.ServerSentEvents)
                .Build();

            hub.On(nameof(IHubMethod.MessageX), (string message) =>
            {
                Console.WriteLine($"Received: {message}");
            });

            await hub.StartAsync(cancellationToken);

            await new TaskCompletionSource().Task.WaitAsync(cancellationToken);
        }

        async Task RunServer(CancellationToken cancellationToken)
        {
            Console.WriteLine();
            Console.Write("Enter port: ");
            var portStr = Console.ReadLine();
            var port = ushort.Parse(portStr);

            using var host = Host.CreateDefaultBuilder()
                .ConfigureWebHost(builder =>
                {
                    builder.UseKestrel(kestrelOptions =>
                    {
                        kestrelOptions.ListenAnyIP(
                            port,
                            listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
                    });
                    builder.ConfigureServices(services =>
                    {
                        services.AddSignalR();
                        services.AddHostedService<HubMessenger>();
                        services.AddCors();
                    });
                    builder.Configure(appBuilder =>
                    {
                        appBuilder.UseFileServer();
                        appBuilder.UseRouting();
                        appBuilder.UseCors(builder => builder
                            .SetIsOriginAllowed(_ => true)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials()
                            .SetPreflightMaxAge(TimeSpan.FromDays(1)));
                        appBuilder.UseEndpoints(endpoints =>
                        {
                            // access to the signalR jobs hub
                            endpoints.MapHub<TestHub>(
                                "/hub",
                                options =>
                                {
                                    options.Transports = HttpTransportType.ServerSentEvents;
                                });
                        });
                    });
                })
                .Build();

            await host.RunAsync(cancellationToken);
        }
    }
}
