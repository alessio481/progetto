using FleetManager.Models;
using FleetManager.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FleetManager.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View(new FleetLoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(FleetLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();
            var utente = await _context.Utenti.FirstOrDefaultAsync(u => u.Email == email);
            if (utente == null || utente.Password != model.Password)
            {
                ModelState.AddModelError(string.Empty, "Credenziali non valide.");
                return View(model);
            }

            var role = string.Equals(utente.Ruolo, "Admin", StringComparison.OrdinalIgnoreCase) ? "admin" : "user";
            var displayName = string.IsNullOrWhiteSpace(utente.NomeCompleto) ? utente.Email : utente.NomeCompleto;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, displayName),
                new Claim(ClaimTypes.NameIdentifier, utente.UtenteID.ToString()),
                new Claim("matricola", utente.UtenteID.ToString()),
                new Claim(ClaimTypes.Email, utente.Email),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}
