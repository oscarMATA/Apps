using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Web.Data;
using PersonalFinance.Web.Data.Entities;
using PersonalFinance.Web.Services;

namespace PersonalFinance.Web.Pages;

public sealed class BudgetsModel(FinanceDbContext database, BudgetThresholdProcessor thresholdProcessor) : PageModel
{
    private static readonly CultureInfo MexicanCulture = CultureInfo.GetCultureInfo("es-MX");

    [BindProperty]
    public PaymentLimitInput PaymentLimitForm { get; set; } = new();

    [BindProperty]
    public CategoryBudgetInput CategoryBudgetForm { get; set; } = new();

    public int Year { get; private set; }
    public int Month { get; private set; }
    public string MonthLabel { get; private set; } = string.Empty;
    public List<PaymentBudgetRow> PaymentBudgets { get; private set; } = [];
    public List<CategoryBudgetRow> CategoryBudgets { get; private set; } = [];
    public List<Category> Categories { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        await thresholdProcessor.RecordCrossingsAsync(today);
        await LoadPageDataAsync(today);
    }

    public async Task<IActionResult> OnPostSavePaymentLimitAsync()
    {
        if (PaymentLimitForm.PaymentType is not { } type || !Enum.IsDefined(type))
            ModelState.AddModelError("PaymentLimitForm.PaymentType", "Choose a valid payment type.");
        ValidateAmount(PaymentLimitForm.Amount, "PaymentLimitForm.Amount");
        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync(DateOnly.FromDateTime(DateTime.Today));
            return Page();
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var existing = await database.PaymentTypeMonthlyLimits.SingleOrDefaultAsync(item =>
            item.Year == today.Year && item.Month == today.Month && item.PaymentType == PaymentLimitForm.PaymentType);

        if (PaymentLimitForm.Amount is null)
        {
            if (existing is not null) database.PaymentTypeMonthlyLimits.Remove(existing);
        }
        else if (existing is null)
        {
            database.PaymentTypeMonthlyLimits.Add(new PaymentTypeMonthlyLimit
            {
                PaymentType = PaymentLimitForm.PaymentType!.Value,
                Year = today.Year,
                Month = today.Month,
                AmountInCentavos = ToCentavos(PaymentLimitForm.Amount.Value)
            });
        }
        else
        {
            existing.AmountInCentavos = ToCentavos(PaymentLimitForm.Amount.Value);
        }

        await database.SaveChangesAsync();
        await thresholdProcessor.RecordCrossingsAsync(today);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveCategoryBudgetAsync()
    {
        if (CategoryBudgetForm.CategoryId is null)
            ModelState.AddModelError("CategoryBudgetForm.CategoryId", "Choose a category.");
        if (CategoryBudgetForm.Amount is null)
            ModelState.AddModelError("CategoryBudgetForm.Amount", "Enter a monthly limit.");
        ValidateAmount(CategoryBudgetForm.Amount, "CategoryBudgetForm.Amount");
        var category = CategoryBudgetForm.CategoryId is { } categoryId
            ? await database.Categories.SingleOrDefaultAsync(item => item.Id == categoryId)
            : null;
        if (category is null || category.IsArchived)
            ModelState.AddModelError("CategoryBudgetForm.CategoryId", "Choose an active category.");

        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync(DateOnly.FromDateTime(DateTime.Today));
            return Page();
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var existing = await database.CategoryMonthlyBudgets.SingleOrDefaultAsync(item =>
            item.CategoryId == CategoryBudgetForm.CategoryId && item.Year == today.Year && item.Month == today.Month);

        if (existing is null)
        {
            database.CategoryMonthlyBudgets.Add(new CategoryMonthlyBudget
            {
                CategoryId = CategoryBudgetForm.CategoryId!.Value,
                Year = today.Year,
                Month = today.Month,
                AmountInCentavos = ToCentavos(CategoryBudgetForm.Amount!.Value)
            });
        }
        else
        {
            existing.AmountInCentavos = ToCentavos(CategoryBudgetForm.Amount!.Value);
        }

        await database.SaveChangesAsync();
        await thresholdProcessor.RecordCrossingsAsync(today);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteCategoryBudgetAsync(int id)
    {
        var budget = await database.CategoryMonthlyBudgets.SingleOrDefaultAsync(item => item.Id == id);
        if (budget is null) return NotFound();
        database.CategoryMonthlyBudgets.Remove(budget);
        await database.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task LoadPageDataAsync(DateOnly today)
    {
        Year = today.Year;
        Month = today.Month;
        MonthLabel = new DateOnly(Year, Month, 1).ToDateTime(TimeOnly.MinValue)
            .ToString("MMMM yyyy", MexicanCulture);
        var monthStart = new DateOnly(Year, Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var paymentLimits = await database.PaymentTypeMonthlyLimits.AsNoTracking()
            .Where(item => item.Year == Year && item.Month == Month)
            .ToDictionaryAsync(item => item.PaymentType, item => item.AmountInCentavos);
        var paymentSpent = await database.Expenses.AsNoTracking()
            .Where(item => item.ExpenseDate >= monthStart && item.ExpenseDate < nextMonth)
            .GroupBy(item => item.PaymentType)
            .Select(group => new { Type = group.Key, Spent = group.Sum(item => item.AmountInCentavos) })
            .ToDictionaryAsync(item => item.Type, item => item.Spent);
        PaymentBudgets = Enum.GetValues<PaymentType>().Select(type =>
        {
            var limit = paymentLimits.GetValueOrDefault(type);
            var spent = paymentSpent.GetValueOrDefault(type);
            return new PaymentBudgetRow(type, limit, spent);
        }).ToList();

        var categoryBudgets = await database.CategoryMonthlyBudgets.AsNoTracking()
            .Where(item => item.Year == Year && item.Month == Month)
            .Select(item => new { item.Id, item.CategoryId, Name = item.Category.Name, item.AmountInCentavos })
            .ToListAsync();
        var categorySpent = await database.Expenses.AsNoTracking()
            .Where(item => item.ExpenseDate >= monthStart && item.ExpenseDate < nextMonth)
            .GroupBy(item => item.CategoryId)
            .Select(group => new { CategoryId = group.Key, Spent = group.Sum(item => item.AmountInCentavos) })
            .ToDictionaryAsync(item => item.CategoryId, item => item.Spent);
        CategoryBudgets = categoryBudgets.Select(item => new CategoryBudgetRow(
            item.Id, item.CategoryId, item.Name, item.AmountInCentavos,
            categorySpent.GetValueOrDefault(item.CategoryId))).ToList();

        Categories = await database.Categories.AsNoTracking()
            .Where(item => !item.IsArchived)
            .OrderBy(item => item.Name)
            .ToListAsync();
    }

    private void ValidateAmount(decimal? amount, string modelKey)
    {
        if (amount is null) return;
        if (amount <= 0 || amount > 999999999.99m)
            ModelState.AddModelError(modelKey, "Enter an amount greater than zero.");
        else if (decimal.Round(amount.Value, 2) != amount)
            ModelState.AddModelError(modelKey, "Enter an amount with no more than two decimal places.");
    }

    private static long ToCentavos(decimal amount) => decimal.ToInt64(amount * 100m);
    private static string FormatMoney(long centavos) => "$" + (centavos / 100m).ToString("N2", MexicanCulture) + " MXN";

    public sealed class PaymentLimitInput
    {
        public PaymentType? PaymentType { get; set; }
        public decimal? Amount { get; set; }
    }

    public sealed class CategoryBudgetInput
    {
        public int? CategoryId { get; set; }
        public decimal? Amount { get; set; }
    }

    public sealed record PaymentBudgetRow(PaymentType PaymentType, long? LimitInCentavos, long SpentInCentavos)
    {
        public long RemainingInCentavos => (LimitInCentavos ?? 0) - SpentInCentavos;
        public bool IsOver => LimitInCentavos is { } limit && SpentInCentavos >= limit;
        public decimal UsagePercent => LimitInCentavos is > 0 ? SpentInCentavos * 100m / LimitInCentavos.Value : 0;
        public string LimitLabel => LimitInCentavos.HasValue ? FormatMoney(LimitInCentavos.Value) : "Not set";
        public string SpentLabel => FormatMoney(SpentInCentavos);
        public string RemainingLabel => LimitInCentavos.HasValue ? FormatMoney(RemainingInCentavos) : "—";
    }

    public sealed record CategoryBudgetRow(int Id, int CategoryId, string Name, long LimitInCentavos, long SpentInCentavos)
    {
        public long RemainingInCentavos => LimitInCentavos - SpentInCentavos;
        public bool IsOver => SpentInCentavos >= LimitInCentavos;
        public decimal UsagePercent => SpentInCentavos * 100m / LimitInCentavos;
        public string LimitLabel => FormatMoney(LimitInCentavos);
        public string SpentLabel => FormatMoney(SpentInCentavos);
        public string RemainingLabel => FormatMoney(RemainingInCentavos);
    }
}
