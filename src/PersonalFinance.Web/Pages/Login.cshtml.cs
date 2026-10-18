using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Web.Data;
using PersonalFinance.Web.Data.Entities;

namespace PersonalFinance.Web.Pages;

public sealed class LoginModel(FinanceDbContext database, IPasswordHasher<AppCredential> passwordHasher) : PageModel
{
    [BindProperty, Required, DataType(DataType.Password), StringLength(128)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true, Name = "setup")]
    public string? SetupStatus { get; set; }

    [BindProperty(SupportsGet = true, Name = "changed")]
    public string? ChangedStatus { get; set; }

    public bool SetupComplete => SetupStatus == "complete";

    public bool PasswordChanged => ChangedStatus == "true";

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        if (!await database.AppCredentials.AnyAsync(item => item.Id == 1))
        {
            return RedirectToPage("/Setup");
        }

        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var credential = await database.AppCredentials.SingleOrDefaultAsync(item => item.Id == 1);
        if (credential is null)
        {
            return RedirectToPage("/Setup");
        }

        var now = DateTime.UtcNow;
        if (credential.LockoutUntilUtc is { } lockoutUntil && lockoutUntil > now)
        {
            ModelState.AddModelError(string.Empty, "Sign-in is temporarily unavailable. Try again later.");
            return Page();
        }

        var verification = passwordHasher.VerifyHashedPassword(credential, credential.PasswordHash, Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            credential.FailedLoginCount++;
            if (credential.FailedLoginCount >= 5)
            {
                credential.FailedLoginCount = 0;
                credential.LockoutUntilUtc = now.AddMinutes(15);
            }
            await database.SaveChangesAsync();
            ModelState.AddModelError(string.Empty, "The password is incorrect or sign-in is temporarily unavailable.");
            return Page();
        }

        if (credential.LockoutUntilUtc is not null || credential.FailedLoginCount > 0)
        {
            credential.LockoutUntilUtc = null;
            credential.FailedLoginCount = 0;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            credential.PasswordHash = passwordHasher.HashPassword(credential, Password);
        }
        await database.SaveChangesAsync();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "owner"),
            new Claim(ClaimTypes.Name, "Owner"),
            new Claim("security_stamp", credential.SecurityStamp)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true, AllowRefresh = true });

        return Url.IsLocalUrl(ReturnUrl) ? LocalRedirect(ReturnUrl!) : RedirectToPage("/Index");
    }
}
