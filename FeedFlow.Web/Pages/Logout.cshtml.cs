using FeedFlow.Web.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FeedFlow.Web.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public LogoutModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public async Task<IActionResult> OnGet(string? returnUrl = "/")
        {
            await _signInManager.SignOutAsync();
            return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
        }
    }
}
