# Security

## Identity Configuration

All settings are configured in `Program.cs` via `AddIdentity<ApplicationUser, IdentityRole>()`.

### Password Policy

| Setting | Value | Purpose |
|---------|-------|---------|
| `RequiredLength` | 8 | Minimum password length |
| `RequireUppercase` | true | At least one uppercase letter |
| `RequireDigit` | true | At least one digit |
| `RequireNonAlphanumeric` | true | At least one special character |

### Account Lockout

| Setting | Value | Purpose |
|---------|-------|---------|
| `MaxFailedAccessAttempts` | 5 | Lock after 5 wrong attempts |
| `DefaultLockoutTimeSpan` | 15 minutes | Lockout duration |
| `AllowedForNewUsers` | true | New accounts can be locked |

Lockout is enforced on **both** stages:
- **Password entry** — `PasswordSignInAsync(lockoutOnFailure: true)`
- **2FA code entry** — `TwoFactorAuthenticatorSignInAsync` with lockout

---

## Two-Factor Authentication (TOTP)

### How It Works

The system uses the **HMAC-based One-Time Password (HOTP)** algorithm with a time-based variant (TOTP, RFC 6238):

1. A shared secret is generated and stored in `AspNetUserTokens`
2. The secret is encoded into an `otpauth://totp/` URI
3. The URI is rendered as a QR code (via QRCoder)
4. The authenticator app and the server independently compute 6-digit codes from the shared secret + current time
5. Codes rotate every 30 seconds

### Token Provider

ASP.NET Core Identity registers the `AuthenticatorTokenProvider` by default via `AddDefaultTokenProviders()`. This handles:
- Key generation: `UserManager.ResetAuthenticatorKeyAsync()`
- Key retrieval: `UserManager.GetAuthenticatorKeyAsync()`
- Code verification: `UserManager.VerifyTwoFactorTokenAsync()`

---

## Security Stamps

Security stamps are a unique hash stored on each user that changes when security-critical actions occur (password change, 2FA toggle, etc.).

### Configuration

```csharp
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});
```

Setting `TimeSpan.Zero` means the cookie middleware validates the stamp on **every request**. This ensures:
- Enabling 2FA immediately invalidates sessions without the `amr` claim
- Password changes instantly log out other sessions
- Disabling 2FA immediately takes effect

---

## Authorization Policy: `Require2FA`

```csharp
options.AddPolicy("Require2FA", policy =>
    policy.RequireAssertion(context =>
    {
        var amrClaim = context.User.FindFirst("amr");
        if (amrClaim is not null && amrClaim.Value == "mfa")
            return true;

        var tfaClaim = context.User.FindFirst("2fa-verified");
        return tfaClaim is not null && tfaClaim.Value == "true";
    }));
```

### How Claims Are Added

Claims are injected into the cookie via `SignInManager.SignInWithClaimsAsync()` at two points:
1. **After 2FA setup** — in `EnableAuthenticator.cshtml.cs`
2. **After 2FA login** — in `LoginWith2fa.cshtml.cs`

Both add:
- `amr` = `mfa` (standard Authentication Methods Reference)
- `2fa-verified` = `true` (custom fallback)

---

## Cookie Authentication

| Setting | Value |
|---------|-------|
| `LoginPath` | `/Account/Login` |
| `AccessDeniedPath` | `/Account/AccessDenied` |
| `LogoutPath` | `/Account/Logout` |
| `ExpireTimeSpan` | 2 hours |
| `SlidingExpiration` | true |

---

## Threat Mitigations Summary

| Threat | Mitigation |
|--------|-----------|
| Brute-force password | Account lockout (5 attempts / 15 min) |
| Brute-force 2FA code | Lockout applies to 2FA stage too |
| Session hijacking after 2FA change | Security stamp validation (`TimeSpan.Zero`) |
| Bypassing 2FA to access Dashboard | `Require2FA` policy checks claims |
| Weak passwords | 8+ chars with complexity requirements |
| Credential stuffing | Unique email requirement, generic error messages |
