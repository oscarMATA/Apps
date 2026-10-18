namespace PersonalFinance.Web.Data.Entities;

public sealed class Expense
{
    public long Id { get; set; }

    // MXN amount stored as integer centavos to avoid floating-point rounding.
    public long AmountInCentavos { get; set; }

    public DateOnly ExpenseDate { get; set; }

    public PaymentType PaymentType { get; set; }

    public int CategoryId { get; set; }

    public long? RecurringExpenseId { get; set; }

    public required string Description { get; set; }

    public Category Category { get; set; } = null!;

    public RecurringExpense? RecurringExpense { get; set; }
}
