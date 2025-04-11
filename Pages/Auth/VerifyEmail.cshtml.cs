using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccreditationSystem.Pages.Auth
{
    public class VerifyEmailModel : PageModel
    {
        public string Message { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                Message = "Invalid verification link.";
                return Page();
            }

            try
            {
                // TODO: Verify the token in your database
                // For demonstration, we'll assume verification is successful
                bool isValid = true;

                if (isValid)
                {
                    Message = "Email verified successfully! You can now log in.";
                }
                else
                {
                    Message = "Invalid or expired verification link.";
                }

                return Page();
            }
            catch (Exception ex)
            {
                Message = $"Error verifying email: {ex.Message}";
                return Page();
            }
        }
    }
}
