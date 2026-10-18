namespace PersonalFinance.Web.Services;

public static class BudgetThresholdRules
{
    private static readonly int[] Thresholds = [85, 100];

    public static IReadOnlyList<int> GetCrossedThresholds(long spentInCentavos, long budgetInCentavos)
    {
        if (spentInCentavos < 0) throw new ArgumentOutOfRangeException(nameof(spentInCentavos));
        if (budgetInCentavos <= 0) return [];

        return Thresholds
            .Where(threshold => spentInCentavos * 100m >= budgetInCentavos * threshold)
            .ToArray();
    }
}
