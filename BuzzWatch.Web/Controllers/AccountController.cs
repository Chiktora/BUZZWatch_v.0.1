using BuzzWatch.Contracts.Auth;
using BuzzWatch.Web.Models;
using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace BuzzWatch.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            ApiClient apiClient,
            ILogger<AccountController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            if (ModelState.IsValid)
            {
                try
                {
                    // Get JWT token from API
                    var response = await _apiClient.PostAsync("/api/v1/auth/login", 
                        new LoginRequest { Email = model.Email, Password = model.Password });
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                        if (loginResponse != null)
                        {
                            // Store token in session
                            HttpContext.Session.SetString("JwtToken", loginResponse.Token);
                            
                            // Also store in TempData for JavaScript
                            TempData["JwtToken"] = loginResponse.Token;
                            
                            // Create claims for cookie authentication
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, model.Email),
                                new Claim("JwtToken", loginResponse.Token)
                            };
                            
                            // Add role claim from the JWT token
                            if (loginResponse.Role != null)
                            {
                                claims.Add(new Claim(ClaimTypes.Role, loginResponse.Role));
                                _logger.LogInformation("Added role {Role} for user {Email}", loginResponse.Role, model.Email);
                            }

                            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                            var authProperties = new AuthenticationProperties
                            {
                                IsPersistent = model.RememberMe
                            };

                            // Sign in with cookies
                            await HttpContext.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                new ClaimsPrincipal(claimsIdentity),
                                authProperties);
                            
                            _logger.LogInformation("User logged in with JWT");
                            
                            if (string.IsNullOrEmpty(returnUrl))
                                return RedirectToAction("Index", "Dashboard");
                            
                            return LocalRedirect(returnUrl);
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        // Specific message for inactive accounts
                        _logger.LogWarning("Login attempt for inactive account: {Email}", model.Email);
                        ModelState.AddModelError(string.Empty, "This account has been deactivated. Please contact an administrator.");
                    }
                    else
                    {
                        _logger.LogWarning("Failed login attempt for {Email}. StatusCode: {StatusCode}", 
                            model.Email, response.StatusCode);
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    }
                    
                    return View(model);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during login");
                    ModelState.AddModelError(string.Empty, "An error occurred during login.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Clear the JWT token from session
            HttpContext.Session.Remove("JwtToken");
            
            // Sign out from cookie authentication
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            return RedirectToAction("Index", "Home");
        }
        
        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize]
        public IActionResult Preferences()
        {
            var model = new UserPreferencesViewModel
            {
                // Default values, in a real app these would come from the database
                Theme = "light",
                DashboardLayout = "grid",
                MetricPreference = "celsius",
                DefaultTimeRange = 24
            };
            
            return View(model);
        }
        
        [Authorize]
        [HttpPost]
        public IActionResult Preferences(UserPreferencesViewModel model)
        {
            if (ModelState.IsValid)
            {
                // In a real app, save preferences to the database
                // For now, we'll just return success
                TempData["SuccessMessage"] = "Your preferences have been saved.";
                return RedirectToAction("Preferences");
            }
            
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> TestApiAccess()
        {
            try
            {
                // Check if token exists
                var token = HttpContext.Session.GetString("JwtToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Content("No JWT token found in session. Please log in again.");
                }

                // Try a simple API call
                var response = await _apiClient.GetAsync("api/v1/devices");
                
                if (response.IsSuccessStatusCode)
                {
                    return Content($"API access successful. Token: {token.Substring(0, 20)}...");
                }
                else
                {
                    return Content($"API call failed. Status: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                return Content($"Error testing API access: {ex.Message}");
            }
        }

        [Authorize]
        public async Task<IActionResult> TestAdminEndpoint()
        {
            try
            {
                // Check if token exists
                var token = HttpContext.Session.GetString("JwtToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Content("No JWT token found in session. Please log in again.");
                }

                // Try calling the admin users endpoint directly
                var response = await _apiClient.GetAsync("api/v1/admin/users");
                
                return Content($"API call status: {response.StatusCode}, Reason: {response.ReasonPhrase}\n\n" +
                              $"Request URL: {response.RequestMessage?.RequestUri}\n\n" +
                              $"Headers: {string.Join(", ", response.RequestMessage?.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");
            }
            catch (Exception ex)
            {
                return Content($"Error testing admin endpoint: {ex.Message}");
            }
        }

        [Authorize]
        public IActionResult DecodeJwt()
        {
            try
            {
                // Get the JWT token from session
                var token = HttpContext.Session.GetString("JwtToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Content("No JWT token found in session. Please log in again.");
                }

                // Parse the JWT token (without validation)
                var parts = token.Split('.');
                if (parts.Length != 3)
                {
                    return Content("Invalid JWT token format.");
                }

                // Decode the header and payload
                string DecodePart(string part)
                {
                    // Add padding if needed
                    string padded = part;
                    while (padded.Length % 4 != 0)
                    {
                        padded += "=";
                    }
                    
                    // Convert from base64url to base64
                    padded = padded.Replace('-', '+').Replace('_', '/');
                    
                    // Decode
                    var bytes = Convert.FromBase64String(padded);
                    return System.Text.Encoding.UTF8.GetString(bytes);
                }

                var header = DecodePart(parts[0]);
                var payload = DecodePart(parts[1]);

                // Format the output
                var output = new StringBuilder();
                output.AppendLine("JWT Token Analysis");
                output.AppendLine("=================");
                output.AppendLine();
                output.AppendLine("Header:");
                output.AppendLine(header);
                output.AppendLine();
                output.AppendLine("Payload:");
                output.AppendLine(payload);
                output.AppendLine();
                
                // Also show cookie claims
                output.AppendLine("Cookie Claims:");
                foreach (var claim in User.Claims)
                {
                    output.AppendLine($"- {claim.Type}: {claim.Value}");
                }

                return Content(output.ToString(), "text/plain");
            }
            catch (Exception ex)
            {
                return Content($"Error decoding JWT: {ex.Message}\n\n{ex.StackTrace}");
            }
        }
    }

    public class LoginViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
} 