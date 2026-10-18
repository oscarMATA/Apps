using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Web.Data;
using PersonalFinance.Web.Data.Entities;

namespace PersonalFinance.Web.Pages;

public sealed class CategoriesModel(FinanceDbContext database) : PageModel
{
    [BindProperty, Required, StringLength(100, MinimumLength = 1)]
    public string NewName { get; set; } = string.Empty;

    public List<Category> Categories { get; private set; } = [];

    public async Task OnGetAsync() => await LoadCategoriesAsync();

    public async Task<IActionResult> OnPostCreateAsync()
    {
        NewName = NewName.Trim();
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return Page();
        }

        database.Categories.Add(new Category { Name = NewName });
        if (!await SaveCategoryAsync())
        {
            await LoadCategoriesAsync();
            return Page();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRenameAsync(int id, string name)
    {
        name = name.Trim();
        if (name.Length is < 1 or > 100)
        {
            TempData["CategoryError"] = "Category names must be between 1 and 100 characters.";
            return RedirectToPage();
        }

        var category = await database.Categories.SingleOrDefaultAsync(item => item.Id == id);
        if (category is null) return NotFound();

        category.Name = name;
        if (!await SaveCategoryAsync())
        {
            TempData["CategoryError"] = "That category name is already in use.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostArchiveAsync(int id)
    {
        var category = await database.Categories.SingleOrDefaultAsync(item => item.Id == id);
        if (category is null) return NotFound();
        category.IsArchived = true;
        await database.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRestoreAsync(int id)
    {
        var category = await database.Categories.SingleOrDefaultAsync(item => item.Id == id);
        if (category is null) return NotFound();
        category.IsArchived = false;
        await database.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task LoadCategoriesAsync() => Categories = await database.Categories
        .OrderBy(item => item.IsArchived)
        .ThenBy(item => item.Name)
        .ToListAsync();

    private async Task<bool> SaveCategoryAsync()
    {
        try
        {
            await database.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(NewName), "A category with that name already exists.");
            return false;
        }
    }
}
