using FleetManager.Models;
using FleetManager.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FleetManager.Controllers
{
    [Authorize(Roles = "Driver")]
    public class DriverController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DriverController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            // ID del driver dal cookie
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(idStr) || !int.TryParse(idStr, out int driverId))
                return RedirectToAction("Login", "Account");

            // ✅ Costruiamo un Visualizzatore "filtrato" sul driver loggato
            var vm = new Visualizzatore
            {
                // Se vuoi far vedere anche i veicoli, puoi mostrare:
                // - tutti i veicoli
                // - oppure solo quelli collegati alle sue prenotazioni
                Veicoli = _context.Veicoli
                    .Include(v => v.UtentePrenotato)
                    .Include(v => v.UtenteManutentore)
                    .ToList(),

                // SOLO il driver loggato
                Utenti = _context.Utenti
                    .Where(u => u.UtenteID == driverId)
                    .ToList(),

                // Prenotazioni del driver
                Prenotazioni = _context.Prenotazioni
                    .Include(p => p.Veicolo)
                    .Include(p => p.Utente)
                    .Where(p => p.UtenteId == driverId)
                    .OrderByDescending(p => p.OraPrenotazione)
                    .ToList(),

                // Manutenzioni assegnate al driver (se per te UtenteId è il manutentore)
                Manutenzioni = _context.Manutenzioni
                    .Include(m => m.Veicolo)
                    .Include(m => m.Utente)
                    .Where(m => m.UtenteId == driverId)
                    .OrderByDescending(m => m.DataInizio)
                    .ToList(),

                // Segnalazioni del driver
                Segnalazioni = _context.Segnalazioni
                    .Include(s => s.Veicolo)
                    .Include(s => s.Utente)
                    .Where(s => s.UtenteID == driverId)
                    .OrderByDescending(s => s.DataCreazione)
                    .ToList(),

                // Snapshot: puoi lasciarlo vuoto o mostrare gli ultimi (non è “personale”)
                DashboardSnapshots = _context.DashboardSnapshots
                    .OrderByDescending(d => d.Giorno)
                    .Take(10)
                    .ToList()
            };

            return View(vm); // Views/Driver/Dashboard.cshtml
        }
    }
}
