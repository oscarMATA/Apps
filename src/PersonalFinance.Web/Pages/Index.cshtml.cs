using System.Globalization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Web.Data;
using PersonalFinance.Web.Data.Entities;

namespace PersonalFinance.Web.Pages;

public sealed class IndexModel(FinanceDbContext database) : PageModel
{
    private static readonly CultureInfo MexicanCulture = CultureInfo.GetCultureInfo("es-MX");

    public string MonthLabel { get; private set; } = string.Empty;
    public string MonthTotalLabel => FormatMoney(PaymentTypeTotals.Sum(item => item.AmountInCentavos));
    public int ExpenseCount { get; private set; }
    public List<PaymentTypeTotal> PaymentTypeTotals { get; private set; } = [];
    public List<CategoryTotal> CategoryTotals { get; private set; } = [];
    public List<RecentExpense> RecentExpenses { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var start = new DateOnly(today.Year, today.Month, 1);
        var end = start.AddMonths(1);
        MonthLabel = start.ToDateTime(TimeOnly.MinValue).ToString("MMMM yyyy", MexicanCulture);

        var paymentTotals = await database.Expenses.AsNoTracking()
            .Where(item => item.ExpenseDate >= start && item.ExpenseDate < end)
            .GroupBy(item => item.PaymentType)
            .Select(group => new { PaymentType = group.Key, Amount = group.Sum(item => item.AmountInCentavos), Count = group.Count() })
            .ToListAsync();

        PaymentTypeTotals = Enum.GetValues<PaymentType>()
            .Select(type =>
            {
                var found = paymentTotals.SingleOrDefault(item => item.PaymentType == type);
                return new PaymentTypeTotal(type.ToDisplayName(), found?.Amount ?? 0);
            })
            .ToList();
        ExpenseCount = paymentTotals.Sum(item => item.Count);

        var categoryTotals = await database.Expenses.AsNoTracking()
            .Where(item => item.ExpenseDate >= start && item.ExpenseDate < end)
            .GroupBy(item => item.Category.Name)
            .Select(group => new { Name = group.Key, AmountInCentavos = group.Sum(item => item.AmountInCentavos) })
            .ToListAsync();
        CategoryTotals = categoryTotals
            .OrderByDescending(item => item.AmountInCentavos)
            .Select(item => new CategoryTotal(item.Name, item.AmountInCentavos))
            .ToList();

        RecentExpenses = await database.Expenses.AsNoTracking()
            .Where(item => item.ExpenseDate >= start && item.ExpenseDate < end)
            .OrderByDescending(item => item.ExpenseDate)
            .ThenByDescending(item => item.Id)
            .Take(8)
            .Select(item => new RecentExpense(item.Id, item.AmountInCentavos, item.ExpenseDate, item.Description, item.PaymentType, item.Category.Name))
            .ToListAsync();
    }

    private static string FormatMoney(long centavos) => "$" + (centavos / 100m).ToString("N2", MexicanCulture) + " MXN";

    public sealed record PaymentTypeTotal(string Name, long AmountInCentavos)
    {
        public string AmountLabel => FormatMoney(AmountInCentavos);
    }

    public sealed record CategoryTotal(string Name, long AmountInCentavos)
    {
        public string AmountLabel => FormatMoney(AmountInCentavos);
    }

    public sealed record RecentExpense(long Id, long AmountInCentavos, DateOnly Date, string Description, PaymentType PaymentType, string CategoryName)
    {
        public string DateLabel => Date.ToString("dd MMM", MexicanCulture);
        public string AmountLabel => FormatMoney(AmountInCentavos);
    }
}
