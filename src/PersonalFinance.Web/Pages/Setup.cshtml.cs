using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Web.Data;
using PersonalFinance.Web.Data.Entities;

namespace PersonalFinance.Web.Pages;

public sealed class SetupModel(FinanceDbContext database, IPasswordHasher<AppCredential> passwordHasher) : PageModel
{
    [BindProperty, Required, StringLength(128, MinimumLength = 12)]
    [DataType(DataType.Password), Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [BindProperty, Required, DataType(DataType.Password), Compare(nameof(Password))]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsHostRequest()) return NotFound();

        if (await database.AppCredentials.AnyAsync(item => item.Id == 1))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!IsHostRequest()) return NotFound();

        if (await database.AppCredentials.AnyAsync(item => item.Id == 1))
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var now = DateTime.UtcNow;
        var credential = new AppCredential
        {
            CreatedAtUtc = now,
            PasswordChangedAtUtc = now,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        credential.PasswordHash = passwordHasher.HashPassword(credential, Password);
        database.AppCredentials.Add(credential);

        try
        {
            await database.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Password setup has already been completed.");
            return Page();
        }

        return RedirectToPage("/Login", new { setup = "complete" });
    }

    private bool IsHostRequest() => HttpContext.Connection.RemoteIpAddress is { } address && IPAddress.IsLoopback(address);
}
