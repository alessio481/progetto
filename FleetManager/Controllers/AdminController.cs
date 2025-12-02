using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        // Pannello di controllo amministratore
        public IActionResult Dashboard()
        {
            // Qui puoi aggiungere la logica per ottenere i dati del pannello di controllo
            var dashboardModel = new AdminDashboardViewModel
            {
                TotalVehicles = 156,
                AvailableVehicles = 120,
                MaintenanceVehicles = 15,
                PendingAssignmentVehicles = 21,
                MonthlyCost = 58900,
                FuelCost = 38200,
                MaintenanceCost = 12500,
                InsuranceCost = 8200
            };

            return View(dashboardModel);
        }

        // Altre funzionalità amministrative...
        public IActionResult Vehicles()
        {
            return View();
        }

        public IActionResult Users()
        {
            return View();
        }

        public IActionResult Assignments()
        {
            return View();
        }

        public IActionResult Maintenance()
        {
            return View();
        }

        public IActionResult Reports()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }
    }

    // Modello di vista del pannello di controllo amministratore
    public class AdminDashboardViewModel
    {
        public int TotalVehicles { get; set; }
        public int AvailableVehicles { get; set; }
        public int MaintenanceVehicles { get; set; }
        public int PendingAssignmentVehicles { get; set; }
        public decimal MonthlyCost { get; set; }
        public decimal FuelCost { get; set; }
        public decimal MaintenanceCost { get; set; }
        public decimal InsuranceCost { get; set; }
    }
}