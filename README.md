# Personal Finance App

Local-first expense and savings app. See [REQUIREMENTS.md](REQUIREMENTS.md) for product behavior and [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) for architecture, sequencing, and planned tests.

## Current development setup

- Windows 11 host, .NET 10 SDK.
- ASP.NET Core Razor Pages project: `src/PersonalFinance.Web`.
- EF Core SQLite provider and an initial schema migration.
- Single-owner password authentication with secure cookies; the first password can be created only from the host itself.
- User-managed categories, expense recording/editing/deletion, monthly expense history, and a current-month overview.
- Optional weekly, monthly, or yearly recurring expenses with automatic fixed-amount entries and catch-up after downtime.
- Monthly budgets by payment type and category, with persistent 85% and 100% threshold crossing records.
- The app binds to `127.0.0.1:5080`; it is not directly exposed to the LAN.
- The SQLite file defaults to `%LOCALAPPDATA%\PersonalFinance\personal-finance.db`. Set `FINANCE_DB_PATH` to an absolute path to override it.
- Database migrations are applied at app startup.

Run the app and Caddy together with `Start-PersonalFinance.cmd` in the project root. It opens separate Command Prompt windows for the ASP.NET Core app and Caddy, then opens `https://127.0.0.1/` in your default browser. Both service windows stay open until you stop or close them.

To run only the app manually:

```powershell
dotnet run --project src/PersonalFinance.Web
```

The Caddy configuration is in `deploy/Caddyfile`. It provides host-only HTTPS at `https://127.0.0.1` for first-run password setup and a LAN site at `https://192.168.100.22`. Both proxy to the loopback app port. First-run password setup rejects requests that do not originate from the host. The LAN listener is now protected by login; keep the app reachable only through Caddy and do not expose it to the public internet.

The Caddy executable is installed outside the project at `%LOCALAPPDATA%\Programs\Caddy\caddy.exe`. Caddy has not been installed as a Windows service.

## Automated tests

Run the unit and SQLite-backed functional checks from the project root:

```powershell
dotnet test --configuration Release
```

Release configuration avoids colliding with the Debug executable when the app is already running. The test project uses an in-memory SQLite database and applies the real EF Core migrations. It does not read or modify the personal finance database under `%LOCALAPPDATA%`. See [the testing guide](docs/testing.md) for test scope and coverage.

## First run and authentication

1. Run `Start-PersonalFinance.cmd` from the project root. It opens the service windows and host URL.
2. If Windows does not trust Caddy's local certificate, stop Caddy, run `& "$env:LOCALAPPDATA\Programs\Caddy\caddy.exe" trust` from an elevated PowerShell window, then run the launcher again.
3. On first run, open `https://127.0.0.1/Setup` on the host PC and create a password of at least 12 characters. Setup is unavailable from other devices. Sign in at `https://192.168.100.22` from the PC or phone.

The app stores only an ASP.NET Core password hash in SQLite. Sessions use an HTTP-only, Secure, SameSite=Strict cookie that expires after 12 hours. Five failed password attempts trigger a 15-minute lockout. Changing the password signs out existing sessions.

Categories can be archived or restored; archived categories remain associated with their past expenses. Expenses can be edited or permanently deleted. Amounts are stored as integer centavos. See [the data model](docs/data-model.md) for field rules and correction behavior.

Recurring expense schedules create regular expense entries while the app is running and catch up every missed due date when it starts after downtime. Monthly schedules use the original day of month, clamped to the last day for shorter months; yearly schedules use the original month/day, with February 29 clamped to February 28 in non-leap years. Deleting a schedule preserves expense entries that it already generated.

To recover access, stop Caddy and the app, then run this from the project root:

```powershell
dotnet run --project src/PersonalFinance.Web -- --reset-authentication
```

This removes only the local login credential; expenses, budgets, goals, contributions, and alert history remain in the database. Restart the app and Caddy, then set a new password from `https://127.0.0.1/Setup`.

## Local persistence development

The EF Core design package and local `dotnet-ef` tool are version 10.0.12. From the project root, create a migration with:

```powershell
dotnet tool run dotnet-ef migrations add MigrationName --project src/PersonalFinance.Web --startup-project src/PersonalFinance.Web
```

The current migration is `InitialCreate`. Never commit the SQLite database file or secrets.
