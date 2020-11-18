using System.Security.Claims;
using IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Shared.Identity;
using OpenTracing;

namespace IdentityServer.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AccountOptions _accountOptions;
        private readonly ITracer _tracer;
        public AccountController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IOptions<AccountOptions> accountOptions,
            IIdentityServerInteractionService interaction,
            ITracer tracer)
        {

            _userManager = userManager;
            _signInManager = signInManager;
            _accountOptions = accountOptions.Value;
            _interaction = interaction;
            _tracer = tracer;
        }
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            using (var scope = _tracer.BuildSpan("LoginView").StartActive(finishSpanOnDispose: true))
            {
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                scope.Span.Log("Get login method");
                return View(new LoginViewModel { ReturnUrl = returnUrl ?? _accountOptions.SPAUrl });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel)
        {
            using (var scope = _tracer.BuildSpan("Login").StartActive(finishSpanOnDispose: true))
            {
                if (!ModelState.IsValid)
                {
                    scope.Span.Log("ModelState not valid");
                    TempData["error"] = "Login or Password incorrect";
                    return View(loginViewModel);
                }
                var user = await _userManager.FindByEmailAsync(loginViewModel.Login);

                if (user == null)
                {
                    scope.Span.Log($"User with login {loginViewModel.Login} not found");
                    TempData["error"] = "Login or Password incorrect";
                    return View(loginViewModel);
                }

                var result = await _signInManager.PasswordSignInAsync(loginViewModel.Login, loginViewModel.Password, false, false);

                if (result.Succeeded)
                {

                    scope.Span.Log($"User with login {loginViewModel.Login} logined in");
                    return Redirect(loginViewModel.ReturnUrl);
                }


                scope.Span.Log($"Incorrect password for user with login {loginViewModel.Login} logined in");
                TempData["error"] = "Login or Password incorrect";

                return View(loginViewModel);

            }
        }

        public IActionResult Register(string returnUrl)
        {
            using (var scope = _tracer.BuildSpan("RegisterView").StartActive(finishSpanOnDispose: true))
            {
                scope.Span.Log("Get register view");
                return View(new RegisterViewModel { ReturnUrl = returnUrl });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel registerViewModel)
        {
            using (var scope = _tracer.BuildSpan("Register").StartActive(finishSpanOnDispose: true))
            {
                if (!ModelState.IsValid)
                {
                    TempData["error"] = "Fields typed incorrect";
                    return View(registerViewModel);
                }

                var user = await _userManager.FindByEmailAsync(registerViewModel.Login);
                if (user != null)
                {
                    TempData["error"] = "User with this login already exist";
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

                    await _userManager.AddClaimAsync(user, new Claim("role", SystemRoles.ClientRoleName));
                    await _userManager.AddClaimAsync(user, new Claim("userId", user.Id.ToString()));
                    await _signInManager.SignInAsync(user, false);

                    return Redirect(registerViewModel.ReturnUrl);
                }

                TempData["error"] = "Register failed";
                return View(registerViewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            using (var scope = _tracer.BuildSpan("LogoutView").StartActive(finishSpanOnDispose: true))
            {
                return await Logout(new LogoutViewModel { LogoutId = logoutId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout(LogoutViewModel model)
        {
            using (var scope = _tracer.BuildSpan("Logout").StartActive(finishSpanOnDispose: true))
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
}