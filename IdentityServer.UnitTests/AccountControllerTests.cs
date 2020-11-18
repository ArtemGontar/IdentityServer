using IdentityServer.Controllers;
using IdentityServer.Models;
using IdentityServer.UnitTests.Fakes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Moq;
using OpenTracing;
using Shared.Identity;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace IdentityServer.UnitTests
{

    public class AccountControllerTests
    {
        private readonly AccountController _accountController;
        private Mock<FakeUserManager> _mockUserManager;
        private Mock<FakeSignInManager> _mockSignInManager;
        private Mock<IOptions<AccountOptions>> _mockOptions;
        private Mock<ITracer> _tracer;
        public AccountControllerTests()
        {
            _mockSignInManager = new Mock<FakeSignInManager>();
            _mockUserManager = new Mock<FakeUserManager>();
            _mockOptions = new Mock<IOptions<AccountOptions>>();

            _accountController = new AccountController(_mockUserManager.Object, _mockSignInManager.Object, _mockOptions.Object, null, _tracer.Object);
        }

        [Fact]
        public async Task GetLoginPage_ShouldResturnsPage()
        {
            //Arrange
            var returnUrl = "http://localhost:4200";
            _accountController.ControllerContext = new ControllerContext();
            _accountController.ControllerContext.HttpContext = new DefaultHttpContext();
            var authManager = new Mock<IAuthenticationService>();
            authManager.Setup(s => s.SignOutAsync(It.IsAny<HttpContext>(),
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        It.IsAny<AuthenticationProperties>())).
                        Returns(Task.FromResult(true));
            var servicesMock = new Mock<IServiceProvider>();
            servicesMock.Setup(sp => sp.GetService(typeof(IAuthenticationService))).Returns(authManager.Object);
            servicesMock.Setup(sp => sp.GetService(typeof(IUrlHelperFactory))).Returns(new UrlHelperFactory());
            _ = servicesMock.Setup(sp => sp.GetService(typeof(ITempDataDictionaryFactory)))
                .Returns(new TempDataDictionaryFactory(new SessionStateTempDataProvider(null)));

            _accountController.ControllerContext.HttpContext.RequestServices = servicesMock.Object;

            //Act
            var result = (ViewResult)await _accountController.Login(returnUrl);

            //Assert
            Assert.Equal(typeof(LoginViewModel).FullName, result.Model.ToString());
        }

        [Fact]
        public async Task PostLogin_CorrectLoginAndPassword_ShouldLogined()
        {
            //Arrange
            var loginViewModel = new LoginViewModel()
            {
                Login = "anonymoysLogin",
                Password = "anonymoysPassword",
                ReturnUrl = "http://localhost:4200"
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(new ApplicationUser());
            _mockSignInManager.Setup(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            //Act
            var result = (RedirectResult)(await _accountController.Login(loginViewModel));

            //Assert
            Assert.Equal(loginViewModel.ReturnUrl, result.Url);
        }

        [Fact]
        public async Task PostLogin_CorrectLoginAndPassword_NotFoundUser_ShouldTempDataError()
        {
            //Arrange
            var loginViewModel = new LoginViewModel()
            {
                Login = "anonymoysLogin",
                Password = "anonymoysPassword",
                ReturnUrl = "http://localhost:4200"
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(default(ApplicationUser));

            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            _accountController.TempData = tempData;
            //Act
            var result = (ViewResult)await _accountController.Login(loginViewModel);

            //Assert
            Assert.Equal(typeof(LoginViewModel).FullName, result.Model.ToString());
            Assert.Equal("Login or Password incorrect", result.TempData["error"]);
        }

        [Fact]
        public async Task PostLogin_CorrectLoginAndPassword_WrongLoginOrPassword_ShouldTempDataError()
        {
            //Arrange
            var loginViewModel = new LoginViewModel()
            {
                Login = "anonymoysLogin",
                Password = "anonymoysPassword",
                ReturnUrl = "http://localhost:4200"
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(new ApplicationUser());
            _mockSignInManager.Setup(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            _accountController.TempData = tempData;
            //Act
            var result = (ViewResult)await _accountController.Login(loginViewModel);

            //Assert
            Assert.Equal(typeof(LoginViewModel).FullName, result.Model.ToString());
            Assert.Equal("Login or Password incorrect", result.TempData["error"]);
        }

        [Fact]
        public async void GetRegisterPage_ShouldResturnsPage()
        {
            //Arrange
            var returnUrl = "http://localhost:4200";
            
            //Act
            var result = (ViewResult)_accountController.Register(returnUrl);

            //Assert
            Assert.Equal(typeof(RegisterViewModel).FullName, result.Model.ToString());
        }

        [Fact]
        public async Task PostRegister_CorrectLoginAndPasswords_ShouldLogined()
        {
            //Arrange
            var registerViewModel = new RegisterViewModel()
            {
                Login = "anonymoysLogin",
                Password = "anonymoysPassword",
                ConfirmPassword = "anonymoysPassword",
                ReturnUrl = "http://localhost:4200"
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(default(ApplicationUser));
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockSignInManager.Setup(x => x.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), null));

            //Act
            var result = (RedirectResult)(await _accountController.Register(registerViewModel));

            //Assert
            Assert.Equal(registerViewModel.ReturnUrl, result.Url);
        }

    }
}
