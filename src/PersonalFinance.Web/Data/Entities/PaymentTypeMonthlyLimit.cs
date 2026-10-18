namespace PersonalFinance.Web.Data.Entities;

public sealed class PaymentTypeMonthlyLimit
{
    public int Id { get; set; }

    public PaymentType PaymentType { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public long AmountInCentavos { get; set; }

    public ICollection<BudgetAlertDelivery> AlertDeliveries { get; } = new List<BudgetAlertDelivery>();
}
