using Akwarium.Models;
using Akwarium.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Akwarium.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly AkwariumDbContext _context;

        public RegisterModel(AkwariumDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Nick jest wymagany.")]
        [StringLength(50, ErrorMessage = "Nick może mieć maksymalnie 50 znaków.")]
        public string Nick { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Email jest wymagany.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy adres e-mail.")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Hasło jest wymagane.")]
        [MinLength(4, ErrorMessage = "Hasło musi mieć co najmniej 4 znaki.")]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public string? ReturnUrl { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {


            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (await _context.Users.AnyAsync(u => u.Email == Email))
            {
                // błąd przypięty do pola Email
                ModelState.AddModelError(nameof(Email), "Użytkownik z takim adresem e-mail już istnieje.");
                return Page();
            }

            var hashed = PasswordHasher.HashPassword(Password);

            var newUser = new User
            {
                Nick = Nick,
                Email = Email,
                Password = hashed,
                Role = "User"
            }; 

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("UserName", newUser.Nick);
            HttpContext.Session.SetString("UserEmail", newUser.Email);
            HttpContext.Session.SetString("UserRole", newUser.Role);

            return RedirectToPage("/Index");
        }
    }
}
