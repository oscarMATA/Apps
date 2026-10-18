namespace PersonalFinance.Web.Data.Entities;

public sealed class CategoryMonthlyBudget
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public long AmountInCentavos { get; set; }

    public Category Category { get; set; } = null!;

    public ICollection<BudgetAlertDelivery> AlertDeliveries { get; } = new List<BudgetAlertDelivery>();
}
