using System.Security.Claims;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Web.Data;
using PersonalFinance.Web.Data.Entities;
using PersonalFinance.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var databasePath = FinanceDatabasePath.Resolve(
    builder.Configuration["Finance:DatabasePath"] ?? Environment.GetEnvironmentVariable("FINANCE_DB_PATH"));

var databaseDirectory = Path.GetDirectoryName(databasePath)
    ?? throw new InvalidOperationException("The configured database path must include a directory.");
Directory.CreateDirectory(databaseDirectory);

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    Mode = SqliteOpenMode.ReadWriteCreate
}.ToString();

builder.Services.AddDbContext<FinanceDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IPasswordHasher<AppCredential>, PasswordHasher<AppCredential>>();
builder.Services.AddScoped<RecurringExpenseProcessor>();
builder.Services.AddScoped<BudgetThresholdProcessor>();
builder.Services.AddHostedService<RecurringExpenseWorker>();
builder.Services.AddHostedService<BudgetThresholdWorker>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "__Host-PersonalFinance.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.Path = "/";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.Events.OnValidatePrincipal = async context =>
        {
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var stamp = context.Principal?.FindFirstValue("security_stamp");
            var database = context.HttpContext.RequestServices.GetRequiredService<FinanceDbContext>();
            var credential = await database.AppCredentials.AsNoTracking().SingleOrDefaultAsync(item => item.Id == 1);

            if (userId != "owner" || credential is null || stamp is null || stamp != credential.SecurityStamp)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Setup");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Error");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
    await database.Database.MigrateAsync();

    if (args.Contains("--reset-authentication", StringComparer.OrdinalIgnoreCase))
    {
        var credential = await database.AppCredentials.SingleOrDefaultAsync(item => item.Id == 1);
        if (credential is null)
        {
            Console.WriteLine("No local password is configured. Visit /Setup after starting the app.");
        }
        else
        {
            database.AppCredentials.Remove(credential);
            await database.SaveChangesAsync();
            Console.WriteLine("The local login credential was cleared. Financial data was not changed.");
        }

        return;
    }
}

// Caddy is the only network-facing entry point and runs on this same host.
// Trust forwarded scheme information only from loopback so HTTPS cookies work behind Caddy.
var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1
};
forwardedHeaders.KnownProxies.Clear();
forwardedHeaders.KnownProxies.Add(IPAddress.Loopback);
forwardedHeaders.KnownProxies.Add(IPAddress.IPv6Loopback);
app.UseForwardedHeaders(forwardedHeaders);

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
