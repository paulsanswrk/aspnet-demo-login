using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using IdentityPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRCoder;

namespace IdentityPortal.Pages.Account;

public class EnableAuthenticatorModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    UrlEncoder urlEncoder) : PageModel
{
    private const string AuthenticatorUriFormat =
        "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string SharedKey { get; set; } = string.Empty;
    public string QrCodeBase64 { get; set; } = string.Empty;
    public string? StatusMessage { get; set; }

    public class InputModel
    {
        [Required]
        [Display(Name = "Verification Code")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "The code must be exactly 6 digits.")]
        public string Code { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToPage("/Account/Login");

        await LoadSharedKeyAndQrCodeAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToPage("/Account/Login");

        if (!ModelState.IsValid)
        {
            await LoadSharedKeyAndQrCodeAsync(user);
            return Page();
        }

        var code = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

        var isTokenValid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            code);

        if (!isTokenValid)
        {
            ModelState.AddModelError("Input.Code", "Verification code is invalid.");
            await LoadSharedKeyAndQrCodeAsync(user);
            return Page();
        }

        // Enable 2FA
        await userManager.SetTwoFactorEnabledAsync(user, true);

        // Update security stamp to invalidate other sessions
        await userManager.UpdateSecurityStampAsync(user);

        // Re-sign in with 2FA claims
        await signInManager.SignOutAsync();
        await signInManager.SignInWithClaimsAsync(user, isPersistent: false,
        [
            new Claim("amr", "mfa"),
            new Claim("2fa-verified", "true")
        ]);

        return RedirectToPage("/Dashboard");
    }

    private async Task LoadSharedKeyAndQrCodeAsync(ApplicationUser user)
    {
        // Reset the authenticator key to generate a fresh one
        var unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);

        if (string.IsNullOrEmpty(unformattedKey))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        }

        SharedKey = FormatKey(unformattedKey!);

        var email = await userManager.GetEmailAsync(user);
        var authenticatorUri = string.Format(
            AuthenticatorUriFormat,
            urlEncoder.Encode("IdentityPortal"),
            urlEncoder.Encode(email!),
            unformattedKey);

        // Generate QR code using QRCoder
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(5, [255, 255, 255], [30, 30, 46]);
        QrCodeBase64 = Convert.ToBase64String(qrCodeBytes);
    }

    private static string FormatKey(string unformattedKey)
    {
        var sb = new StringBuilder();
        var currentPosition = 0;

        while (currentPosition + 4 < unformattedKey.Length)
        {
            sb.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }

        if (currentPosition < unformattedKey.Length)
            sb.Append(unformattedKey.AsSpan(currentPosition));

        return sb.ToString().ToUpperInvariant();
    }
}
