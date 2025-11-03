using Akwarium.Models;
using Akwarium.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Akwarium.Pages.Account
{
    public class ChangePasswordModel : PageModel
    {
        private readonly AkwariumDbContext _context;

        public ChangePasswordModel(AkwariumDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Podaj obecne hasło.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Podaj nowe hasło.")]
        [MinLength(4, ErrorMessage = "Hasło musi mieć co najmniej 4 znaki.")]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Powtórz nowe hasło.")]
        [Compare("NewPassword", ErrorMessage = "Nowe hasło i powtórzone hasło są różne.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public IActionResult OnGet()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToPage("/Account/Login");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToPage("/Account/Login");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ErrorMessage = "Nie znaleziono użytkownika.";
                return Page();
            }

            if (!PasswordHasher.VerifyPassword(CurrentPassword, user.Password))
            {
                ModelState.AddModelError(nameof(CurrentPassword), "Obecne hasło jest nieprawidłowe.");
                return Page();
            }

            user.Password = PasswordHasher.HashPassword(NewPassword);
            await _context.SaveChangesAsync();

            SuccessMessage = "Hasło zostało zmienione.";
            return Page();
        }
    }
}
