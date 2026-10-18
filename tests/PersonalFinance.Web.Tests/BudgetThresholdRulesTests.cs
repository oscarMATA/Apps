using PersonalFinance.Web.Services;

namespace PersonalFinance.Web.Tests;

public sealed class BudgetThresholdRulesTests
{
    [Theory]
    [InlineData(8_499, 10_000, "")]
    [InlineData(8_500, 10_000, "85")]
    [InlineData(9_999, 10_000, "85")]
    [InlineData(10_000, 10_000, "85,100")]
    [InlineData(25_000, 10_000, "85,100")]
    [InlineData(1, 0, "")]
    public void Returns_exactly_the_thresholds_reached(long spent, long limit, string expected)
    {
        var actual = BudgetThresholdRules.GetCrossedThresholds(spent, limit);
        Assert.Equal(expected, string.Join(',', actual));
    }

    [Fact]
    public void Rejects_negative_spending()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BudgetThresholdRules.GetCrossedThresholds(-1, 100));
    }
}
