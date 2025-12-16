using FleetManager.Models;
using FleetManager.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.Controllers
{
    public class DebugController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DebugController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult ciao() // Pagina/Debug/ciao
        {
            return Content("ciao");
        }
        public IActionResult Index() //Prova debug. Prima di andare a Pagina/Debug vedere di avere dati sul db
        {
            var veicoli = _context.Veicoli.ToList();
            return View(veicoli);
        }

        [HttpPost]
        public IActionResult AggiungiVeicolo(Veicolo v)
        {
            if (!ModelState.IsValid)
                return View(v);

            _context.Veicoli.Add(v);
            _context.SaveChanges();

            ViewBag.Messaggio = "Veicolo inserito!";
            return View();
        }

        [HttpGet]
        public IActionResult AggiungiVeicolo()
        {
            return View();
        }

        /*
        public IActionResult InserisciTantiDati()
        {
            // =======================================================
            // 1) UTENTI (realistici)
            // =======================================================

            var utenti = new List<Utente>
    {
        new Utente { Nome="Alessio", Cognome="Picciati", Email="alessio.picciati@example.com", Password="1234", Ruolo="Admin" },
        new Utente { Nome="Marco", Cognome="Bianchi", Email="marco.bianchi@example.com", Password="pass1", Ruolo="Driver" },
        new Utente { Nome="Chiara", Cognome="Rossi", Email="chiara.rossi@example.com", Password="pass2", Ruolo="Driver" },
        new Utente { Nome="Sara", Cognome="Verdi", Email="sara.verdi@example.com", Password="pass3", Ruolo="Driver" },
        new Utente { Nome="Luca", Cognome="Neri", Email="luca.neri@example.com", Password="pass4", Ruolo="Driver" },
        new Utente { Nome="Giulia", Cognome="Ferrari", Email="giulia.ferrari@example.com", Password="gpass", Ruolo="Driver" },
        new Utente { Nome="Davide", Cognome="Conti", Email="davide.conti@example.com", Password="dpass", Ruolo="Driver" },
        new Utente { Nome="Martina", Cognome="Pellegrini", Email="martina.pellegrini@example.com", Password="mpass", Ruolo="Driver" }
    };

            _context.Utenti.AddRange(utenti);
            _context.SaveChanges();


            // =======================================================
            // 2) VEICOLI (realistici, marche e modelli veri)
            // =======================================================

            var veicoli = new List<Veicolo>
    {
        new Veicolo { Targa="AA123AA", Marca="Fiat", Modello="Panda", Tipo="Auto", Stato="Disponibile", LivelloCarburante=80, Colore="Bianco" },
        new Veicolo { Targa="BB456BB", Marca="Volkswagen", Modello="Golf", Tipo="Auto", Stato="InUso", LivelloCarburante=45, Colore="Nero" },
        new Veicolo { Targa="CC789CC", Marca="Ford", Modello="Transit", Tipo="Furgone", Stato="Manutenzione", LivelloCarburante=60, Colore="Bianco" },
        new Veicolo { Targa="DD321DD", Marca="Opel", Modello="Corsa", Tipo="Auto", Stato="Disponibile", LivelloCarburante=70, Colore="Grigio" },
        new Veicolo { Targa="EE654EE", Marca="Renault", Modello="Clio", Tipo="Auto", Stato="InUso", LivelloCarburante=30, Colore="Blu" },
        new Veicolo { Targa="FF987FF", Marca="Mercedes", Modello="Sprinter", Tipo="Furgone", Stato="Disponibile", LivelloCarburante=55, Colore="Bianco" },
        new Veicolo { Targa="GG111GG", Marca="Toyota", Modello="Yaris", Tipo="Auto", Stato="Disponibile", LivelloCarburante=90, Colore="Rosso" },
        new Veicolo { Targa="HH222HH", Marca="Iveco", Modello="Daily", Tipo="Furgone", Stato="Manutenzione", LivelloCarburante=40, Colore="Bianco" }
    };

            _context.Veicoli.AddRange(veicoli);
            _context.SaveChanges();


            // =======================================================
            // 3) PRENOTAZIONI (storico realistico)
            // =======================================================

            var prenotazioni = new List<Prenotazione>
    {
        new Prenotazione { UtenteId=utenti[1].UtenteID, VeicoloId=veicoli[0].VeicoloId, OraPrenotazione=DateTime.Now.AddDays(-10), OraRilascio=DateTime.Now.AddDays(-9) },
        new Prenotazione { UtenteId=utenti[2].UtenteID, VeicoloId=veicoli[3].VeicoloId, OraPrenotazione=DateTime.Now.AddDays(-6), OraRilascio=DateTime.Now.AddDays(-5) },
        new Prenotazione { UtenteId=utenti[3].UtenteID, VeicoloId=veicoli[4].VeicoloId, OraPrenotazione=DateTime.Now.AddDays(-2), OraRilascio=null },   // attuale
        new Prenotazione { UtenteId=utenti[5].UtenteID, VeicoloId=veicoli[6].VeicoloId, OraPrenotazione=DateTime.Now.AddDays(-3), OraRilascio=DateTime.Now.AddDays(-2) },
        new Prenotazione { UtenteId=utenti[6].UtenteID, VeicoloId=veicoli[1].VeicoloId, OraPrenotazione=DateTime.Now.AddDays(-1), OraRilascio=null }
    };

            _context.Prenotazioni.AddRange(prenotazioni);
            _context.SaveChanges();


            // =======================================================
            // 4) MANUTENZIONI (realistiche)
            // =======================================================

            var manutenzioni = new List<Manutenzione>
    {
        new Manutenzione { VeicoloId=veicoli[2].VeicoloId, UtenteId=utenti[0].UtenteID, Descrizione="Sostituzione pastiglie freni", DataInizio=DateTime.Now.AddDays(-4), DataFine=null },
        new Manutenzione { VeicoloId=veicoli[7].VeicoloId, UtenteId=utenti[1].UtenteID, Descrizione="Tagliando completo", DataInizio=DateTime.Now.AddDays(-20), DataFine=DateTime.Now.AddDays(-18) },
        new Manutenzione { VeicoloId=veicoli[5].VeicoloId, UtenteId=utenti[3].UtenteID, Descrizione="Cambio pneumatici", DataInizio=DateTime.Now.AddDays(-8), DataFine=DateTime.Now.AddDays(-7) }
    };

            _context.Manutenzioni.AddRange(manutenzioni);
            _context.SaveChanges();


            // =======================================================
            // 5) SEGNALAZIONI (realistiche)
            // =======================================================

            var segnalazioni = new List<Segnalazione>
    {
        new Segnalazione { UtenteID=utenti[1].UtenteID, VeicoloID=veicoli[0].VeicoloId, Tipo="Rumore sospetto", Descrizione="Ticchettio in accelerazione", Stato="Aperta" },
        new Segnalazione { UtenteID=utenti[3].UtenteID, VeicoloID=veicoli[3].VeicoloId, Tipo="Pneumatico sgonfio", Descrizione="Ruota anteriore sinistra quasi a terra", Stato="InLavorazione" },
        new Segnalazione { UtenteID=utenti[4].UtenteID, VeicoloID=veicoli[1].VeicoloId, Tipo="Danno carrozzeria", Descrizione="Graffio sul paraurti posteriore", Stato="Risolta" },
        new Segnalazione { UtenteID=utenti[6].UtenteID, VeicoloID=veicoli[6].VeicoloId, Tipo="Spia motore accesa", Descrizione="Possibile anomalia sensore lambda", Stato="Aperta" }
    };

            _context.Segnalazioni.AddRange(segnalazioni);
            _context.SaveChanges();


            // =======================================================
            // 6) DASHBOARD SNAPSHOTS (ultimi 10 giorni)
            // =======================================================

            for (int i = 1; i <= 10; i++)
            {
                _context.DashboardSnapshots.Add(new DashboardSnapshot
                {
                    Giorno = DateTime.Today.AddDays(-i),
                    VeicoliTotali = veicoli.Count,
                    VeicoliDisponibili = veicoli.Count(v => v.Stato == "Disponibile"),
                    VeicoliInUso = veicoli.Count(v => v.Stato == "InUso"),
                    VeicoliInManutenzione = veicoli.Count(v => v.Stato == "Manutenzione"),
                    SegnalazioniAperte = segnalazioni.Count(s => s.Stato == "Aperta"),
                    PrenotazioniAttive = prenotazioni.Count(p => p.OraRilascio == null)
                });
            }

            _context.SaveChanges();

            return Content("📌 Dati realistici inseriti con successo!");
        }
        */


        public IActionResult OttieniVeicoli()
        {
            VisualizzatoreDatiGenerali viewModel = new VisualizzatoreDatiGenerali
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

            return View(viewModel);
        }
    }
}
