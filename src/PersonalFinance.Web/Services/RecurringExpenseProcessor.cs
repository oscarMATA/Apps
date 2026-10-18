using Microsoft.EntityFrameworkCore;
using PersonalFinance.Web.Data;
using PersonalFinance.Web.Data.Entities;

namespace PersonalFinance.Web.Services;

public sealed class RecurringExpenseProcessor(FinanceDbContext database)
{
    public async Task<int> CreateDueExpensesAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var dueIds = await database.RecurringExpenses.AsNoTracking()
            .Where(template => template.NextDueDate <= today)
            .OrderBy(template => template.NextDueDate)
            .Select(template => template.Id)
            .ToListAsync(cancellationToken);

        var createdCount = 0;
        foreach (var id in dueIds)
        {
            await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
            var template = await database.RecurringExpenses.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (template is null || template.NextDueDate > today)
            {
                await transaction.RollbackAsync(cancellationToken);
                continue;
            }

            var templateCreatedCount = 0;
            while (template.NextDueDate <= today)
            {
                var dueDate = template.NextDueDate;
                var alreadyCreated = await database.Expenses.AnyAsync(
                    item => item.RecurringExpenseId == template.Id && item.ExpenseDate == dueDate,
                    cancellationToken);

                if (!alreadyCreated)
                {
                    database.Expenses.Add(new Expense
                    {
                        AmountInCentavos = template.AmountInCentavos,
                        ExpenseDate = dueDate,
                        PaymentType = template.PaymentType,
                        CategoryId = template.CategoryId,
                        Description = template.Description,
                        RecurringExpenseId = template.Id
                    });
                    templateCreatedCount++;
                }

                template.NextDueDate = RecurrenceDateCalculator.GetNextDueDate(
                    template.Frequency,
                    dueDate,
                    template.FirstDueDate);
            }

            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            createdCount += templateCreatedCount;
        }

        return createdCount;
    }

}
