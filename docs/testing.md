# Automated testing

## Run the suite

From the repository root on Windows, run:

```powershell
dotnet test --configuration Release
```

The test project is `tests/PersonalFinance.Web.Tests`. A successful run should report all tests passing. Release configuration lets you run the tests while the app is running in Debug. The SQLite tests open a fresh in-memory database for each case and apply the application's real EF Core migrations. They do not access the user's persistent database or send email.

## Test layers

### Unit tests

`RecurrenceDateCalculatorTests` checks schedule date calculations without a database:

- Weekly schedules advance by seven days.
- Monthly schedules retain their original day when later months allow it and clamp to the last day of shorter months.
- Yearly February 29 schedules clamp to February 28 in non-leap years.
- Budget thresholds are exact at 85% and 100%; a single expense may cross both, zero/missing limits produce no thresholds, and negative spend is rejected.

### SQLite functional tests

`RecurringExpenseProcessorTests` exercises the production processor against a migrated SQLite database:

- Applying all migrations to a fresh database.
- Creating due expense records with the template's amount, date, payment type, category, description, and source link.
- Catching up missed monthly and yearly occurrences and advancing the next due date.
- Preventing duplicate expense entries when a due date is reprocessed.
- Preserving generated expense history when a recurring schedule is deleted.
- Combining payment types into category budgets while keeping payment-type budgets separate.
- Persisting each threshold once, retaining event history after an expense correction, and separating budgets by calendar month.

## Coverage gaps

The automated coverage now includes recurring expenses and monthly budget calculations/event persistence. Authentication, category and expense page workflows, summary calculations, savings goals, Outlook delivery, and browser/device behavior still need automated or manual checks. Any checks that use real email should use a dedicated test mailbox and explicit consent; routine database tests must not transmit messages.

## Test data and isolation

Functional checks use disposable in-memory SQLite databases with fixed dates and values. They do not seed, clear, or otherwise alter the live database. Tests should remain deterministic and should avoid using today's date unless the behavior under test specifically depends on the current date.
