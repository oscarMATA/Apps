namespace PersonalFinance.Web.Data.Entities;

public sealed class BudgetAlertDelivery
{
    public long Id { get; set; }

    public int? PaymentTypeMonthlyLimitId { get; set; }

    public int? CategoryMonthlyBudgetId { get; set; }

    public int ThresholdPercent { get; set; }

    public DateTimeOffset? LastAttemptAtUtc { get; set; }

    public DateTimeOffset? SentAtUtc { get; set; }

    public string? LastFailure { get; set; }

    public PaymentTypeMonthlyLimit? PaymentTypeMonthlyLimit { get; set; }

    public CategoryMonthlyBudget? CategoryMonthlyBudget { get; set; }
}
