using Akwarium.Models;
using Akwarium.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Akwarium.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly AkwariumDbContext _context;

        public LoginModel(AkwariumDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Email jest wymagany.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy adres e-mail.")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Hasło jest wymagane.")]
        public string Password { get; set; } = string.Empty;

        // już jest:
        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            // nic specjalnego, ReturnUrl przyjdzie z query (?returnUrl=...)
        }

        public async Task<IActionResult> OnPostAsync()
        {


            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (user == null || !PasswordHasher.VerifyPassword(Password, user.Password))
            {
                ErrorMessage = "Niepoprawny email lub hasło.";
                ModelState.AddModelError(string.Empty, ErrorMessage);
                return Page();
            }

            HttpContext.Session.SetString("UserName", user.Nick);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserRole", user.Role);




            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                return Redirect(ReturnUrl);



            if (user.Role == "Admin")
            {
                return RedirectToPage("/AdminPanel");
            }
            else
            {
                return RedirectToPage("/Index");
            }

        }
    }
}
