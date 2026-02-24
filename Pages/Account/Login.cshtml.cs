using System.ComponentModel.DataAnnotations;
using IdentityPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityPortal.Pages.Account;

public class LoginModel(SignInManager<ApplicationUser> signInManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required, EmailAddress, Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = await signInManager.PasswordSignInAsync(
            Input.Email,
            Input.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return RedirectToPage("/Dashboard");
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("/Account/LoginWith2fa");
        }

        if (result.IsLockedOut)
        {
            ErrorMessage = "Your account has been locked out due to too many failed attempts. Please try again in 15 minutes.";
            return Page();
        }

        ErrorMessage = "Invalid email or password.";
        return Page();
    }
}
