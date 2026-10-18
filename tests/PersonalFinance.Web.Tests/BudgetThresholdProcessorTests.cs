using Microsoft.EntityFrameworkCore;
using PersonalFinance.Web.Data.Entities;
using PersonalFinance.Web.Services;

namespace PersonalFinance.Web.Tests;

public sealed class BudgetThresholdProcessorTests
{
    [Fact]
    public async Task Aggregates_all_payment_types_for_categories_but_keeps_type_limits_separate()
    {
        await using var database = await SqliteFinanceTestDatabase.CreateAsync();
        var category = new Category { Name = "Food" };
        database.Context.Categories.Add(category);
        await database.Context.SaveChangesAsync();
        var month = new DateOnly(2026, 10, 1);

        database.Context.PaymentTypeMonthlyLimits.AddRange(
            NewPaymentLimit(PaymentType.Cash, month, 10_000),
            NewPaymentLimit(PaymentType.CreditCard, month, 10_000));
        database.Context.CategoryMonthlyBudgets.Add(new CategoryMonthlyBudget
        {
            CategoryId = category.Id,
            Year = month.Year,
            Month = month.Month,
            AmountInCentavos = 20_000
        });
        database.Context.Expenses.AddRange(
            NewExpense(category.Id, month, PaymentType.Cash, 9_000),
            NewExpense(category.Id, month.AddDays(1), PaymentType.CreditCard, 8_000));
        await database.Context.SaveChangesAsync();

        var created = await new BudgetThresholdProcessor(database.Context)
            .RecordCrossingsAsync(new DateOnly(2026, 10, 17));

        var events = await database.Context.BudgetAlertDeliveries.AsNoTracking().ToListAsync();
        Assert.Equal(2, created);
        Assert.Contains(events, item => item.PaymentTypeMonthlyLimitId is not null && item.ThresholdPercent == 85);
        Assert.Contains(events, item => item.CategoryMonthlyBudgetId is not null && item.ThresholdPercent == 85);
        Assert.DoesNotContain(events, item => item.ThresholdPercent == 100);
        Assert.Equal(0, await new BudgetThresholdProcessor(database.Context).RecordCrossingsAsync(new DateOnly(2026, 10, 17)));
    }

    [Fact]
    public async Task One_large_expense_records_both_thresholds_once()
    {
        await using var database = await SqliteFinanceTestDatabase.CreateAsync();
        var category = new Category { Name = "Electronics" };
        database.Context.Categories.Add(category);
        await database.Context.SaveChangesAsync();
        var month = new DateOnly(2026, 10, 1);
        database.Context.PaymentTypeMonthlyLimits.Add(NewPaymentLimit(PaymentType.Debit, month, 10_000));
        database.Context.Expenses.Add(NewExpense(category.Id, month, PaymentType.Debit, 12_000));
        await database.Context.SaveChangesAsync();

        var created = await new BudgetThresholdProcessor(database.Context)
            .RecordCrossingsAsync(new DateOnly(2026, 10, 17));

        var events = await database.Context.BudgetAlertDeliveries
            .Where(item => item.PaymentTypeMonthlyLimitId != null)
            .OrderBy(item => item.ThresholdPercent)
            .Select(item => item.ThresholdPercent)
            .ToListAsync();
        Assert.Equal(2, created);
        Assert.Equal([85, 100], events);
    }

    [Fact]
    public async Task Repeated_checks_after_correction_keep_the_original_monthly_crossing_record()
    {
        await using var database = await SqliteFinanceTestDatabase.CreateAsync();
        var category = new Category { Name = "Transport" };
        database.Context.Categories.Add(category);
        await database.Context.SaveChangesAsync();
        var month = new DateOnly(2026, 10, 1);
        database.Context.PaymentTypeMonthlyLimits.Add(NewPaymentLimit(PaymentType.Cash, month, 10_000));
        var expense = NewExpense(category.Id, month, PaymentType.Cash, 9_000);
        database.Context.Expenses.Add(expense);
        await database.Context.SaveChangesAsync();
        var processor = new BudgetThresholdProcessor(database.Context);
        Assert.Equal(1, await processor.RecordCrossingsAsync(new DateOnly(2026, 10, 17)));

        database.Context.Expenses.Remove(expense);
        await database.Context.SaveChangesAsync();

        Assert.Equal(0, await processor.RecordCrossingsAsync(new DateOnly(2026, 10, 17)));
        Assert.Equal(1, await database.Context.BudgetAlertDeliveries.CountAsync());
    }

    [Fact]
    public async Task New_month_uses_its_own_budget_and_preserves_prior_month_history()
    {
        await using var database = await SqliteFinanceTestDatabase.CreateAsync();
        var category = new Category { Name = "Dining" };
        database.Context.Categories.Add(category);
        await database.Context.SaveChangesAsync();
        var october = new DateOnly(2026, 10, 1);
        var november = new DateOnly(2026, 11, 1);
        database.Context.PaymentTypeMonthlyLimits.AddRange(
            NewPaymentLimit(PaymentType.Cash, october, 10_000),
            NewPaymentLimit(PaymentType.Cash, november, 20_000));
        database.Context.Expenses.AddRange(
            NewExpense(category.Id, october, PaymentType.Cash, 10_000),
            NewExpense(category.Id, november, PaymentType.Cash, 17_000));
        await database.Context.SaveChangesAsync();

        await new BudgetThresholdProcessor(database.Context).RecordCrossingsAsync(new DateOnly(2026, 10, 31));
        await new BudgetThresholdProcessor(database.Context).RecordCrossingsAsync(new DateOnly(2026, 11, 1));

        var events = await database.Context.BudgetAlertDeliveries
            .Include(item => item.PaymentTypeMonthlyLimit)
            .OrderBy(item => item.PaymentTypeMonthlyLimit!.Month)
            .ThenBy(item => item.ThresholdPercent)
            .ToListAsync();
        Assert.Equal(3, events.Count);
        Assert.Equal([85, 100], events.Where(item => item.PaymentTypeMonthlyLimit!.Month == 10)
            .Select(item => item.ThresholdPercent));
        Assert.Equal([85], events.Where(item => item.PaymentTypeMonthlyLimit!.Month == 11)
            .Select(item => item.ThresholdPercent));
    }

    private static PaymentTypeMonthlyLimit NewPaymentLimit(PaymentType type, DateOnly month, long amount) => new()
    {
        PaymentType = type,
        Year = month.Year,
        Month = month.Month,
        AmountInCentavos = amount
    };

    private static Expense NewExpense(int categoryId, DateOnly date, PaymentType type, long amount) => new()
    {
        AmountInCentavos = amount,
        ExpenseDate = date,
        PaymentType = type,
        CategoryId = categoryId,
        Description = "Test purchase"
    };
}
