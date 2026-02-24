using IdentityPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityPortal.Pages;

[Authorize(Policy = "Require2FA")]
public class DashboardModel(UserManager<ApplicationUser> userManager) : PageModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public async Task OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is not null)
        {
            FullName = user.FullName;
            Email = user.Email ?? string.Empty;
            CreatedAt = user.CreatedAt;
        }
    }
}
