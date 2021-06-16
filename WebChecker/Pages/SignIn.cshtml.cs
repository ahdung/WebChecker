using AhDung.WebChecker.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AhDung.WebChecker.Pages
{
    //[IgnoreAntiforgeryToken]
    [BindProperties]
    public class SignInModel : PageModel
    {
        readonly AppSettings _settings;

        public User UserInfo { get; set; }

        public string ErrorMessage { get; set; }

        public SignInModel(AppSettings settings)
        {
            _settings = settings;
        }

        public IActionResult OnGet(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return Redirect(returnUrl ?? "~/");
            }

            return Page();
        }

        public async Task<IActionResult> OnPost(string returnUrl)
        {
            if (ModelState.IsValid && ValidateUser(out var user))
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, user.Name),
                };

                await HttpContext.SignInAsync(
                    new ClaimsPrincipal(new ClaimsIdentity(claims, "cookies")),
                    new AuthenticationProperties
                    {
                        IsPersistent = true
                    });
                return Redirect(returnUrl ?? "~/");
            }

            ErrorMessage = "用户名或密码错误！";
            return Page();
        }

        bool ValidateUser(out User user)
        {
            user = _settings.Users.Find(x => x.Enabled
                                             && string.Equals(x.Name, UserInfo.Name, StringComparison.OrdinalIgnoreCase)
                                             && x.Password == UserInfo.Password);
            return user != null;
        }
    }
}