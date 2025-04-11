using AccreditationSystem.Pages.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace AccreditationSystem.Pages.Auth
{
    [BindProperties]
    public class ForgotPasswordModel : PageModel
    {
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public ForgotPasswordModel(IEmailService emailService, IConfiguration configuration)
        {
            _emailService = emailService;
            _configuration = configuration;
        }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = "";

        public string errorMessage = "";
        public string successMessage = "";

        public void OnGet()
        {
            // Just display the form
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                errorMessage = "Please provide a valid email address";
                return Page();
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Check if the email exists
                    string checkQuery = "SELECT COUNT(*) FROM users WHERE email = @Email";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Email", Email);
                        int userCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                        if (userCount == 0)
                        {
                            // Don't reveal that the user doesn't exist for security reasons
                            successMessage = "If your email is registered, you will receive password reset instructions.";
                            return Page();
                        }
                    }

                    // Generate a password reset token
                    string resetToken = GenerateResetToken();
                    DateTime tokenExpiry = DateTime.Now.AddHours(24);

                    // Store the token in the database
                    string insertTokenQuery = @"
                        IF EXISTS (SELECT 1 FROM password_reset_tokens WHERE email = @Email)
                            UPDATE password_reset_tokens SET token = @Token, expiry_date = @ExpiryDate, created_at = GETDATE() WHERE email = @Email
                        ELSE
                            INSERT INTO password_reset_tokens (email, token, expiry_date, created_at)
                            VALUES (@Email, @Token, @ExpiryDate, GETDATE())";

                    using (SqlCommand insertTokenCommand = new SqlCommand(insertTokenQuery, connection))
                    {
                        insertTokenCommand.Parameters.AddWithValue("@Email", Email);
                        insertTokenCommand.Parameters.AddWithValue("@Token", resetToken);
                        insertTokenCommand.Parameters.AddWithValue("@ExpiryDate", tokenExpiry);
                        await insertTokenCommand.ExecuteNonQueryAsync();
                    }

                    // Generate reset password link
                    string resetLink = Url.Page(
                        "/Auth/ResetPassword",
                        pageHandler: null,
                        values: new { email = Email, token = resetToken },
                        protocol: Request.Scheme);

                    // Prepare email body
                    string emailBody = $@"
                        <h2>Password Reset Request</h2>
                        <p>You requested to reset your password for your BestShop account. Please click the link below to set a new password:</p>
                        <p><a href='{resetLink}'>Reset Your Password</a></p>
                        <p>This link will expire in 24 hours.</p>
                        <p>If you didn't request this password reset, please ignore this email or contact support if you have concerns.</p>
                        <p>Best regards,<br>The BestShop Team</p>";

                    // Send reset password email
                    await _emailService.SendEmailAsync(
                        Email,
                        "BestShop - Password Reset Request",
                        emailBody);
                }

                successMessage = "If your email is registered, you will receive password reset instructions.";
                return Page();
            }
            catch (Exception ex)
            {
                errorMessage = $"An error occurred: {ex.Message}";
                return Page();
            }
        }

        private string GenerateResetToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var tokenData = new byte[32];
                rng.GetBytes(tokenData);
                return Convert.ToBase64String(tokenData)
                    .Replace("/", "_")
                    .Replace("+", "-")
                    .Replace("=", "");
            }
        }
    }
}
