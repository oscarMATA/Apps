namespace PersonalFinance.Web.Data;

public static class FinanceDatabasePath
{
    public static string Resolve(string? configuredPath = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new InvalidOperationException("Windows did not provide a local application data directory.");
        }

        return Path.Combine(localApplicationData, "PersonalFinance", "personal-finance.db");
    }
}
