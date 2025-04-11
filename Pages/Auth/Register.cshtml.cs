using AccreditationSystem.Pages.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace AccreditationSystem.Pages.Auth
{
    [BindProperties]
    public class RegisterModel : PageModel
    {
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public RegisterModel(IEmailService emailService, IConfiguration configuration)
        {
            _emailService = emailService;
            _configuration = configuration;
        }

        [Required(ErrorMessage = "The First Name is required")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "The Last Name is required")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "The Email is required"), EmailAddress]
        public string Email { get; set; } = "";

        public string? Phone { get; set; } = "";

        [Required(ErrorMessage = "The Address is required")]
        public string Address { get; set; } = "";

        [Required(ErrorMessage = "The Password is required")]
        [StringLength(50, ErrorMessage = "Password must be between 5 and 50 characters", MinimumLength = 5)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Comfirm password is required")]
        [Compare("Password", ErrorMessage = "Password and Comfirm Password do not match")]
        public string ComfirmPassword { get; set; } = "";

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = "customer"; // Default role

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
                errorMessage = "Data validation failed";
                return Page();
            }

            if (Phone == null) Phone = "";

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Check if email already exists
                    string checkQuery = "SELECT COUNT(*) FROM users WHERE email = @Email";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Email", Email);
                        int existingCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (existingCount > 0)
                        {
                            errorMessage = "Email already registered. Please use a different email.";
                            return Page();
                        }
                    }

                    // Insert new user
                    string insertQuery = @"INSERT INTO users (firstname, lastname, email, phone, address, password, role, created_at) 
                                         VALUES (@FirstName, @LastName, @Email, @Phone, @Address, @Password, @Role, GETDATE())";

                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@FirstName", FirstName);
                        command.Parameters.AddWithValue("@LastName", LastName);
                        command.Parameters.AddWithValue("@Email", Email);
                        command.Parameters.AddWithValue("@Phone", Phone);
                        command.Parameters.AddWithValue("@Address", Address);
                        command.Parameters.AddWithValue("@Password", HashPassword(Password));
                        command.Parameters.AddWithValue("@Role", Role);

                        command.ExecuteNonQuery();
                    }
                }

                // Generate verification token
                string verificationToken = GenerateVerificationToken();

                // Generate verification link
                string verificationLink = Url.Page(
                    "/Auth/VerifyEmail",
                    pageHandler: null,
                    values: new { email = Email, token = verificationToken },
                    protocol: Request.Scheme);

                // Prepare email body
                string emailBody = $@"
                    <h2>Welcome to BestShop, {FirstName}!</h2>
                    <p>Thank you for registering with us. Please verify your email address by clicking the link below:</p>
                    <p><a href='{verificationLink}'>Verify Email Address</a></p>
                    <p>If you didn't create this account, please ignore this email.</p>
                    <p>Best regards,<br>The BestShop Team</p>";

                // Send verification email
                await _emailService.SendEmailAsync(
                    Email,
                    "BestShop - Verify Your Email Address",
                    emailBody);

                successMessage = "Account created successfully! Please check your email to verify your account.";
                return Page();
            }
            catch (Exception ex)
            {
                errorMessage = $"Error creating account: {ex.Message}";
                return Page();
            }
        }

        private string GenerateVerificationToken()
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

        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
