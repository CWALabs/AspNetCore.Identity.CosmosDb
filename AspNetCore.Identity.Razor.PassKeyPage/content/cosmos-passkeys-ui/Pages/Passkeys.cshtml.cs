using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspNetCore.Identity.Razor.PassKeyPage.Pages;

[Authorize]
public class PasskeysModel : PageModel
{
    public void OnGet()
    {
    }
}
