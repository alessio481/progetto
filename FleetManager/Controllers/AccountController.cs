using FleetManager.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System;
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

        // ====================
        //  GET: /Account/Login
        // ====================
        public IActionResult Login()
        {
            return View();
        }

        // ====================
        //  POST: /Account/Login perchè si va a scrivere
        // ====================
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Cerca l'utente per email
            var utente = _context.Utenti.FirstOrDefault(u => u.Email == model.Email);

            if (utente == null)
            {
                ModelState.AddModelError("", "Email non trovata.");
                return View(model);
            }

            // Controllo password (semplice per progetto)
            if (utente.Password != model.Password)
            {
                ModelState.AddModelError("", "Password errata.");
                return View(model);
            }

            // ===========================================
            // AUTENTICAZIONE CON COOKIE SEMPLICE unica parte difficile
            // ===========================================
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, utente.Email),
                new Claim("UtenteID", utente.UtenteID.ToString()),
                new Claim("Ruolo", utente.Ruolo)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity)
            );

            return RedirectToAction("Index", "Home");
        }

        // ====================
        //  GET: /Account/Register
        // ====================
        public IActionResult Register()
        {
            return View();
        }

        // ====================
        //  POST: /Account/Register
        // ====================
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Controllo email già registrata
            if (_context.Utenti.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("", "Email già utilizzata.");
                return View(model);
            }

            // CREA UN NUOVO UTENTE
            var nuovo = new Utente
            {
                Nome = model.Nome,
                Cognome = model.Cognome,
                Email = model.Email,
                Password = model.Password, // semplice
                DataNascita = model.DataNascita,
                Ruolo = "Driver", // default
                DataRegistrazione = DateTime.Now
            };

            _context.Utenti.Add(nuovo);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        // ====================
        //  /Account/Logout
        // ====================
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
