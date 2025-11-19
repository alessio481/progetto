using FleetManager.Data;
using FleetManager.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FleetManager.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        // unire due ctor
        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            // Crea e passa il modello alla vista
            var dashboardData = new DashboardData();
            return View(dashboardData);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult RoleSelection()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}