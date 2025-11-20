using Microsoft.AspNetCore.Mvc;
using FleetManager.Models;

namespace FleetManager.Controllers
{
    public class DebugController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DebugController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult InserisciDati()
        {
            var user = new Utente
            {
                Nome = "Luca",
                Cognome = "Verdi",
                Email = "luca@example.com",
                Password = "1234",
                Ruolo = "Driver"
            };

            _context.Utenti.Add(user);
            _context.SaveChanges();

            var veicolo = new Veicolo
            {
                Targa = "AB123CD",
                Marca = "Fiat",
                Modello = "Panda",
                Tipo = "Auto",
                Stato = "Disponibile",
                LivelloCarburante = 80
            };

            _context.Veicoli.Add(veicolo);
            _context.SaveChanges();

            return Content("Dati inseriti!");
        }
    }
}
