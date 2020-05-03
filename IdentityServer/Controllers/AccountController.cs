﻿using System.Security.Claims;
using IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Shared.Identity;

namespace IdentityServer.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly UserManager<ApplicationUser> _userManager;
        public AccountController(SignInManager<ApplicationUser> signInManager, 
            UserManager<ApplicationUser> userManager,
            IIdentityServerInteractionService interaction)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _interaction = interaction;
        }
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            return View(new LoginViewModel{ReturnUrl = returnUrl});
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel)
        {
            var user = await _userManager.FindByEmailAsync(loginViewModel.Login);

            if (user == null)
            {
                return BadRequest("Wrong email or password");
            }

            var result = await _signInManager.PasswordSignInAsync(loginViewModel.Login, loginViewModel.Password, false, false);

            if (result.Succeeded)
            {
                return Redirect(loginViewModel.ReturnUrl);
            }
            else if(result.IsLockedOut)
            {
                
            }
            return View(loginViewModel);
        }

        public IActionResult Register(string returnUrl)
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
            var user = new ApplicationUser()
            {
                Email = registerViewModel.Login,
                UserName = registerViewModel.Login
            };
            var result = await _userManager.CreateAsync(user, registerViewModel.Password);

            if (result.Succeeded)
            {
                await _userManager.AddClaimAsync(user, new Claim("userName", user.UserName));
                await _userManager.AddClaimAsync(user, new Claim("email", user.Email));
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