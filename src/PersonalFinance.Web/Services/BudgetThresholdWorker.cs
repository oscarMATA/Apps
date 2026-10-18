using Microsoft.EntityFrameworkCore;

namespace PersonalFinance.Web.Services;

public sealed class BudgetThresholdWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<BudgetThresholdWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<BudgetThresholdProcessor>();
                var recorded = await processor.RecordCrossingsAsync(DateOnly.FromDateTime(DateTime.Today), stoppingToken);
                if (recorded > 0)
                {
                    logger.LogInformation("Recorded {ThresholdCount} monthly budget threshold crossings.", recorded);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (DbUpdateException exception)
            {
                logger.LogError(exception, "Could not record budget threshold crossings; will retry on the next cycle.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Budget threshold processing failed; will retry on the next cycle.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
