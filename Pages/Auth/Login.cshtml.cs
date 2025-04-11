using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace AccreditationSystem.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public LoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = "";

        public string ErrorMessage { get; set; } = "";

        public void OnGet()
        {
            // Just display the login form
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                // Show specific validation errors
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToList();

                foreach (var error in errors)
                {
                    ErrorMessage += $"Field: {error.Key}, Errors: {string.Join(", ", error.Errors.Select(e => e.ErrorMessage))}\n";
                }
                return Page();
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Get user with matching email
                    string query = "SELECT id, firstname, email, password, role FROM users WHERE email = @Email";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Email", Email);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Get stored password hash
                                string storedPassword = reader["password"].ToString();
                                int userId = Convert.ToInt32(reader["id"]);
                                string firstName = reader["firstname"].ToString();
                                string userRole = reader["role"].ToString();

                                // Calculate hash of entered password
                                string hashedInputPassword = HashPassword(Password);

                                // Add these lines for debugging
                                if (storedPassword != hashedInputPassword)
                                {
                                    ErrorMessage = "Password mismatch. Contact administrator.";
                                    return Page();
                                }

                                // Password is correct, set session variables
                                HttpContext.Session.SetInt32("UserId", userId);
                                HttpContext.Session.SetString("UserName", firstName);
                                HttpContext.Session.SetString("UserEmail", Email);
                                HttpContext.Session.SetString("UserRole", userRole);

                                // Redirect based on role
                                if (userRole.ToLower() == "admin")
                                {
                                    return RedirectToPage("/Admin/Dashboard");
                                }
                                else
                                {
                                    return RedirectToPage("/Customer/Dashboard");
                                }
                            }
                            else
                            {
                                ErrorMessage = "Email not found in system.";
                                return Page();
                            }
                        }
                    }
                }

                // We should only get here if the email wasn't found
                ErrorMessage = "Login failed: Invalid credentials.";
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login failed: {ex.Message}";
                return Page();
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
