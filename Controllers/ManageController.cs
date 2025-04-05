using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Secure_Student_Management_System.Models;
using System.Threading.Tasks;
using System.Linq;


namespace Secure_Student_Management_System.Controllers
{
    public class ManageController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAuthenticationSchemeProvider _schemeProvider;

        public ManageController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IAuthenticationSchemeProvider schemeProvider)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _schemeProvider = schemeProvider;
        }

        // GET: /Manage/Index
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Error", "Home");
            }

            var logins = await _userManager.GetLoginsAsync(user);
            var externalLogins = logins
                .Where(login => login.LoginProvider != "Google") // Filter out non-Google logins
                .Select(login => new ExternalLoginViewModel
                {
                    LoginProvider = login.LoginProvider,
                    ProviderKey = login.ProviderKey,
                    ProviderDisplayName = login.LoginProvider
                })
                .ToList();

            var model = new IndexViewModel
            {
                Logins = logins,
                ExternalLogins = externalLogins,
                HasPassword = await _userManager.HasPasswordAsync(user),
                TwoFactor = await _userManager.GetTwoFactorEnabledAsync(user),
                BrowserRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user)
            };

            return View(model);
        }

        // GET: /Manage/RemoveLogin
        public async Task<IActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Error", "Home");
            }

            var result = await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);

            if (result.Succeeded)
            {
                TempData["StatusMessage"] = "Your external login was removed.";
            }
            else
            {
                TempData["StatusMessage"] = "Error removing external login.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Manage/ExternalLogin
        public IActionResult ExternalLogin(string provider)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Manage");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        // GET: /Manage/ExternalLoginCallback
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            // Retrieve external login info
            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
            {
                TempData["ErrorMessage"] = "Error occurred while connecting to external login provider.";
                return RedirectToAction(nameof(Index));
            }

            // Get current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Error", "Home");
            }

            // Add external login to the user
            var result = await _userManager.AddLoginAsync(user, info);

            if (result.Succeeded)
            {
                // Refresh sign-in and update status message
                await _signInManager.RefreshSignInAsync(user);
                TempData["StatusMessage"] = "Your external login was added.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["ErrorMessage"] = "Error occurred while adding the external login.";
                return RedirectToAction(nameof(Index));
            }
        }


        // GET: /Manage/RemovePhoneNumber
        public IActionResult RemovePhoneNumber()
        {
            return View();
        }

        // POST: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePhoneNumber(RemovePhoneNumberViewModel model)
        {
            if (model == null)
            {
                TempData["ErrorMessage"] = "Invalid phone number data.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Error", "Home");
            }

            var result = await _userManager.SetPhoneNumberAsync(user, null);

            if (result.Succeeded)
            {
                TempData["StatusMessage"] = "Your phone number was removed.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error occurred while removing your phone number.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Manage/AddPhoneNumber
        public IActionResult AddPhoneNumber()
        {
            return View();
        }

        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (model == null || !ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid phone number data.";
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Error", "Home");
            }

            var result = await _userManager.SetPhoneNumberAsync(user, model.Number);

            if (result.Succeeded)
            {
                TempData["StatusMessage"] = "Your phone number was added.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["ErrorMessage"] = "Error occurred while adding your phone number.";
            }

            return View(model);
        }

        // GET: /Manage/ConfigureTwoFactor
        public IActionResult ConfigureTwoFactor()
        {
            return View();
        }

        // POST: /Manage/ConfigureTwoFactor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfigureTwoFactor(ConfigureTwoFactorViewModel model)
        {
            if (model == null || !ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid two-factor configuration data.";
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Error", "Home");
            }

            var selectedProvider = model.SelectedProvider;

            // Implement logic for configuring two-factor authentication (e.g., SMS, Authenticator App)
            // Example: Add or configure two-factor options based on the selected provider
            TempData["StatusMessage"] = $"Two-factor authentication configured with {selectedProvider}.";
            return RedirectToAction(nameof(Index));
        }
        // Controllers/ManageController.cs
        public async Task<IActionResult> ManageLogins()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var currentLogins = await _userManager.GetLoginsAsync(user);
            var otherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
                .Where(auth => currentLogins.All(ul => auth.Name != ul.LoginProvider))
                .ToList();

            return View(new ManageLoginsViewModel
            {
                CurrentLogins = currentLogins,
                OtherLogins = otherLogins,
                ShowRemoveButton = user.PasswordHash != null || currentLogins.Count > 1
            });
        }
    }
}
