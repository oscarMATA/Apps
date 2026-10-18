namespace PersonalFinance.Web.Data.Entities;

public enum RecurrenceFrequency
{
    Weekly,
    Monthly,
    Yearly
}

public static class RecurrenceFrequencyExtensions
{
    public static string ToDisplayName(this RecurrenceFrequency frequency) => frequency switch
    {
        RecurrenceFrequency.Weekly => "Weekly",
        RecurrenceFrequency.Monthly => "Monthly",
        RecurrenceFrequency.Yearly => "Yearly",
        _ => frequency.ToString()
    };
}
