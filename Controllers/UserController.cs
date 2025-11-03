using Akwarium.Models;
using Microsoft.AspNetCore.Mvc;


namespace Akwarium.Controllers
{

    [Route("/api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AkwariumDbContext _context;

        public UserController(AkwariumDbContext context)
        {
            _context = context;
        }
        /*
             [HttpGet]
             public IActionResult Login()
             {
                 return View();
             }

             [HttpPost]
             public IActionResult Login(string username, string password)
             {
                 var user = _context.Users.FirstOrDefault(u => u.Nick == username && u.Password == password);

                 if (user == null)
                 {
                     ViewBag.Error = "Nieprawidłowy login lub hasło.";
                     return View();
                 }

                 HttpContext.Session.SetString("Username", user.Nick);
                 return RedirectToAction("Index", "Home");
             }

             public IActionResult Logout()
             {
                 HttpContext.Session.Clear();
                 return RedirectToAction("Login");
             }
        */
    }
}
