# Personal Expense and Savings App — Requirements

**Status:** Requirements discovery, agreed decisions recorded  
**Last updated:** 2026-10-11  
**Purpose:** Durable project brief for future design and implementation work.

## 1. Product summary

Build a web app from scratch that runs on a computer on the user's personal network. The user can open it from multiple devices on that network. The host computer acts as the server and stores the app's expense and savings data locally.

The app helps one user record expenses, monitor monthly spending limits and savings goals, review summaries, and receive Outlook email alerts when spending approaches a budget limit.

## 2. User and deployment

- There is one user.
- The user accesses the app from multiple devices on their personal network.
- The app is hosted on the user's computer; that computer must be running and reachable for other devices to use the app.
- Require a password to use the app.
- Store the app's primary data on the host computer. No cloud database is requested.
- Sending email alerts requires outbound internet access. Alert messages will pass through the user's Outlook account.

## 3. Expense records

Each expense must include:

- Amount, in Mexican pesos (MXN).
- Date.
- Payment type: cash, credit card, voucher card, or debit.
- Optionally define a periodic expense with a fixed amount and weekly, monthly, or yearly schedule; the app automatically creates a dated expense entry for each scheduled payment.
- Create every occurrence missed while the app was off when it starts again.
- A monthly schedule repeats on the original day of the month, using that month's final day when the original day does not exist. A yearly schedule repeats on the original month and day; February 29 uses February 28 in non-leap years.
- Category, selected from categories created by the user.
- Description.

## 4. Budgets and spending alerts

- The user can configure a separate monthly spending limit for each payment type.
- The user can create and configure monthly budgets for expense categories.
- A category budget includes expenses from all payment types combined.
- Budgets use calendar months.
- Send an email alert when spending first reaches or crosses 85% of a budget, and another when it first reaches or crosses 100%.
- Apply these alerts to both payment-type limits and category budgets.
- Send each threshold alert at most once per budget per month.
- The alert destination is the user's personal Outlook account.
- Recommended email delivery: Microsoft Graph with delegated `Mail.Send` permission and user sign-in/consent. Do not require an SMTP server to run locally. This is a proposed implementation approach, to be validated during technical design.

## 5. Summaries

Provide weekly, monthly, and yearly summaries in the app.

- Week: Monday through Sunday.
- Month: calendar month.
- Year: calendar year.
- Summaries show spending amount, payment type, and category.
- Currency display is MXN.

## 6. Savings goals

- The user can create multiple savings goals.
- Each goal has a monthly target amount.
- The user can record contributions toward each goal.
- Monthly progress restarts each month.
- Preserve past contributions so the user can review contribution history.

## 7. Planning and suggestions

- Show a warning when current spending pace projects that a monthly payment-type limit or category budget will be exceeded by month end.
- Base the projection on the current month's spending pace.
- Suggest how much is reasonable to spend in particular categories to help the user stay within budgets and reach savings goals.

**Projection detail to settle during design:** A straightforward initial calculation is month-to-date spend divided by elapsed calendar days, multiplied by the number of days in the month. Decide how to handle very early-month projections and whether users can dismiss or tune warnings.

## 8. Security and data handling

- Require password authentication for access from all devices.
- Keep the app database on the host computer.
- Use Outlook only to deliver alerts; avoid including unnecessary financial details in email content.
- Protect stored credentials and authentication tokens. Never store the user's Outlook password in the app.

## 9. Agreed decisions

| Topic | Decision |
| --- | --- |
| User count | One user |
| Access | Multiple devices on a personal network |
| Hosting and primary storage | User's computer, acting as the server |
| App access control | Password required |
| Currency | Mexican pesos (MXN) |
| Expense fields | Amount, date, payment type, user-created category, description |
| Payment types | Cash, credit card, voucher card, debit |
| Periodic expenses | Optional fixed-amount schedules; weekly, monthly, or yearly; create every missed occurrence after downtime |
| Payment-type limits | Separate monthly limit for each type |
| Category budgets | Monthly, combine all payment types |
| Email thresholds | 85% and 100%, once per threshold per budget per month |
| Email account | Personal Outlook account |
| Email delivery direction | Microsoft Graph is recommended; no local SMTP server needed |
| Week definition | Monday through Sunday |
| Savings | Multiple goals, each with monthly target and recorded contributions |
| Savings history | Monthly progress restarts; retain prior contributions for review |
| Spending forecast | Project month-end spend from current spending pace |

## 10. Decisions still open

These are not blockers for documenting the agreed requirements; settle them before or during implementation:

1. How the first password is configured and how it can be changed or recovered.
2. Whether to provide a backup/export and restore feature for the local data.
3. Whether categories and budgets are created from a blank slate or with optional examples.
4. Whether suggestions should calculate a per-day or per-week remaining category allowance, and how they should account for savings targets.
5. What details an email alert should contain (recommended: budget name/type, percentage used, and amount remaining; omit expense descriptions).
6. Whether expenses and contributions can be edited or deleted, and how a correction affects already-sent alerts and summaries.

## 11. Out of scope unless added later

- Multiple users or separate accounts.
- Access from outside the personal network.
- Cloud storage or synchronization.
- Automatic bank or card transaction import.
- Multi-currency support.

