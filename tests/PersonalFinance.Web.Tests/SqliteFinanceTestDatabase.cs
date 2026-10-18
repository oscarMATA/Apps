using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Web.Data;

namespace PersonalFinance.Web.Tests;

internal sealed class SqliteFinanceTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private SqliteFinanceTestDatabase(SqliteConnection connection, FinanceDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    public FinanceDbContext Context { get; }

    public static async Task<SqliteFinanceTestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new FinanceDbContext(options);
        await context.Database.MigrateAsync();

        return new SqliteFinanceTestDatabase(connection, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
