using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Data.Sqlite;

namespace PersonalFinance.Web.Data;

public sealed class FinanceDbContextFactory : IDesignTimeDbContextFactory<FinanceDbContext>
{
    public FinanceDbContext CreateDbContext(string[] args)
    {
        var databasePath = FinanceDatabasePath.Resolve(Environment.GetEnvironmentVariable("FINANCE_DB_PATH"));
        var databaseDirectory = Path.GetDirectoryName(databasePath)
            ?? throw new InvalidOperationException("The configured database path must include a directory.");
        Directory.CreateDirectory(databaseDirectory);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new FinanceDbContext(options);
    }
}
