namespace PersonalFinance.Web.Data.Entities;

public sealed class SavingsGoal
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public long MonthlyTargetInCentavos { get; set; }

    public bool IsArchived { get; set; }

    public ICollection<SavingsContribution> Contributions { get; } = new List<SavingsContribution>();
}
