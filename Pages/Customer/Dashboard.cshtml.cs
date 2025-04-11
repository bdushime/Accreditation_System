using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccreditationSystem.Pages.Customer
{
    public class DashboardModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Check if user is logged in
            string userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToPage("/Auth/Login");
            }

            return Page();
        }
    }
}
