using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Web.Data;
using PersonalFinance.Web.Data.Entities;

namespace PersonalFinance.Web.Pages;

public sealed class RecurringExpensesModel(FinanceDbContext database) : PageModel
{
    private static readonly CultureInfo MexicanCulture = CultureInfo.GetCultureInfo("es-MX");

    [BindProperty]
    public RecurringExpenseInput Form { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public long? Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Saved { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Deleted { get; set; }

    public List<RecurringExpenseRow> Schedules { get; private set; } = [];
    public List<Category> ActiveCategories { get; private set; } = [];
    public List<SelectListItem> CategoryOptions { get; private set; } = [];
    public List<SelectListItem> PaymentTypes { get; } = Enum.GetValues<PaymentType>()
        .Select(value => new SelectListItem(value.ToDisplayName(), value.ToString()))
        .ToList();
    public List<SelectListItem> Frequencies { get; } = Enum.GetValues<RecurrenceFrequency>()
        .Select(value => new SelectListItem(value.ToDisplayName(), value.ToString()))
        .ToList();
    public string FirstDueDateLabel { get; private set; } = string.Empty;
    public string NextDueDateLabel { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        Form = new RecurringExpenseInput { FirstDueDate = DateOnly.FromDateTime(DateTime.Today) };

        if (Id is > 0)
        {
            var template = await database.RecurringExpenses.AsNoTracking().SingleOrDefaultAsync(item => item.Id == Id.Value);
            if (template is null) return NotFound();
            Form = new RecurringExpenseInput
            {
                Id = template.Id,
                Amount = template.AmountInCentavos / 100m,
                PaymentType = template.PaymentType,
                CategoryId = template.CategoryId,
                Frequency = template.Frequency,
                FirstDueDate = template.FirstDueDate,
                Description = template.Description
            };
            FirstDueDateLabel = FormatDate(template.FirstDueDate);
            NextDueDateLabel = FormatDate(template.NextDueDate);
        }

        await LoadPageDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (Form.Amount is { } amount && decimal.Round(amount, 2) != amount)
        {
            ModelState.AddModelError("Form.Amount", "Enter an amount with no more than two decimal places.");
        }
        if (Form.Id is null && Form.FirstDueDate < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError("Form.FirstDueDate", "The first due date must be today or later.");
        }

        var category = Form.CategoryId is { } categoryId
            ? await database.Categories.SingleOrDefaultAsync(item => item.Id == categoryId)
            : null;
        var existing = Form.Id is > 0
            ? await database.RecurringExpenses.SingleOrDefaultAsync(item => item.Id == Form.Id.Value)
            : null;

        if (Form.Id is > 0 && existing is null) return NotFound();
        if (category is null || (category.IsArchived && existing?.CategoryId != category.Id))
        {
            ModelState.AddModelError("Form.CategoryId", "Choose an active category.");
        }
        if (Form.PaymentType is { } paymentType && !Enum.IsDefined(paymentType))
        {
            ModelState.AddModelError("Form.PaymentType", "Choose a valid payment type.");
        }
        if (Form.Frequency is { } frequency && !Enum.IsDefined(frequency))
        {
            ModelState.AddModelError("Form.Frequency", "Choose a valid schedule.");
        }

        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync();
            if (existing is not null)
            {
                FirstDueDateLabel = FormatDate(existing.FirstDueDate);
                NextDueDateLabel = FormatDate(existing.NextDueDate);
            }
            return Page();
        }

        var template = existing ?? new RecurringExpense
        {
            Description = string.Empty,
            FirstDueDate = Form.FirstDueDate,
            NextDueDate = Form.FirstDueDate
        };
        template.AmountInCentavos = decimal.ToInt64(Form.Amount!.Value * 100m);
        template.Description = Form.Description!.Trim();
        template.PaymentType = Form.PaymentType!.Value;
        template.CategoryId = Form.CategoryId!.Value;
        template.Frequency = Form.Frequency!.Value;

        if (existing is null) database.RecurringExpenses.Add(template);
        await database.SaveChangesAsync();
        return RedirectToPage("/RecurringExpenses", new { saved = true });
    }

    public async Task<IActionResult> OnPostDeleteAsync(long id)
    {
        var template = await database.RecurringExpenses.SingleOrDefaultAsync(item => item.Id == id);
        if (template is null) return NotFound();
        database.RecurringExpenses.Remove(template);
        await database.SaveChangesAsync();
        return RedirectToPage("/RecurringExpenses", new { deleted = true });
    }

    private async Task LoadPageDataAsync()
    {
        Schedules = await database.RecurringExpenses.AsNoTracking()
            .OrderBy(item => item.NextDueDate)
            .ThenBy(item => item.Description)
            .Select(item => new RecurringExpenseRow(
                item.Id,
                item.AmountInCentavos,
                item.Description,
                item.PaymentType,
                item.Category.Name,
                item.Frequency,
                item.NextDueDate))
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
    private static string FormatDate(DateOnly date) => date.ToString("dd MMM yyyy", MexicanCulture);

    public sealed class RecurringExpenseInput
    {
        public long? Id { get; set; }

        [Required, Range(typeof(decimal), "0.01", "999999999.99")]
        public decimal? Amount { get; set; }

        [Required]
        public PaymentType? PaymentType { get; set; }

        [Required]
        public int? CategoryId { get; set; }

        [Required]
        public RecurrenceFrequency? Frequency { get; set; }

        [Required, DataType(DataType.Date)]
        public DateOnly FirstDueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Required, StringLength(500, MinimumLength = 1)]
        public string? Description { get; set; }
    }

    public sealed record RecurringExpenseRow(
        long Id,
        long AmountInCentavos,
        string Description,
        PaymentType PaymentType,
        string CategoryName,
        RecurrenceFrequency Frequency,
        DateOnly NextDueDate)
    {
        public string AmountLabel => FormatMoney(AmountInCentavos);
        public string NextDueDateLabel => FormatDate(NextDueDate);
    }
}
