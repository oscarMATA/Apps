using PersonalFinance.Web.Data.Entities;

namespace PersonalFinance.Web.Services;

/// <summary>Calculates the next occurrence while preserving a schedule's original calendar anchor.</summary>
public static class RecurrenceDateCalculator
{
    public static DateOnly GetNextDueDate(
        RecurrenceFrequency frequency,
        DateOnly currentDueDate,
        DateOnly firstDueDate) => frequency switch
    {
        RecurrenceFrequency.Weekly => currentDueDate.AddDays(7),
        RecurrenceFrequency.Monthly => NextMonthlyDate(currentDueDate, firstDueDate.Day),
        RecurrenceFrequency.Yearly => NextYearlyDate(currentDueDate, firstDueDate.Month, firstDueDate.Day),
        _ => throw new InvalidOperationException($"Unknown recurrence frequency: {frequency}.")
    };

    private static DateOnly NextMonthlyDate(DateOnly current, int anchorDay)
    {
        var firstOfNextMonth = new DateOnly(current.Year, current.Month, 1).AddMonths(1);
        var day = Math.Min(anchorDay, DateTime.DaysInMonth(firstOfNextMonth.Year, firstOfNextMonth.Month));
        return new DateOnly(firstOfNextMonth.Year, firstOfNextMonth.Month, day);
    }

    private static DateOnly NextYearlyDate(DateOnly current, int anchorMonth, int anchorDay)
    {
        var year = current.Year + 1;
        var day = Math.Min(anchorDay, DateTime.DaysInMonth(year, anchorMonth));
        return new DateOnly(year, anchorMonth, day);
    }
}
