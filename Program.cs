using IdentityPortal.Data;
using IdentityPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ───────────────────────────── EF Core ─────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Configure Forwarded Headers for Nginx ──
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Only loopback proxies are allowed by default. 
    // Clear the KnownNetworks and KnownProxies collections to accept headers from Nginx.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ───────────────────────────── Identity ─────────────────────────────
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password policy
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;

        // Lockout policy
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User settings
        options.User.RequireUniqueEmail = true;

        // Sign-in settings (no email confirm for demo)
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ───────────────────────────── Cookie ─────────────────────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LogoutPath = "/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;

    // Security hardening
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = ".Identity.Portal.Auth";
});

// ── Security stamp validation (standard interval) ──
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    // The default is 30 minutes. Setting to zero causes ad-hoc claims (like 2FA) 
    // to be dropped on every request when the principal is refreshed.
    options.ValidationInterval = TimeSpan.FromMinutes(30);
});

// ───────────────────────────── Authorization ─────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Require2FA", policy =>
        policy.RequireAssertion(context =>
        {
            // Check for amr claim containing "mfa"
            // This is the standard claim added by Identity for 2FA
            if (context.User.HasClaim(c => c.Type == "amr" && c.Value == "mfa"))
                return true;

            // Fallback: check for our custom 2fa-verified claim
            return context.User.HasClaim(c => c.Type == "2fa-verified" && c.Value == "true");
        }));
});

builder.Services.AddRazorPages();

var app = builder.Build();

// ───────────────── Apply migrations automatically ─────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// ── Apply Forwarded Headers BEFORE other middleware ──
app.UseForwardedHeaders();

// ───────────────────────── HTTP Pipeline ──────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
