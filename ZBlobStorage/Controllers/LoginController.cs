using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AESCTR_Standard;
using ZBlobStorage.Models;

namespace ZBlobStorage.Controllers
{
    public class LoginController : Controller
    {
        #region Fields
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginController> _logger;
        private readonly string _encryptionKey;
        #endregion
        #region Ctor
        public LoginController(IConfiguration configuration, ILogger<LoginController> logger)
        {
            _configuration = configuration;
            _logger=logger;
            _encryptionKey = _configuration.GetValue<string>("puller");
        }
        #endregion

        #region Method
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string userName, string password)
        {
            var users = _configuration.GetSection("Authentication:Users").Get<List<User>>();

            foreach (var user in users)
            {
                string decryptedUsername = AESCTR.Decrypt(user.UserName, _encryptionKey).Content;
                string decryptedPassword = AESCTR.Decrypt(user.Password, _encryptionKey).Content;

                if (userName == decryptedUsername && password == decryptedPassword)
                {
                    var claims = new[]
                    {
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, user.Role),
            };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties();

                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                             new ClaimsPrincipal(identity),
                                             authProperties);

                    return RedirectToAction("Index", "Home");
                }
            }

            TempData["ErrorMessage"] = "Invalid username or password.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
        #endregion
    }


}
