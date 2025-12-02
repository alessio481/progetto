using FleetManager.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace FleetManager.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // Mostra la pagina Login
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // ✳️ QUI: Inserisci la query per 'utente dal database'
            // var user = _context.Users.FirstOrDefault(u => u.Username == model.Username && u.PasswordHash == Hash(model.Password));
            // if (user == null) { ModelState.AddModelError(...); return View(model); }

            // *** Temporaneo: autenticazione semplificata ***
            bool isValidUser = (model.Username == "admin" && model.Password == "admin123")
                               || (model.Username == "user" && model.Password == "user123");

            if (!isValidUser)
            {
                ModelState.AddModelError("", "Username o password non validi.");
                return View(model);
            }

            // Ruolo (in DB avrai probabilmente una proprietà Role)
            string role = model.Username == "admin" ? "Admin" : "User";

            // Crea claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.Username),
                new Claim(ClaimTypes.Role, role)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            // Reindirizza in base al ruolo
            if (role == "Admin")
                return RedirectToAction("Dashboard", "Admin");
            else
                return RedirectToAction("Dashboard", "User");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Il campo Username è obbligatorio")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Il campo Password è obbligatorio")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public class RegisterModel
    {
        [Required(ErrorMessage = "Il campo Username è obbligatorio")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Il campo Email è obbligatorio")]
        [EmailAddress(ErrorMessage = "Email non valida")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Il campo Password è obbligatorio")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Conferma la password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Le password non coincidono")]
        public string? ConfirmPassword { get; set; }
    }



}
