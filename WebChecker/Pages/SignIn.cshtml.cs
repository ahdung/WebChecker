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
        public User UserInfo { get; set; }

        public string ErrorMessage { get; set; }

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

            ErrorMessage = "�û������������";
            return Page();
        }

        bool ValidateUser(out User user)
        {
            user = AppSettings.Users.Find(x => x.Enabled
                                               && string.Equals(x.Name, UserInfo.Name, StringComparison.OrdinalIgnoreCase)
                                               && x.Password == UserInfo.Password);
            return user != null;
        }
    }
}