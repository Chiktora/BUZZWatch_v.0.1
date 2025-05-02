using BuzzWatch.Contracts.Auth;
using BuzzWatch.Web.Models;
using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;

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
                            
                            // Add role claim for admin user
                            if (model.Email.ToLower() == "admin@local")
                            {
                                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
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
                    
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
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
    }

    public class LoginViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
} 