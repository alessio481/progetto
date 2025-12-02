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

        public IActionResult ciao() // Pagina/Debug/ciao
        {
            return Content("ciao");
        }
        public IActionResult Index() //Prova debug. Prima di andare a Pagina/Debug vedere di avere dati sul db
        {
            var veicoli = _context.Veicoli.ToList();
            return View(veicoli);
        }

        public IActionResult InserisciTantiDati()
        {
            // ================================
            // 1) UTENTI            DA ELIMINARE dopo aver fatto l'update del server sql  
            // ================================

            var utenti = new List<Utente>
            {
                new Utente { Nome="Alessio", Cognome="Picciati", Email="ale@example.com", Password="1234", Ruolo="Admin" },
                new Utente { Nome="Marco", Cognome="Bianchi", Email="marco@example.com", Password="pass1", Ruolo="Driver" },
                new Utente { Nome="Chiara", Cognome="Rossi", Email="chiara@example.com", Password="pass2", Ruolo="Driver" },
                new Utente { Nome="Sara", Cognome="Verdi", Email="sara@example.com", Password="pass3", Ruolo="Driver" },
                new Utente { Nome="Luca", Cognome="Neri", Email="luca@example.com", Password="pass4", Ruolo="Driver" }
            };

            _context.Utenti.AddRange(utenti);
            _context.SaveChanges();


            // ================================
            // 2) VEICOLI
            // ================================

            var veicoli = new List<Veicolo>
            {
                new Veicolo { Targa="AA111AA", Marca="Fiat", Modello="Panda", Tipo="Auto", Stato="Disponibile", LivelloCarburante=90, Colore="Bianco" },
                new Veicolo { Targa="BB222BB", Marca="Volkswagen", Modello="Golf", Tipo="Auto", Stato="InUso", LivelloCarburante=40, Colore="Nero" },
                new Veicolo { Targa="CC333CC", Marca="Ford", Modello="Transit", Tipo="Furgone", Stato="Manutenzione", LivelloCarburante=60, Colore="Bianco" },
                new Veicolo { Targa="DD444DD", Marca="Opel", Modello="Corsa", Tipo="Auto", Stato="Disponibile", LivelloCarburante=70, Colore="Grigio" },
                new Veicolo { Targa="EE555EE", Marca="Renault", Modello="Clio", Tipo="Auto", Stato="InUso", LivelloCarburante=30, Colore="Blu" }
            };

            _context.Veicoli.AddRange(veicoli);
            _context.SaveChanges();


            // ================================
            // 3) PRENOTAZIONI (storico)
            // ================================

            var prenotazioni = new List<Prenotazione>
            {
                new Prenotazione { UtenteId=utenti[1].UtenteID, VeicoloId=veicoli[0].VeicoloId, OraPrenotazione=DateTime.Now.AddDays(-3), OraRilascio=DateTime.Now.AddDays(-2) },
                new Prenotazione { UtenteId=utenti[2].UtenteID, VeicoloId=veicoli[3].VeicoloId, OraPrenotazione=DateTime.Now.AddDays(-5), OraRilascio=DateTime.Now.AddDays(-4) },
                new Prenotazione { UtenteId=utenti[3].UtenteID, VeicoloId=veicoli[4].VeicoloId, OraPrenotazione=DateTime.Now.AddDays(-1), OraRilascio=null } // attuale
            };

            _context.Prenotazioni.AddRange(prenotazioni);
            _context.SaveChanges();


            // ================================
            // 4) MANUTENZIONI
            // ================================

            var manutenzioni = new List<Manutenzione>
            {
                new Manutenzione { VeicoloId=veicoli[2].VeicoloId, UtenteId=utenti[0].UtenteID, Descrizione="Sostituzione freni", DataInizio=DateTime.Now.AddDays(-2), DataFine=null },
                new Manutenzione { VeicoloId=veicoli[1].VeicoloId, UtenteId=utenti[1].UtenteID, Descrizione="Cambio olio", DataInizio=DateTime.Now.AddDays(-10), DataFine=DateTime.Now.AddDays(-9) }
            };

            _context.Manutenzioni.AddRange(manutenzioni);
            _context.SaveChanges();


            // ================================
            // 5) SEGNALAZIONI
            // ================================

            var segnalazioni = new List<Segnalazione>
            {
                new Segnalazione { UtenteID=utenti[1].UtenteID, VeicoloID=veicoli[0].VeicoloId, Tipo="Rumori sospetti", Descrizione="Strano rumore al motore", Stato="Aperta" },
                new Segnalazione { UtenteID=utenti[3].UtenteID, VeicoloID=veicoli[3].VeicoloId, Tipo="Pneumatico a terra", Descrizione="Foratura improvvisa", Stato="InLavorazione" },
                new Segnalazione { UtenteID=utenti[4].UtenteID, VeicoloID=veicoli[1].VeicoloId, Tipo="Carrozzeria danneggiata", Descrizione="Graffio lato destro", Stato="Risolta" }
            };

            _context.Segnalazioni.AddRange(segnalazioni);
            _context.SaveChanges();


            // ================================
            // 6) DASHBOARD SNAPSHOTS (storico 10 giorni)
            // ================================

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

            return Content("📌 Dati COMPLETI inseriti con successo!");
        }

        public IActionResult OttieniVeicoli()
        {
            var veicoli = _context.Veicoli.ToList();
            return View(veicoli);

        }
    }
}
