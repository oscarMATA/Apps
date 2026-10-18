namespace PersonalFinance.Web.Data.Entities;

public sealed class RecurringExpense
{
    public long Id { get; set; }

    public long AmountInCentavos { get; set; }

    public required string Description { get; set; }

    public PaymentType PaymentType { get; set; }

    public int CategoryId { get; set; }

    public RecurrenceFrequency Frequency { get; set; }

    public DateOnly FirstDueDate { get; set; }

    public DateOnly NextDueDate { get; set; }

    public Category Category { get; set; } = null!;

    public ICollection<Expense> GeneratedExpenses { get; } = new List<Expense>();
}
