using Microsoft.EntityFrameworkCore;

namespace PersonalFinance.Web.Services;

public sealed class RecurringExpenseWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<RecurringExpenseWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<RecurringExpenseProcessor>();
                var created = await processor.CreateDueExpensesAsync(DateOnly.FromDateTime(DateTime.Today), stoppingToken);
                if (created > 0)
                {
                    logger.LogInformation("Created {ExpenseCount} due recurring expense entries.", created);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (DbUpdateException exception)
            {
                logger.LogError(exception, "Could not create due recurring expense entries; will retry on the next cycle.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Recurring expense processing failed; will retry on the next cycle.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
