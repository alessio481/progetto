using FleetManager.Models;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Se l'utente ha gia fatto login, lo mandiamo subito in dashboard.
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return RedirectToAction("Login", "Account");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Per la pagina di errore ci basta l'id della richiesta corrente.
            var model = new ErroreViewModel
            {
                IdRichiesta = HttpContext.TraceIdentifier
            };

            return View(model);
        }
    }
}
