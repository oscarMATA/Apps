using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Web.Data;
using PersonalFinance.Web.Data.Entities;
using PersonalFinance.Web.Services;

namespace PersonalFinance.Web.Pages;

public sealed class ExpensesModel(FinanceDbContext database, BudgetThresholdProcessor thresholdProcessor) : PageModel
{
    private static readonly CultureInfo MexicanCulture = CultureInfo.GetCultureInfo("es-MX");

    [BindProperty]
    public ExpenseInput Form { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int Month { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Year { get; set; }

    [BindProperty(SupportsGet = true)]
    public long? Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Saved { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Deleted { get; set; }

    public List<ExpenseRow> Expenses { get; private set; } = [];
    public List<Category> ActiveCategories { get; private set; } = [];
    public List<SelectListItem> CategoryOptions { get; private set; } = [];
    public List<SelectListItem> PaymentTypes { get; } = Enum.GetValues<PaymentType>()
        .Select(value => new SelectListItem(value.ToDisplayName(), value.ToString()))
        .ToList();
    public DateOnly PreviousMonth { get; private set; }
    public DateOnly NextMonth { get; private set; }
    public string MonthLabel { get; private set; } = string.Empty;
    public string TotalLabel => FormatMoney(Expenses.Sum(item => item.AmountInCentavos));

    public async Task<IActionResult> OnGetAsync()
    {
        EnsurePeriod();
        Form = new ExpenseInput { ExpenseDate = DateOnly.FromDateTime(DateTime.Today) };

        if (Id is > 0)
        {
            var expense = await database.Expenses.AsNoTracking().SingleOrDefaultAsync(item => item.Id == Id.Value);
            if (expense is null) return NotFound();
            Form = new ExpenseInput
            {
                Id = expense.Id,
                Amount = expense.AmountInCentavos / 100m,
                ExpenseDate = expense.ExpenseDate,
                PaymentType = expense.PaymentType,
                CategoryId = expense.CategoryId,
                Description = expense.Description
            };
        }

        await LoadPageDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        EnsurePeriod();
        if (Form.Amount is { } amount && decimal.Round(amount, 2) != amount)
        {
            ModelState.AddModelError("Form.Amount", "Enter an amount with no more than two decimal places.");
        }

        var category = Form.CategoryId is { } categoryId
            ? await database.Categories.SingleOrDefaultAsync(item => item.Id == categoryId)
            : null;
        var existing = Form.Id is > 0
            ? await database.Expenses.SingleOrDefaultAsync(item => item.Id == Form.Id.Value)
            : null;

        if (Form.Id is > 0 && existing is null) return NotFound();
        if (category is null || (category.IsArchived && existing?.CategoryId != category.Id))
        {
            ModelState.AddModelError("Form.CategoryId", "Choose an active category.");
        }
        if (Form.PaymentType is { } type && !Enum.IsDefined(type))
        {
            ModelState.AddModelError("Form.PaymentType", "Choose a valid payment type.");
        }

        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync();
            return Page();
        }

        var record = existing ?? new Expense { Description = string.Empty };
        record.AmountInCentavos = decimal.ToInt64(Form.Amount!.Value * 100m);
        record.ExpenseDate = Form.ExpenseDate;
        record.PaymentType = Form.PaymentType!.Value;
        record.CategoryId = Form.CategoryId!.Value;
        record.Description = Form.Description!.Trim();

        if (existing is null) database.Expenses.Add(record);
        await database.SaveChangesAsync();
        await thresholdProcessor.RecordCrossingsAsync(DateOnly.FromDateTime(DateTime.Today));
        return RedirectToPage("/Expenses", new { month = record.ExpenseDate.Month, year = record.ExpenseDate.Year, saved = true });
    }

    public async Task<IActionResult> OnPostDeleteAsync(long id, int month, int year)
    {
        var expense = await database.Expenses.SingleOrDefaultAsync(item => item.Id == id);
        if (expense is null) return NotFound();
        database.Expenses.Remove(expense);
        await database.SaveChangesAsync();
        await thresholdProcessor.RecordCrossingsAsync(DateOnly.FromDateTime(DateTime.Today));
        EnsurePeriod(month, year);
        return RedirectToPage("/Expenses", new { month = Month, year = Year, deleted = true });
    }

    private void EnsurePeriod()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        EnsurePeriod(Month, Year);
        if (Month == 0) Month = today.Month;
        if (Year == 0) Year = today.Year;
    }

    private void EnsurePeriod(int month, int year)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        Month = month is >= 1 and <= 12 ? month : today.Month;
        Year = year is >= 2000 and <= 2100 ? year : today.Year;
    }

    private async Task LoadPageDataAsync()
    {
        var start = new DateOnly(Year, Month, 1);
        var end = start.AddMonths(1);
        var startOfPrevious = start.AddMonths(-1);
        PreviousMonth = startOfPrevious;
        NextMonth = start.AddMonths(1);
        MonthLabel = start.ToDateTime(TimeOnly.MinValue).ToString("MMMM yyyy", MexicanCulture);

        Expenses = await database.Expenses.AsNoTracking()
            .Where(item => item.ExpenseDate >= start && item.ExpenseDate < end)
            .OrderByDescending(item => item.ExpenseDate)
            .ThenByDescending(item => item.Id)
            .Select(item => new ExpenseRow
            {
                Id = item.Id,
                AmountInCentavos = item.AmountInCentavos,
                Date = item.ExpenseDate,
                Description = item.Description,
                PaymentType = item.PaymentType,
                IsScheduled = item.RecurringExpenseId.HasValue,
                CategoryName = item.Category.Name
            })
            .ToListAsync();

        ActiveCategories = await database.Categories.AsNoTracking()
            .Where(item => !item.IsArchived)
            .OrderBy(item => item.Name)
            .ToListAsync();
        if (Form.CategoryId is { } selectedId && ActiveCategories.All(item => item.Id != selectedId))
        {
            var selected = await database.Categories.AsNoTracking().SingleOrDefaultAsync(item => item.Id == selectedId);
            if (selected is not null) ActiveCategories.Add(selected);
        }
        CategoryOptions = ActiveCategories
            .OrderBy(item => item.IsArchived)
            .ThenBy(item => item.Name)
            .Select(item => new SelectListItem(item.IsArchived ? $"{item.Name} (archived)" : item.Name, item.Id.ToString()))
            .ToList();
    }

    private static string FormatMoney(long centavos) => "$" + (centavos / 100m).ToString("N2", MexicanCulture) + " MXN";

    public sealed class ExpenseInput
    {
        public long? Id { get; set; }

        [Required, Range(typeof(decimal), "0.01", "999999999.99")]
        public decimal? Amount { get; set; }

        [Required, DataType(DataType.Date)]
        public DateOnly ExpenseDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Required]
        public PaymentType? PaymentType { get; set; }

        [Required]
        public int? CategoryId { get; set; }

        [Required, StringLength(500, MinimumLength = 1)]
        public string? Description { get; set; }
    }

    public sealed class ExpenseRow
    {
        public long Id { get; init; }
        public long AmountInCentavos { get; init; }
        public DateOnly Date { get; init; }
        public string Description { get; init; } = string.Empty;
        public PaymentType PaymentType { get; init; }
        public bool IsScheduled { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public string DateLabel => Date.ToString("dd MMM yyyy", MexicanCulture);
        public string AmountLabel => FormatMoney(AmountInCentavos);
    }
}
