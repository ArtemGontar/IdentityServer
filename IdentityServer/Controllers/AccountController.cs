using System.Security.Claims;
using IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Shared.Identity;

namespace IdentityServer.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AccountOptions _accountOptions;
        public AccountController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IOptions<AccountOptions> accountOptions,
            IIdentityServerInteractionService interaction)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _interaction = interaction;
            _accountOptions = accountOptions.Value;
        }
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            return View(new LoginViewModel{ReturnUrl = returnUrl ?? _accountOptions.SPAUrl });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel)
        {
            var user = await _userManager.FindByEmailAsync(loginViewModel.Login);

            if (user == null)
            {
                return View(loginViewModel);
            }

            var result = await _signInManager.PasswordSignInAsync(loginViewModel.Login, loginViewModel.Password, false, false);
            
            if (result.Succeeded)
            {
                return Redirect(loginViewModel.ReturnUrl);
            }
            return View(loginViewModel);
        }

        public async Task<IActionResult> Register(string returnUrl)
        {
            return View(new RegisterViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel registerViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(registerViewModel);
            }
            
            var user = await _userManager.FindByEmailAsync(registerViewModel.Login);
            if (user != null)
            {
                return View(registerViewModel);
            }

            user = new ApplicationUser()
            {
                Email = registerViewModel.Login,
                UserName = registerViewModel.Login
            };
            
            var result = await _userManager.CreateAsync(user, registerViewModel.Password);
            if (result.Succeeded)
            {
                await _userManager.AddClaimAsync(user, new Claim("userName", user.UserName));
                await _userManager.AddClaimAsync(user, new Claim("email", user.Email));
                var identityRoleResult = await _userManager.AddToRoleAsync(user, SystemRoles.ClientRoleName);
                if (!identityRoleResult.Succeeded)
                {
                    return BadRequest(
                        $"System Role of User with email '{user.Email}' not added.");
                }

                //await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("role", Roles.User));
                await _signInManager.SignInAsync(user, false);
                
                return Redirect(registerViewModel.ReturnUrl);
            }

            return View(registerViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            return await Logout(new LogoutViewModel { LogoutId = logoutId });
        }

        [HttpPost]
        public async Task<IActionResult> Logout(LogoutViewModel model)
        {
            // delete authentication cookie
            await _signInManager.SignOutAsync();

            // set this so UI rendering sees an anonymous user
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            var logout = await _interaction.GetLogoutContextAsync(model.LogoutId);

            if (!string.IsNullOrEmpty(logout?.PostLogoutRedirectUri))
                return Redirect(logout.PostLogoutRedirectUri);

            return RedirectToAction(nameof(Login));
        }
    }
}