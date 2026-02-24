# Authentication Flow

## Overview

```
Register ──▶ EnableAuthenticator ──▶ Dashboard
                                        ▲
Login ──▶ LoginWith2fa ─────────────────┘
  │
  └──▶ AccessDenied (if 2FA not set up)
```

---

## 1. Registration (`/Account/Register`)

**Page**: `Register.cshtml` / `Register.cshtml.cs`

| Field | Validation |
|-------|-----------|
| Full Name | Required, 2–100 chars |
| Email | Required, valid email, unique |
| Password | Required, 8+ chars, uppercase + digit + special |
| Confirm Password | Must match Password |

**Flow**:
1. `UserManager.CreateAsync()` creates the `ApplicationUser`
2. `SignInManager.SignInAsync()` immediately signs in the user
3. Redirects to `/Account/EnableAuthenticator`

> The user is signed in but has no `amr` claim yet, so the `Require2FA` policy will deny access to the Dashboard.

---

## 2. Authenticator Setup (`/Account/EnableAuthenticator`)

**Page**: `EnableAuthenticator.cshtml` / `EnableAuthenticator.cshtml.cs`

**QR Code Generation**:
1. `UserManager.GetAuthenticatorKeyAsync()` retrieves or generates a TOTP secret
2. An `otpauth://totp/...` URI is constructed with the issuer, email, and secret
3. `QRCoder.PngByteQRCode` renders the URI as a PNG with a dark background
4. The PNG is embedded as a Base64 `<img>` tag
5. A formatted manual key is displayed as fallback

**Verification**:
1. User enters 6-digit code from their authenticator app
2. `UserManager.VerifyTwoFactorTokenAsync()` validates against the TOTP provider
3. On success:
   - `UserManager.SetTwoFactorEnabledAsync(user, true)` — enables 2FA
   - `UserManager.UpdateSecurityStampAsync(user)` — invalidates other sessions
   - `SignInManager.SignInWithClaimsAsync()` — re-signs in with `amr: mfa` and `2fa-verified: true`
4. Redirects to `/Dashboard`

---

## 3. Login — Step 1: Password (`/Account/Login`)

**Page**: `Login.cshtml` / `Login.cshtml.cs`

**Flow**:
1. `SignInManager.PasswordSignInAsync()` with `lockoutOnFailure: true`
2. Result handling:

| Result | Action |
|--------|--------|
| `Succeeded` | Redirect to `/Dashboard` (user has no 2FA) |
| `RequiresTwoFactor` | Redirect to `/Account/LoginWith2fa` |
| `IsLockedOut` | Display lockout message (15 min) |
| Other | Display "Invalid email or password" |

---

## 4. Login — Step 2: 2FA Challenge (`/Account/LoginWith2fa`)

**Page**: `LoginWith2fa.cshtml` / `LoginWith2fa.cshtml.cs`

**Flow**:
1. `OnGetAsync()` — calls `SignInManager.GetTwoFactorAuthenticationUserAsync()` to verify the user came from the password step
2. User enters 6-digit TOTP code
3. `SignInManager.TwoFactorAuthenticatorSignInAsync()` with lockout enabled
4. On success:
   - Signs out the partial 2FA session
   - Re-signs in with `amr: mfa` and `2fa-verified: true` claims
   - Redirects to `/Dashboard`

---

## 5. Dashboard (`/Dashboard`)

**Page**: `Dashboard.cshtml` / `Dashboard.cshtml.cs`

Protected by `[Authorize(Policy = "Require2FA")]` — the policy checks:
1. `amr` claim with value `mfa`, **OR**
2. `2fa-verified` claim with value `true`

If neither claim is present, the user is redirected to `/Account/AccessDenied`.

Displays: user's name, email, 2FA status, join date, and authentication method.

---

## 6. Access Denied (`/Account/AccessDenied`)

Shown when a user tries to reach the Dashboard without completing 2FA setup. Provides a "Set Up 2FA Now" button linking to `/Account/EnableAuthenticator`.

---

## 7. Logout (`/Account/Logout`)

- `POST` handler calls `SignInManager.SignOutAsync()`
- Clears the authentication cookie (including all claims)
- Displays a confirmation page with a "Sign In Again" link
