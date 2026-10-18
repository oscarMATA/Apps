using Microsoft.EntityFrameworkCore;
using PersonalFinance.Web.Data;
using PersonalFinance.Web.Data.Entities;

namespace PersonalFinance.Web.Services;

/// <summary>Persists each monthly budget threshold crossing once; email delivery is handled in Phase 8.</summary>
public sealed class BudgetThresholdProcessor(FinanceDbContext database)
{
    public async Task<int> RecordCrossingsAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        var paymentLimits = await database.PaymentTypeMonthlyLimits
            .Include(item => item.AlertDeliveries)
            .Where(item => item.Year == today.Year && item.Month == today.Month)
            .ToListAsync(cancellationToken);
        var categoryBudgets = await database.CategoryMonthlyBudgets
            .Include(item => item.AlertDeliveries)
            .Where(item => item.Year == today.Year && item.Month == today.Month)
            .ToListAsync(cancellationToken);

        if (paymentLimits.Count == 0 && categoryBudgets.Count == 0) return 0;

        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var byPaymentType = await database.Expenses.AsNoTracking()
            .Where(item => item.ExpenseDate >= monthStart && item.ExpenseDate < nextMonth)
            .GroupBy(item => item.PaymentType)
            .Select(group => new { PaymentType = group.Key, Spent = group.Sum(item => item.AmountInCentavos) })
            .ToDictionaryAsync(item => item.PaymentType, item => item.Spent, cancellationToken);
        var byCategory = await database.Expenses.AsNoTracking()
            .Where(item => item.ExpenseDate >= monthStart && item.ExpenseDate < nextMonth)
            .GroupBy(item => item.CategoryId)
            .Select(group => new { CategoryId = group.Key, Spent = group.Sum(item => item.AmountInCentavos) })
            .ToDictionaryAsync(item => item.CategoryId, item => item.Spent, cancellationToken);

        var created = 0;
        foreach (var limit in paymentLimits)
        {
            var spent = byPaymentType.GetValueOrDefault(limit.PaymentType);
            created += AddMissingDeliveries(
                limit.AlertDeliveries.Select(item => item.ThresholdPercent),
                BudgetThresholdRules.GetCrossedThresholds(spent, limit.AmountInCentavos),
                threshold => new BudgetAlertDelivery
                {
                    PaymentTypeMonthlyLimitId = limit.Id,
                    ThresholdPercent = threshold
                });
        }

        foreach (var budget in categoryBudgets)
        {
            var spent = byCategory.GetValueOrDefault(budget.CategoryId);
            created += AddMissingDeliveries(
                budget.AlertDeliveries.Select(item => item.ThresholdPercent),
                BudgetThresholdRules.GetCrossedThresholds(spent, budget.AmountInCentavos),
                threshold => new BudgetAlertDelivery
                {
                    CategoryMonthlyBudgetId = budget.Id,
                    ThresholdPercent = threshold
                });
        }

        if (created > 0) await database.SaveChangesAsync(cancellationToken);
        return created;
    }

    private int AddMissingDeliveries(
        IEnumerable<int> existingThresholds,
        IEnumerable<int> crossedThresholds,
        Func<int, BudgetAlertDelivery> create)
    {
        var existing = existingThresholds.ToHashSet();
        var created = 0;
        foreach (var threshold in crossedThresholds)
        {
            if (existing.Add(threshold))
            {
                database.BudgetAlertDeliveries.Add(create(threshold));
                created++;
            }
        }
        return created;
    }
}
