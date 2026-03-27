using System.Diagnostics;
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

        public IActionResult Privacy()
        {
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var idRichiesta = HttpContext.TraceIdentifier;
            if (Activity.Current != null && !string.IsNullOrWhiteSpace(Activity.Current.Id))
            {
                idRichiesta = Activity.Current.Id;
            }

            var model = new ErroreViewModel
            {
                IdRichiesta = idRichiesta
            };

            return View(model);
        }
    }
}
