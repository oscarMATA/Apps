using Microsoft.EntityFrameworkCore;
using PersonalFinance.Web.Data.Entities;
using PersonalFinance.Web.Services;

namespace PersonalFinance.Web.Tests;

public sealed class RecurringExpenseProcessorTests
{
    [Fact]
    public async Task Applies_all_migrations_to_a_fresh_database()
    {
        await using var database = await SqliteFinanceTestDatabase.CreateAsync();

        Assert.Empty(await database.Context.Database.GetPendingMigrationsAsync());
        await using var command = database.Context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'RecurringExpenses';";
        Assert.Equal(1L, (long)(await command.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task Creates_due_expense_with_template_fields_and_advances_schedule()
    {
        await using var database = await SqliteFinanceTestDatabase.CreateAsync();
        var category = new Category { Name = "Utilities" };
        database.Context.Categories.Add(category);
        await database.Context.SaveChangesAsync();

        var firstDueDate = new DateOnly(2026, 10, 15);
        var template = NewTemplate(category.Id, firstDueDate, RecurrenceFrequency.Weekly);
        database.Context.RecurringExpenses.Add(template);
        await database.Context.SaveChangesAsync();

        var created = await new RecurringExpenseProcessor(database.Context)
            .CreateDueExpensesAsync(new DateOnly(2026, 10, 15), CancellationToken.None);

        var expense = await database.Context.Expenses.SingleAsync();
        Assert.Equal(1, created);
        Assert.Equal(12_345, expense.AmountInCentavos);
        Assert.Equal(firstDueDate, expense.ExpenseDate);
        Assert.Equal(PaymentType.Debit, expense.PaymentType);
        Assert.Equal(category.Id, expense.CategoryId);
        Assert.Equal("Monthly internet bill", expense.Description);
        Assert.Equal(template.Id, expense.RecurringExpenseId);
        Assert.Equal(new DateOnly(2026, 10, 22), template.NextDueDate);
    }

    [Fact]
    public async Task Catches_up_every_missed_monthly_occurrence_and_keeps_anchor_day()
    {
        await using var database = await SqliteFinanceTestDatabase.CreateAsync();
        var category = new Category { Name = "Rent" };
        database.Context.Categories.Add(category);
        await database.Context.SaveChangesAsync();

        var firstDueDate = new DateOnly(2026, 1, 31);
        database.Context.RecurringExpenses.Add(NewTemplate(
            category.Id,
            firstDueDate,
            RecurrenceFrequency.Monthly));
        await database.Context.SaveChangesAsync();

        var created = await new RecurringExpenseProcessor(database.Context)
            .CreateDueExpensesAsync(new DateOnly(2026, 3, 31), CancellationToken.None);

        var expenses = await database.Context.Expenses.OrderBy(item => item.ExpenseDate).ToListAsync();
        Assert.Equal(3, created);
        Assert.Equal(
            [new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 31)],
            expenses.Select(item => item.ExpenseDate));
        Assert.Equal(new DateOnly(2026, 4, 30), (await database.Context.RecurringExpenses.SingleAsync()).NextDueDate);
    }

    [Fact]
    public async Task Catches_up_yearly_schedule_through_non_leap_year()
    {
        await using var database = await SqliteFinanceTestDatabase.CreateAsync();
        var category = new Category { Name = "Annual" };
        database.Context.Categories.Add(category);
        await database.Context.SaveChangesAsync();

        database.Context.RecurringExpenses.Add(NewTemplate(
            category.Id,
            new DateOnly(2024, 2, 29),
            RecurrenceFrequency.Yearly));
        await database.Context.SaveChangesAsync();

        var created = await new RecurringExpenseProcessor(database.Context)
            .CreateDueExpensesAsync(new DateOnly(2025, 3, 1), CancellationToken.None);

        var expenses = await database.Context.Expenses.OrderBy(item => item.ExpenseDate).ToListAsync();
        Assert.Equal(2, created);
        Assert.Equal([new DateOnly(2024, 2, 29), new DateOnly(2025, 2, 28)], expenses.Select(item => item.ExpenseDate));
        Assert.Equal(new DateOnly(2026, 2, 28), (await database.Context.RecurringExpenses.SingleAsync()).NextDueDate);
    }

    [Fact]
    public async Task Does_not_duplicate_an_occurrence_if_the_due_date_is_reprocessed()
    {
        await using var database = await SqliteFinanceTestDatabase.CreateAsync();
        var category = new Category { Name = "Subscriptions" };
        database.Context.Categories.Add(category);
        await database.Context.SaveChangesAsync();

        var firstDueDate = new DateOnly(2026, 10, 1);
        var template = NewTemplate(category.Id, firstDueDate, RecurrenceFrequency.Monthly);
        database.Context.RecurringExpenses.Add(template);
        await database.Context.SaveChangesAsync();

        var processor = new RecurringExpenseProcessor(database.Context);
        Assert.Equal(1, await processor.CreateDueExpensesAsync(firstDueDate, CancellationToken.None));

        template.NextDueDate = firstDueDate;
        await database.Context.SaveChangesAsync();
        Assert.Equal(0, await processor.CreateDueExpensesAsync(firstDueDate, CancellationToken.None));

        Assert.Equal(1, await database.Context.Expenses.CountAsync());
        Assert.Equal(new DateOnly(2026, 11, 1), template.NextDueDate);
    }

    [Fact]
    public async Task Deleting_schedule_preserves_generated_expenses_without_schedule_link()
    {
        await using var database = await SqliteFinanceTestDatabase.CreateAsync();
        var category = new Category { Name = "Membership" };
        database.Context.Categories.Add(category);
        await database.Context.SaveChangesAsync();

        var template = NewTemplate(category.Id, new DateOnly(2026, 10, 1), RecurrenceFrequency.Monthly);
        database.Context.RecurringExpenses.Add(template);
        await database.Context.SaveChangesAsync();
        await new RecurringExpenseProcessor(database.Context)
            .CreateDueExpensesAsync(new DateOnly(2026, 10, 1), CancellationToken.None);

        database.Context.RecurringExpenses.Remove(template);
        await database.Context.SaveChangesAsync();

        var preservedExpense = await database.Context.Expenses.SingleAsync();
        Assert.Null(preservedExpense.RecurringExpenseId);
        Assert.Equal(new DateOnly(2026, 10, 1), preservedExpense.ExpenseDate);
    }

    private static RecurringExpense NewTemplate(
        int categoryId,
        DateOnly firstDueDate,
        RecurrenceFrequency frequency) => new()
    {
        AmountInCentavos = 12_345,
        Description = "Monthly internet bill",
        PaymentType = PaymentType.Debit,
        CategoryId = categoryId,
        Frequency = frequency,
        FirstDueDate = firstDueDate,
        NextDueDate = firstDueDate
    };
}
