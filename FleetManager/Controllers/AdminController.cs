using FleetManager.Models;
using FleetManager.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            var viewModel = new Visualizzatore
            {
                Veicoli = _context.Veicoli
                    .Include(v => v.UtentePrenotato)
                    .Include(v => v.UtenteManutentore)
                    .ToList(),

                Utenti = _context.Utenti.ToList(),

                Prenotazioni = _context.Prenotazioni
                    .Include(p => p.Veicolo)
                    .Include(p => p.Utente)
                    .OrderByDescending(p => p.OraPrenotazione)
                    .ToList(),

                Manutenzioni = _context.Manutenzioni
                    .Include(m => m.Veicolo)
                    .Include(m => m.Utente)
                    .OrderByDescending(m => m.DataInizio)
                    .ToList(),

                Segnalazioni = _context.Segnalazioni
                    .Include(s => s.Veicolo)
                    .Include(s => s.Utente)
                    .OrderByDescending(s => s.DataCreazione)
                    .ToList(),

                DashboardSnapshots = _context.DashboardSnapshots
                    .OrderByDescending(d => d.Giorno)
                    .ToList()
            };

            return View(viewModel); // Views/Admin/Dashboard.cshtml
        }
    }
}
