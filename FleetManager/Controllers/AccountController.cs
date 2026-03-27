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

            return View(new LoginUtenteViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginUtenteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var emailInserita = model.Email.Trim();
            var passwordInserita = model.Password;

            // Cerchiamo l'utente per email.
            var utente = await _context.Utenti.FirstOrDefaultAsync(item => item.Email == emailInserita);
            if (utente == null)
            {
                ModelState.AddModelError(string.Empty, "Credenziali non valide.");
                return View(model);
            }

            if (utente.Password != passwordInserita)
            {
                ModelState.AddModelError(string.Empty, "Credenziali non valide.");
                return View(model);
            }

            var ruolo = "user";
            var ruoloPulito = string.Empty;
            if (!string.IsNullOrWhiteSpace(utente.Ruolo))
            {
                ruoloPulito = utente.Ruolo.Trim().ToLowerInvariant();
            }

            if (ruoloPulito == "admin")
            {
                ruolo = "admin";
            }

            var nomeDaMostrare = utente.Email;
            if (!string.IsNullOrWhiteSpace(utente.NomeCompleto))
            {
                nomeDaMostrare = utente.NomeCompleto;
            }

            // Questi dati finiscono nel cookie e servono per riconoscere l'utente.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, nomeDaMostrare),
                new Claim(ClaimTypes.NameIdentifier, utente.UtenteID.ToString()),
                new Claim("matricola", utente.UtenteID.ToString()),
                new Claim(ClaimTypes.Email, utente.Email),
                new Claim(ClaimTypes.Role, ruolo)
            };

            var identita = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identita);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Dopo il login si entra sempre nella dashboard principale.
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
