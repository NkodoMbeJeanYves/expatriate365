using Microsoft.Extensions.Options;
using server.Infrastructure.Services;

namespace server.Infrastructure.BackgroundServices;

public sealed class EmailOutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOutboxOptions> options,
    ILogger<EmailOutboxDispatcher> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            log.LogInformation("Email outbox dispatcher disabled by configuration");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Clamp(options.Value.PollSeconds, 5, 3600)));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<EmailOutboxProcessor>();
                var processed = await processor.ProcessBatchAsync(stoppingToken);
                if (processed > 0)
                    log.LogDebug("Email outbox dispatcher processed {Count} messages", processed);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                log.LogError(exception, "Email outbox dispatch cycle failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
