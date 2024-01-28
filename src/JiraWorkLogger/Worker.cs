using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JiraWorkLogger;

public class Worker(
    ILogger<Worker> logger,
    IHostApplicationLifetime hostApplicationLifetime,
    IServiceScopeFactory serviceScopeFactory)
    : IHostedService
{
    private readonly CancellationTokenSource stoppingCts = new();

    public Task StartAsync(CancellationToken startAbortedCt)
    {
        hostApplicationLifetime.ApplicationStarted.Register(() => OnStarted(startAbortedCt));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        stoppingCts.Cancel();
        return Task.CompletedTask;
    }

    private void OnStarted(CancellationToken startAbortedCt)
    {
        var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(startAbortedCt, stoppingCts.Token);
        var ct = combinedCts.Token;
        
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var workLogger = scope.ServiceProvider.GetRequiredService<WorkLogger>();
                await workLogger.Run();
                
                logger.LogInformation("Finished");
            }
            catch (Exception ex)
            {
                Environment.ExitCode = 1;
                logger.LogError(ex, "Unhandled exception has occurred");
            }

            hostApplicationLifetime.StopApplication();
        }, ct);
    }
}