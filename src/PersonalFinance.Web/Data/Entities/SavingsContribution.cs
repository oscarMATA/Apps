namespace PersonalFinance.Web.Data.Entities;

public sealed class SavingsContribution
{
    public long Id { get; set; }

    public int SavingsGoalId { get; set; }

    public long AmountInCentavos { get; set; }

    public DateOnly ContributionDate { get; set; }

    public string? Description { get; set; }

    public SavingsGoal SavingsGoal { get; set; } = null!;
}
