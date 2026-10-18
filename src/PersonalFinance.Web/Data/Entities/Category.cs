namespace PersonalFinance.Web.Data.Entities;

public sealed class Category
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public bool IsArchived { get; set; }

    public ICollection<Expense> Expenses { get; } = new List<Expense>();

    public ICollection<RecurringExpense> RecurringExpenses { get; } = new List<RecurringExpense>();

    public ICollection<CategoryMonthlyBudget> MonthlyBudgets { get; } = new List<CategoryMonthlyBudget>();
}
