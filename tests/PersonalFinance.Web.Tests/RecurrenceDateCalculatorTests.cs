using PersonalFinance.Web.Data.Entities;
using PersonalFinance.Web.Services;

namespace PersonalFinance.Web.Tests;

public sealed class RecurrenceDateCalculatorTests
{
    [Fact]
    public void Weekly_schedule_advances_by_seven_days()
    {
        var next = RecurrenceDateCalculator.GetNextDueDate(
            RecurrenceFrequency.Weekly,
            new DateOnly(2026, 10, 17),
            new DateOnly(2026, 10, 17));

        Assert.Equal(new DateOnly(2026, 10, 24), next);
    }

    [Theory]
    [InlineData(2026, 1, 31, 2026, 2, 28)]
    [InlineData(2026, 2, 28, 2026, 3, 31)]
    [InlineData(2024, 1, 31, 2024, 2, 29)]
    public void Monthly_schedule_clamps_short_months_and_restores_original_anchor(
        int year, int month, int day, int expectedYear, int expectedMonth, int expectedDay)
    {
        var firstDueDate = new DateOnly(year, 1, 31);
        var next = RecurrenceDateCalculator.GetNextDueDate(
            RecurrenceFrequency.Monthly,
            new DateOnly(year, month, day),
            firstDueDate);

        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), next);
    }

    [Theory]
    [InlineData(2024, 2, 29, 2025, 2, 28)]
    [InlineData(2025, 2, 28, 2026, 2, 28)]
    public void Yearly_february_29_schedule_clamps_in_non_leap_years(
        int currentYear, int currentMonth, int currentDay, int expectedYear, int expectedMonth, int expectedDay)
    {
        var next = RecurrenceDateCalculator.GetNextDueDate(
            RecurrenceFrequency.Yearly,
            new DateOnly(currentYear, currentMonth, currentDay),
            new DateOnly(2024, 2, 29));

        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), next);
    }
}
