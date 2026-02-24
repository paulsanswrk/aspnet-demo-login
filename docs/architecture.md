# Architecture

## Project Structure

```
IdentityPortal/
├── Data/
│   └── ApplicationDbContext.cs      # EF Core context (primary constructor)
├── Models/
│   └── ApplicationUser.cs           # Custom IdentityUser
├── Migrations/
│   └── *_InitialCreate.cs           # Auto-generated Identity schema
├── Pages/
│   ├── Account/
│   │   ├── Register.cshtml(.cs)     # User registration
│   │   ├── Login.cshtml(.cs)        # Password authentication
│   │   ├── LoginWith2fa.cshtml(.cs) # TOTP challenge
│   │   ├── EnableAuthenticator.cshtml(.cs) # QR code + 2FA setup
│   │   ├── Logout.cshtml(.cs)       # Sign out
│   │   └── AccessDenied.cshtml(.cs) # 2FA enforcement redirect
│   ├── Dashboard.cshtml(.cs)        # Protected area (Require2FA)
│   ├── Index.cshtml(.cs)            # Landing page
│   └── Shared/
│       └── _Layout.cshtml           # Dark-themed layout
├── wwwroot/css/
│   └── site.css                     # Custom design system
├── Program.cs                       # DI, middleware, policies
└── appsettings.json                 # Connection string
```

---

## Design Decisions

### .NET 10 Features Used

| Feature | Where | Example |
|---------|-------|---------|
| Primary Constructors | All PageModels, DbContext | `class LoginModel(SignInManager<ApplicationUser> sm) : PageModel` |
| File-scoped Namespaces | All `.cs` files | `namespace IdentityPortal.Models;` |
| Collection Expressions | LoginWith2fa, EnableAuthenticator | `[new Claim("amr", "mfa")]` |
| Nullable Reference Types | Throughout | `string? ErrorMessage` |

### Custom Identity User

`ApplicationUser` extends `IdentityUser` with:
- `FullName` (string) — display name
- `CreatedAt` (DateTime) — registration timestamp

### No Bootstrap

The layout uses a custom CSS design system (`site.css`) with CSS custom properties instead of Bootstrap. This provides:
- Smaller payload (no unused CSS)
- Dark theme with glassmorphism effects
- Full design control via `--var` tokens

---

## Data Flow

```
┌─────────────┐     ┌──────────────────────┐     ┌─────────────────┐
│  Razor Page  │────▶│  ASP.NET Core Identity│────▶│   MSSQL Server  │
│  (UI Layer)  │     │  UserManager/SignIn   │     │ IdentityPortalDb│
└─────────────┘     └──────────────────────┘     └─────────────────┘
       │                      │
       │              ┌───────┴────────┐
       │              │ SecurityStamp  │
       │              │ Validation     │
       │              └───────┬────────┘
       │                      │
       ▼                      ▼
 ┌───────────┐        ┌─────────────┐
 │  QRCoder  │        │Cookie Auth  │
 │ (QR Gen)  │        │+ Claims     │
 └───────────┘        └─────────────┘
```

### Database Tables (auto-created by Identity)

| Table | Purpose |
|-------|---------|
| `AspNetUsers` | User accounts + `FullName`, `CreatedAt` |
| `AspNetUserTokens` | Authenticator keys (TOTP secrets) |
| `AspNetUserClaims` | User claims (amr, 2fa-verified) |
| `AspNetRoles` | Role definitions |
| `AspNetUserRoles` | User ↔ Role mapping |
| `AspNetUserLogins` | External login providers |
| `AspNetRoleClaims` | Role-based claims |

---

## Dependency Injection (`Program.cs`)

Services are registered in this order:

1. **DbContext** — `ApplicationDbContext` with SQL Server provider
2. **Identity** — `ApplicationUser` + `IdentityRole` with password/lockout options
3. **Cookie** — Login/AccessDenied/Logout paths, 2-hour sliding expiry
4. **SecurityStamp** — `TimeSpan.Zero` validation interval
5. **Authorization** — `Require2FA` policy
6. **Razor Pages** — standard
