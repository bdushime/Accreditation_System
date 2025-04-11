using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccreditationSystem.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Check if user is logged in and is admin
            string userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || userRole.ToLower() != "admin")
            {
                return RedirectToPage("/Auth/Login");
            }

            return Page();
        }
    }
}
