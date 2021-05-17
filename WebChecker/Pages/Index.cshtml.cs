using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace AhDung.WebChecker.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        public int PageRefreshIntervalSeconds { get; set; } = 10;

        public void OnGet()
        {
            if (int.TryParse(Request.Cookies["refresh"], out var refresh))
            {
                PageRefreshIntervalSeconds = refresh;
            }
        }

        public async Task<IActionResult> OnGetSignOutAsync()
        {
            await HttpContext.SignOutAsync();
            return Redirect("~/signin");
        }
    }
}