using FleetManager.Data;
using FleetManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FleetManager.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var veicoli = await _context.Vehicles.ToListAsync();
            var assignments = await _context.Assignments
                .Include(a => a.Vehicle)
                .Include(a => a.Driver)
                .OrderByDescending(a => a.DataInizio)
                .Take(5)
                .ToListAsync();

            var dashboard = new DashboardData
            {
                VeicoliTotali = veicoli.Count,
                VeicoliDisponibili = veicoli.Count(v => v.Stato == VehicleStatus.Disponibile),
                VeicoliAssegnati = veicoli.Count(v => v.Stato == VehicleStatus.Assegnato),
                VeicoliInManutenzione = veicoli.Count(v => v.Stato == VehicleStatus.Manutenzione),
                AssegnazioniAperte = assignments.Count(a => a.InCorso),
                UltimeAssegnazioni = assignments
            };

            return View(dashboard);
        }

        public IActionResult Privacy()
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
