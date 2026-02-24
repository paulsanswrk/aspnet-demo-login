using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using IdentityPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityPortal.Pages.Account;

public class LoginWith2faModel(
    SignInManager<ApplicationUser> signInManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? ErrorMessage { get; set; }
    public string Email { get; set; } = string.Empty;

    public class InputModel
    {
        [Required]
        [Display(Name = "Authentication Code")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "The code must be exactly 6 digits.")]
        public string TwoFactorCode { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Ensure the user has gone through the password screen first
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
            return RedirectToPage("/Account/Login");

        Email = user.Email ?? string.Empty;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
            return RedirectToPage("/Account/Login");

        Email = user.Email ?? string.Empty;

        var code = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

        var result = await signInManager.TwoFactorAuthenticatorSignInAsync(
            code,
            isPersistent: false,
            rememberClient: false);

        if (result.Succeeded)
        {
            // Identity's TwoFactorAuthenticatorSignInAsync might not automatically
            // include the "mfa" claim in the way our policy expects in all versions.
            // We manually re-sign with specific claims to be absolutely sure.
            
            await signInManager.SignOutAsync();
            await signInManager.SignInWithClaimsAsync(user, isPersistent: false,
            [
                new Claim("amr", "mfa"),
                new Claim("2fa-verified", "true")
            ]);

            return RedirectToPage("/Dashboard");
        }

        if (result.IsLockedOut)
        {
            ErrorMessage = "Account locked out due to too many failed attempts. Try again in 15 minutes.";
            return RedirectToPage();
        }

        ErrorMessage = "Invalid authenticator code.";
        return RedirectToPage();
    }
}
