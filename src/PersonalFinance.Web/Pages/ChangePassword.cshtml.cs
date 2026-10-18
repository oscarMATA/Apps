using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Web.Data;
using PersonalFinance.Web.Data.Entities;

namespace PersonalFinance.Web.Pages;

public sealed class ChangePasswordModel(FinanceDbContext database, IPasswordHasher<AppCredential> passwordHasher) : PageModel
{
    [BindProperty, Required, DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [BindProperty, Required, StringLength(128, MinimumLength = 12), DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty, Required, DataType(DataType.Password), Compare(nameof(NewPassword))]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var credential = await database.AppCredentials.SingleOrDefaultAsync(item => item.Id == 1);
        if (credential is null || passwordHasher.VerifyHashedPassword(credential, credential.PasswordHash, CurrentPassword) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(CurrentPassword), "The current password is incorrect.");
            return Page();
        }

        credential.PasswordHash = passwordHasher.HashPassword(credential, NewPassword);
        credential.SecurityStamp = Guid.NewGuid().ToString("N");
        credential.PasswordChangedAtUtc = DateTime.UtcNow;
        credential.FailedLoginCount = 0;
        credential.LockoutUntilUtc = null;
        await database.SaveChangesAsync();

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Login", new { changed = "true" });
    }
}
