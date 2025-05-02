using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace BuzzWatch.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ApiClient apiClient, ILogger<UsersController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Index()
        {
            try
            {
                // This will eventually call the API to get a list of users
                var users = new List<UserViewModel>
                {
                    new UserViewModel
                    {
                        Id = Guid.NewGuid(),
                        Email = "admin@local",
                        Name = "Admin User",
                        Role = "Admin",
                        IsActive = true
                    }
                };

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users");
                return View("Error");
            }
        }

        // GET: /Admin/Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // This will eventually call the API to create a user
                    // For now, just redirect back to the index
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating user");
                    ModelState.AddModelError("", "Error creating user. Please try again.");
                }
            }

            return View(model);
        }

        // GET: /Admin/Users/Edit/5
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                // This will eventually call the API to get the user details
                var user = new EditUserViewModel
                {
                    Id = id,
                    Email = "admin@local",
                    Name = "Admin User",
                    Role = "Admin",
                    IsActive = true
                };

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user {UserId}", id);
                return NotFound();
            }
        }

        // POST: /Admin/Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditUserViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // This will eventually call the API to update the user
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating user {UserId}", id);
                    ModelState.AddModelError("", "Error updating user. Please try again.");
                }
            }

            return View(model);
        }

        // GET: /Admin/Users/Devices/5
        public async Task<IActionResult> Devices(Guid id)
        {
            try
            {
                // This will eventually call the API to get the user's device permissions
                var viewModel = new UserDevicesViewModel
                {
                    UserId = id,
                    UserName = "Admin User",
                    UserEmail = "admin@local",
                    Devices = new List<UserDevicePermission>()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching devices for user {UserId}", id);
                return NotFound();
            }
        }

        // POST: /Admin/Users/AssignDevice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDevice(AssignDeviceViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // This will eventually call the API to assign the device to the user
                    return RedirectToAction(nameof(Devices), new { id = model.UserId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error assigning device to user");
                    ModelState.AddModelError("", "Error assigning device. Please try again.");
                }
            }

            // Rebuild the user devices view model
            var viewModel = new UserDevicesViewModel
            {
                UserId = model.UserId,
                UserName = "User Name", // This would be populated from the API
                UserEmail = "user@email.com", // This would be populated from the API
                Devices = new List<UserDevicePermission>() // This would be populated from the API
            };

            return View(nameof(Devices), viewModel);
        }
    }

    public class UserViewModel
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateUserViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class EditUserViewModel
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
    }

    public class UserDevicesViewModel
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public List<UserDevicePermission> Devices { get; set; } = new();
    }

    public class UserDevicePermission
    {
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceLocation { get; set; } = string.Empty;
        public bool HasAccess { get; set; }
        public bool CanManage { get; set; }
    }

    public class AssignDeviceViewModel
    {
        public Guid UserId { get; set; }
        public Guid DeviceId { get; set; }
        public bool HasAccess { get; set; }
        public bool CanManage { get; set; }
    }
} 