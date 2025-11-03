using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Akwarium.Pages
{
    public class AdminPanel : PageModel
    {
        public string? UserName { get; set; }

        public IActionResult OnGet()
        {
            var role = HttpContext.Session.GetString("UserRole");

            // jeœli nie admin – wracamy na stronê g³ówn¹
            if (role != "Admin")
            {
                return RedirectToPage("/Index");
            }

            UserName = HttpContext.Session.GetString("UserName");
            return Page();
        }
    }
}
