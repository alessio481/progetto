using FleetManager.Models;
using FleetManager.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FleetManager.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private const int IndiceGruppo1 = 1;
        private const int IndiceGruppo2 = 2;
        private const int IndiceGruppo3 = 3;
        private const string Gruppo1 = "FONDAZIONE SETTORE-1";
        private const string Gruppo2 = "FONDAZIONE SETTORE-2";
        private const string Gruppo3 = "FONDAZIONE SETTORE-3";
        private const string UrlImmagineBase = "https://via.placeholder.com/400x250";

        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(FiltriDashboardViewModel filtri)
        {
            // 1 leggiamo l'utente loggato
            // 2 prendiamo i veicoli dal database
            // 3 prepariamo le schede da mostrare nella view
            var idUtenteCorrente = OttieniIdUtenteCorrente();
            if (idUtenteCorrente == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var eAdmin = User.IsInRole("admin");
            var utenteCorrente = await _context.Utenti
                .FirstOrDefaultAsync(utente => utente.UtenteID == idUtenteCorrente.Value);

            if (utenteCorrente == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var veicoliDb = await _context.Veicoli
                .Include(veicolo => veicolo.UtentePrenotato)
                .OrderBy(veicolo => veicolo.Gruppo)
                .ThenBy(veicolo => veicolo.Marca)
                .ThenBy(veicolo => veicolo.Modello)
                .ToListAsync();


            var model = new DashboardPaginaViewModel
            {
                EAdmin = eAdmin,
                NomeUtenteCorrente = utenteCorrente.NomeCompleto,
                MessaggioOperazione = TempData["StatusMessage"]?.ToString(),
                MessaggioErrore = TempData["ErrorMessage"]?.ToString(),
                TempoGuidaSecondi = utenteCorrente.TempoGuidaSecondi,
                InizioGuidaUnix = utenteCorrente.InizioGuidaUnix,
                FiltriRicerca = filtri
            };

            var elencoVeicoli = new List<SchedaVeicoloViewModel>();

            foreach (var veicolo in veicoliDb)
            {
                var puoVederlo = eAdmin || veicolo.UtentePrenotatoID == idUtenteCorrente.Value;
                if (!puoVederlo)
                {
                    continue;
                }

                if (!RispettaFiltri(veicolo, filtri))
                {
                    continue;
                }

                elencoVeicoli.Add(CreaSchedaVeicolo(veicolo, eAdmin, idUtenteCorrente.Value));
            }

            model.SchedeVeicoli = elencoVeicoli;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Crea()
        {
            if (!User.IsInRole("admin"))
            {
                return Forbid();
            }

            var model = await PreparaFormVeicoloAsync(new FormVeicoloViewModel
            {
                EAdmin = true,
                ECreazione = true
            });

            return View("Edit", model);
        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public IActionResult CreaUtente()
        {
            return View("EditUtente", new FormUtenteViewModel());
        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> TempiGuidaUtenti()
        {
            var utentiDb = await _context.Utenti
                .OrderBy(utente => utente.Cognome)
                .ThenBy(utente => utente.Nome)
                .ToListAsync();

            var model = new TempiGuidaUtentiViewModel
            {
                NomeAdmin = User.Identity?.Name ?? "Admin"
            };

            foreach (var utente in utentiDb)
            {
                if (!string.IsNullOrWhiteSpace(utente.Ruolo) &&
                    utente.Ruolo.Trim().ToLowerInvariant() == "admin")
                {
                    continue;
                }

                model.Utenti.Add(new RigaTempoGuidaUtenteViewModel
                {
                    Nome = utente.Nome,
                    Cognome = utente.Cognome,
                    Email = utente.Email,
                    TempoGuidaSecondi = utente.TempoGuidaSecondi,
                    InizioGuidaUnix = utente.InizioGuidaUnix
                });
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Modifica(int id)
        {
            var idUtenteCorrente = OttieniIdUtenteCorrente();
            if (idUtenteCorrente == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var eAdmin = User.IsInRole("admin");
            var veicolo = await _context.Veicoli
                .Include(item => item.UtentePrenotato)
                .FirstOrDefaultAsync(item => item.VeicoloId == id);

            if (veicolo == null)
            {
                TempData["ErrorMessage"] = "Veicolo non trovato.";
                return RedirectToAction(nameof(Index));
            }

            if (!eAdmin && veicolo.UtentePrenotatoID != idUtenteCorrente.Value)
            {
                return Forbid();
            }

            var model = await PreparaFormVeicoloAsync(new FormVeicoloViewModel
            {
                IdVeicolo = veicolo.VeicoloId,
                EAdmin = eAdmin,
                ECreazione = false,
                Modello = CostruisciNomeModello(veicolo),
                Targa = veicolo.Targa,
                IdAssegnatario = veicolo.UtentePrenotatoID,
                Gruppo = SistemaGruppo(veicolo.Gruppo, veicolo.Tipo),
                Chilometraggio = veicolo.Chilometraggio,
                LivelloCarburante = SistemaLivelloCarburante(veicolo.LivelloCarburante),
                TipoCarburante = veicolo.Carburante,
                Stato = StatoPerVista(veicolo.Stato),
                DataPossesso = veicolo.DataPossesso,
                LinkImmagine = veicolo.ImageUrl,
                RevisioneInizio = veicolo.RevisioneInizio,
                BolloInizio = veicolo.BolloInizio,
                TagliandoInizio = veicolo.TagliandoInizio,
                AssicurazioneInizio = veicolo.AssicurazioneInizio
            });

            return View("Edit", model);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> SalvaUtente(FormUtenteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("EditUtente", model);
            }

            var nomePulito = model.Nome.Trim();
            var cognomePulito = model.Cognome.Trim();
            var emailPulita = model.Email.Trim().ToLowerInvariant();
            var passwordPulita = model.Password.Trim();

            if (nomePulito.Length < 2)
            {
                ModelState.AddModelError(string.Empty, "Il nome deve avere almeno 2 caratteri.");
                return View("EditUtente", model);
            }

            if (cognomePulito.Length < 2)
            {
                ModelState.AddModelError(string.Empty, "Il cognome deve avere almeno 2 caratteri.");
                return View("EditUtente", model);
            }

            if (passwordPulita.Length < 5)
            {
                ModelState.AddModelError(string.Empty, "La password deve avere almeno 5 caratteri.");
                return View("EditUtente", model);
            }

            var utenteEsistente = await _context.Utenti.FirstOrDefaultAsync(item => item.Email == emailPulita);
            if (utenteEsistente != null)
            {
                ModelState.AddModelError(string.Empty, "Esiste gia un utente con questa email.");
                return View("EditUtente", model);
            }

            if (model.DataNascita == null)
            {
                ModelState.AddModelError(string.Empty, "Inserisci una data di nascita.");
                return View("EditUtente", model);
            }

            var dataNascita = model.DataNascita.Value.Date;
            if (dataNascita > DateTime.Today)
            {
                ModelState.AddModelError(string.Empty, "La data di nascita non puo essere nel futuro.");
                return View("EditUtente", model);
            }

            if (dataNascita < DateTime.Today.AddYears(-100))
            {
                ModelState.AddModelError(string.Empty, "La data di nascita e troppo lontana.");
                return View("EditUtente", model);
            }

            var utente = new Utente
            {
                Nome = nomePulito,
                Cognome = cognomePulito,
                Email = emailPulita,
                Password = passwordPulita,
                DataNascita = dataNascita,
                Ruolo = "Driver",
                DataRegistrazione = DateTime.Now,
                TempoGuidaSecondi = 0,
                InizioGuidaUnix = null
            };

            try
            {
                _context.Utenti.Add(utente);
                await _context.SaveChangesAsync();
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Errore durante il salvataggio dell'utente.");
                return View("EditUtente", model);
            }

            TempData["StatusMessage"] = "Utente creato correttamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Salva(FormVeicoloViewModel model)
        {
            var idUtenteCorrente = OttieniIdUtenteCorrente();
            if (idUtenteCorrente == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var eAdmin = User.IsInRole("admin");
            model.EAdmin = eAdmin;
            model.ECreazione = !model.IdVeicolo.HasValue;

            if (!ModelState.IsValid)
            {
                model = await PreparaFormVeicoloAsync(model);
                return View("Edit", model);
            }

            if (model.ECreazione && !eAdmin)
            {
                return Forbid();
            }

            // Un solo metodo gestisce sia creazione sia modifica.
            Veicolo veicolo;
            var statoPrimaDelSalvataggio = "non in uso";
            int? idAssegnatarioPrimaDelSalvataggio = null;

            if (model.IdVeicolo.HasValue)
            {
                var veicoloTrovato = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == model.IdVeicolo.Value);
                if (veicoloTrovato == null)
                {
                    TempData["ErrorMessage"] = "Veicolo non trovato.";
                    return RedirectToAction(nameof(Index));
                }

                veicolo = veicoloTrovato;
                statoPrimaDelSalvataggio = StatoPerVista(veicolo.Stato);
                idAssegnatarioPrimaDelSalvataggio = veicolo.UtentePrenotatoID;

                if (!eAdmin && veicolo.UtentePrenotatoID != idUtenteCorrente.Value)
                {
                    return Forbid();
                }
            }
            else
            {
                veicolo = new Veicolo
                {
                    Tipo = "Auto",
                    DataCreazione = DateTime.Now
                };
                _context.Veicoli.Add(veicolo);
            }

            CopiaDatiFormSuVeicolo(veicolo, model, eAdmin);
            await AllineaTempoGuidaDopoCambioStatoAsync(
                statoPrimaDelSalvataggio,
                idAssegnatarioPrimaDelSalvataggio,
                StatoPerVista(veicolo.Stato),
                veicolo.UtentePrenotatoID);

            await _context.SaveChangesAsync();

            if (model.ECreazione)
            {
                TempData["StatusMessage"] = "Veicolo creato correttamente.";
            }
            else
            {
                TempData["StatusMessage"] = "Veicolo salvato correttamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UsaOra(int id)
        {
            var idUtenteCorrente = OttieniIdUtenteCorrente();
            if (idUtenteCorrente == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var utente = await _context.Utenti.FirstOrDefaultAsync(item => item.UtenteID == idUtenteCorrente.Value);
            if (utente == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var veicolo = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == id);
            if (veicolo == null)
            {
                TempData["ErrorMessage"] = "Veicolo non trovato.";
                return RedirectToAction(nameof(Index));
            }

            if (veicolo.UtentePrenotatoID != idUtenteCorrente.Value)
            {
                return Forbid();
            }

            var statoAttuale = StatoPerVista(veicolo.Stato);

            if (statoAttuale == "in uso")
            {
                veicolo.Stato = "Disponibile";
                FermaGuidaUtente(utente);
                TempData["StatusMessage"] = "Stato veicolo aggiornato e tempo di guida salvato.";
            }
            else
            {
                veicolo.Stato = "InUso";
                AvviaGuidaUtente(utente);
                TempData["StatusMessage"] = "Stato veicolo aggiornato. Guida iniziata.";
            }

            veicolo.DataAggiornamento = DateTime.Now;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> SegnalaManutenzione(int id)
        {
            var idUtenteCorrente = OttieniIdUtenteCorrente();
            if (idUtenteCorrente == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (User.IsInRole("admin"))
            {
                return Forbid();
            }

            var veicolo = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == id);
            if (veicolo == null)
            {
                TempData["ErrorMessage"] = "Veicolo non trovato.";
                return RedirectToAction(nameof(Index));
            }

            if (veicolo.UtentePrenotatoID != idUtenteCorrente.Value)
            {
                return Forbid();
            }

            await FermaGuidaSeVeicoloEraInUsoAsync(veicolo);
            veicolo.Stato = "RichiestaManu";
            veicolo.DataAggiornamento = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Segnalazione manutenzione inviata.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> ApprovaManutenzione(int id)
        {
            var veicolo = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == id);
            if (veicolo == null)
            {
                TempData["ErrorMessage"] = "Veicolo non trovato.";
                return RedirectToAction(nameof(Index));
            }

            veicolo.Stato = "Manutenzione";
            veicolo.DataAggiornamento = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Veicolo impostato in manutenzione.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> TerminaManutenzione(int id)
        {
            var veicolo = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == id);
            if (veicolo == null)
            {
                TempData["ErrorMessage"] = "Veicolo non trovato.";
                return RedirectToAction(nameof(Index));
            }

            veicolo.Stato = "Disponibile";
            veicolo.DataAggiornamento = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Manutenzione terminata correttamente.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Elimina(int id)
        {
            var veicolo = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == id);
            if (veicolo == null)
            {
                TempData["ErrorMessage"] = "Veicolo non trovato.";
                return RedirectToAction(nameof(Index));
            }

            await FermaGuidaSeVeicoloEraInUsoAsync(veicolo);
            _context.Veicoli.Remove(veicolo);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Veicolo eliminato correttamente.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> RipristinaDemo()
        {
            await DemoDataSeeder.RipristinaDatiDemoAsync(_context);
            TempData["StatusMessage"] = "Dataset demo ripristinato.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> SvuotaDatiDemo()
        {
            _context.Veicoli.RemoveRange(_context.Veicoli);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Parco auto svuotato correttamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<FormVeicoloViewModel> PreparaFormVeicoloAsync(FormVeicoloViewModel model)
        {
            // Qui prepariamo tutte le select della pagina.
            var utentiDb = await _context.Utenti
                .OrderBy(utente => utente.Cognome)
                .ThenBy(utente => utente.Nome)
                .ToListAsync();

            var opzioniAssegnatario = new List<OpzioneSelectViewModel>();

            foreach (var utente in utentiDb)
            {
                var ruolo = string.Empty;
                if (!string.IsNullOrWhiteSpace(utente.Ruolo))
                {
                    ruolo = utente.Ruolo.Trim().ToLowerInvariant();
                }

                if (ruolo == "admin")
                {
                    continue;
                }

                opzioniAssegnatario.Add(new OpzioneSelectViewModel
                {
                    Valore = utente.UtenteID.ToString(),
                    Testo = utente.NomeCompleto + " | " + utente.Email
                });
            }

            model.OpzioniAssegnatario = opzioniAssegnatario;

            model.OpzioniGruppo = new List<OpzioneSelectViewModel>
            {
                new() { Valore = IndiceGruppo1.ToString(), Testo = Gruppo1 },
                new() { Valore = IndiceGruppo2.ToString(), Testo = Gruppo2 },
                new() { Valore = IndiceGruppo3.ToString(), Testo = Gruppo3 }
            };

            model.OpzioniCarburante = new List<OpzioneSelectViewModel>
            {
                new() { Valore = "2", Testo = "Alto" },
                new() { Valore = "1", Testo = "Medio" },
                new() { Valore = "0", Testo = "Riserva" }
            };

            model.OpzioniStato = new List<OpzioneSelectViewModel>
            {
                new() { Valore = "non in uso", Testo = "Non in uso" },
                new() { Valore = "in uso", Testo = "In uso" },
                new() { Valore = "in richiesta manutenzione", Testo = "In richiesta manutenzione" },
                new() { Valore = "in manutenzione", Testo = "In manutenzione" }
            };

            return model;
        }

        private SchedaVeicoloViewModel CreaSchedaVeicolo(Veicolo veicolo, bool eAdmin, int idUtenteCorrente)
        {
            // Dal modello DB creiamo un oggetto molto semplice da stampare nella view.
            var stato = StatoPerVista(veicolo.Stato);
            var eAssegnatario = veicolo.UtentePrenotatoID == idUtenteCorrente;
            var puoUsareOra = false;
            var puoSegnalareManutenzione = false;
            var puoApprovareManutenzione = false;
            var puoTerminareManutenzione = false;

            if (!eAdmin && eAssegnatario && stato != "in manutenzione" && stato != "in richiesta manutenzione")
            {
                puoUsareOra = true;
                puoSegnalareManutenzione = true;
            }

            if (eAdmin && stato == "in richiesta manutenzione")
            {
                puoApprovareManutenzione = true;
            }

            if (eAdmin && stato == "in manutenzione")
            {
                puoTerminareManutenzione = true;
            }

            var mostraTimerGuida = false;
            long? inizioGuidaUnix = null;

            // Se un'auto e' in uso, mostriamo il timer a chi la sta guardando.
            if (stato == "in uso" && veicolo.UtentePrenotato?.InizioGuidaUnix.HasValue == true)
            {
                mostraTimerGuida = true;
                inizioGuidaUnix = veicolo.UtentePrenotato.InizioGuidaUnix;
            }

            return new SchedaVeicoloViewModel
            {
                IdVeicolo = veicolo.VeicoloId,
                Modello = CostruisciNomeModello(veicolo),
                Targa = veicolo.Targa,
                Stato = stato,
                Gruppo = NomeGruppo(SistemaGruppo(veicolo.Gruppo, veicolo.Tipo)),
                NomeAssegnatario = veicolo.UtentePrenotato?.NomeCompleto,
                Chilometraggio = veicolo.Chilometraggio,
                LivelloCarburante = SistemaLivelloCarburante(veicolo.LivelloCarburante),
                TipoCarburante = veicolo.Carburante,
                LinkImmagine = ScegliUrlImmagine(veicolo.ImageUrl),
                DataPossesso = veicolo.DataPossesso,
                RevisioneInizio = veicolo.RevisioneInizio,
                RevisioneScadenza = CalcolaScadenza(veicolo.RevisioneInizio, 2),
                BolloInizio = veicolo.BolloInizio,
                BolloScadenza = CalcolaScadenza(veicolo.BolloInizio, 1),
                TagliandoInizio = veicolo.TagliandoInizio,
                TagliandoScadenza = CalcolaScadenza(veicolo.TagliandoInizio, 1),
                AssicurazioneInizio = veicolo.AssicurazioneInizio,
                AssicurazioneScadenza = CalcolaScadenza(veicolo.AssicurazioneInizio, 1),
                MostraTimerGuida = mostraTimerGuida,
                InizioGuidaUnix = inizioGuidaUnix,
                PuoModificare = eAdmin || eAssegnatario,
                PuoUsareOra = puoUsareOra,
                PuoSegnalareManutenzione = puoSegnalareManutenzione,
                PuoApprovareManutenzione = puoApprovareManutenzione,
                PuoTerminareManutenzione = puoTerminareManutenzione
            };
        }

        private static bool RispettaFiltri(Veicolo veicolo, FiltriDashboardViewModel filtri)
        {
            var nomeModello = PortaInMinuscolo(CostruisciNomeModello(veicolo));
            var nomeAssegnatario = PortaInMinuscolo(veicolo.UtentePrenotato?.NomeCompleto);
            var stato = StatoPerVista(veicolo.Stato);
            var livelloCarburante = SistemaLivelloCarburante(veicolo.LivelloCarburante);
            var gruppo = SistemaGruppo(veicolo.Gruppo, veicolo.Tipo);

            if (!string.IsNullOrWhiteSpace(filtri.Ricerca))
            {
                var testoRicerca = PortaInMinuscolo(filtri.Ricerca);
                var pezziRicerca = new List<string>
                {
                    nomeModello,
                    PortaInMinuscolo(veicolo.Targa),
                    nomeAssegnatario,
                    PortaInMinuscolo(NomeGruppo(gruppo)),
                    PortaInMinuscolo(stato),
                    PortaInMinuscolo(veicolo.Carburante),
                    PortaInMinuscolo(EtichettaCarburante(livelloCarburante))
                };
                var testoCompleto = string.Join(' ', pezziRicerca);

                if (!testoCompleto.Contains(testoRicerca))
                {
                    return false;
                }
            }

            if (filtri.Gruppo.HasValue && gruppo != filtri.Gruppo.Value)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(filtri.Stato) && stato != filtri.Stato)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(filtri.Assegnatario) &&
                !nomeAssegnatario.Contains(PortaInMinuscolo(filtri.Assegnatario)))
            {
                return false;
            }

            if (filtri.LivelloCarburante.HasValue && livelloCarburante != filtri.LivelloCarburante.Value)
            {
                return false;
            }

            return true;
        }

        private void CopiaDatiFormSuVeicolo(Veicolo veicolo, FormVeicoloViewModel model, bool eAdmin)
        {
            // Tutti i campi modificabili passano da qui.
            if (eAdmin)
            {
                string marca;
                string modello;
                SeparaMarcaEModello(model.Modello, out marca, out modello);

                veicolo.Marca = marca;
                veicolo.Modello = modello;
                veicolo.Targa = model.Targa.Trim().ToUpperInvariant();
            }

            veicolo.Chilometraggio = Math.Max(model.Chilometraggio, 0);
            veicolo.LivelloCarburante = SistemaLivelloCarburante(model.LivelloCarburante);
            veicolo.RevisioneInizio = model.RevisioneInizio;
            veicolo.BolloInizio = model.BolloInizio;
            veicolo.TagliandoInizio = model.TagliandoInizio;
            veicolo.AssicurazioneInizio = model.AssicurazioneInizio;
            veicolo.DataAggiornamento = DateTime.Now;

            if (eAdmin)
            {
                veicolo.Carburante = model.TipoCarburante?.Trim();
                veicolo.DataPossesso = model.DataPossesso;
                veicolo.ImageUrl = null;
                if (!string.IsNullOrWhiteSpace(model.LinkImmagine))
                {
                    veicolo.ImageUrl = model.LinkImmagine.Trim();
                }

                veicolo.UtentePrenotatoID = model.IdAssegnatario;
                veicolo.Gruppo = SistemaGruppo(model.Gruppo, veicolo.Tipo);
                veicolo.Stato = StatoPerDatabase(model.Stato);
            }
        }

        private async Task AllineaTempoGuidaDopoCambioStatoAsync(
            string statoPrima,
            int? idUtentePrima,
            string statoDopo,
            int? idUtenteDopo)
        {
            var stessaGuida = statoPrima == "in uso" &&
                              statoDopo == "in uso" &&
                              idUtentePrima.HasValue &&
                              idUtentePrima == idUtenteDopo;

            if (stessaGuida)
            {
                return;
            }

            if (statoPrima == "in uso" && idUtentePrima.HasValue)
            {
                var utentePrima = await _context.Utenti.FirstOrDefaultAsync(item => item.UtenteID == idUtentePrima.Value);
                if (utentePrima != null)
                {
                    FermaGuidaUtente(utentePrima);
                }
            }

            if (statoDopo == "in uso" && idUtenteDopo.HasValue)
            {
                var utenteDopo = await _context.Utenti.FirstOrDefaultAsync(item => item.UtenteID == idUtenteDopo.Value);
                if (utenteDopo != null)
                {
                    AvviaGuidaUtente(utenteDopo);
                }
            }
        }

        private async Task FermaGuidaSeVeicoloEraInUsoAsync(Veicolo veicolo)
        {
            var stato = StatoPerVista(veicolo.Stato);
            if (stato != "in uso" || !veicolo.UtentePrenotatoID.HasValue)
            {
                return;
            }

            var utente = await _context.Utenti.FirstOrDefaultAsync(item => item.UtenteID == veicolo.UtentePrenotatoID.Value);
            if (utente != null)
            {
                FermaGuidaUtente(utente);
            }
        }

        private static void FermaGuidaUtente(Utente utente)
        {
            if (utente.InizioGuidaUnix.HasValue)
            {
                var unixAdesso = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var secondiGuidati = unixAdesso - utente.InizioGuidaUnix.Value;

                if (secondiGuidati > 0)
                {
                    utente.TempoGuidaSecondi += secondiGuidati;
                }
            }

            utente.InizioGuidaUnix = null;
        }

        private static void AvviaGuidaUtente(Utente utente)
        {
            if (!utente.InizioGuidaUnix.HasValue)
            {
                utente.InizioGuidaUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }

        private int? OttieniIdUtenteCorrente()
        {
            var valore = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(valore))
            {
                return null;
            }

            if (int.TryParse(valore, out var idUtente))
            {
                return idUtente;
            }

            return null;
        }

        private static DateTime? CalcolaScadenza(DateTime? dataInizio, int anni)
        {
            if (!dataInizio.HasValue)
            {
                return null;
            }

            return dataInizio.Value.AddYears(anni);
        }

        private static int SistemaLivelloCarburante(int livelloCarburante)
        {
            if (livelloCarburante < 0)
            {
                return 0;
            }

            if (livelloCarburante > 2)
            {
                return 2;
            }

            return livelloCarburante;
        }

        private static string CostruisciNomeModello(Veicolo veicolo)
        {
            if (string.IsNullOrWhiteSpace(veicolo.Marca))
            {
                return veicolo.Modello;
            }

            if (string.IsNullOrWhiteSpace(veicolo.Modello) ||
                string.Equals(veicolo.Marca, veicolo.Modello, StringComparison.OrdinalIgnoreCase))
            {
                return veicolo.Marca;
            }

            return veicolo.Marca + " " + veicolo.Modello;
        }

        private static int SistemaGruppo(int gruppo, string? tipo)
        {
            if (gruppo == IndiceGruppo1 || gruppo == IndiceGruppo2 || gruppo == IndiceGruppo3)
            {
                return gruppo;
            }

            if (string.Equals(tipo, "Furgone", StringComparison.OrdinalIgnoreCase))
            {
                return IndiceGruppo2;
            }

            return IndiceGruppo1;
        }

        private static string NomeGruppo(int gruppo)
        {
            var nomeGruppo = Gruppo1;

            switch (gruppo)
            {
                case IndiceGruppo2:
                    nomeGruppo = Gruppo2;
                    break;

                case IndiceGruppo3:
                    nomeGruppo = Gruppo3;
                    break;
            }

            return nomeGruppo;
        }

        private static string StatoPerVista(string? statoDatabase)
        {
            var stato = string.Empty;
            if (!string.IsNullOrWhiteSpace(statoDatabase))
            {
                stato = statoDatabase.Trim().ToLowerInvariant();
            }

            // Traduce lo stato del database in uno stato piu leggibile per la pagina.
            var statoPerLaPagina = "non in uso";

            switch (stato)
            {
                case "disponibile":
                    statoPerLaPagina = "non in uso";
                    break;

                case "inuso":
                    statoPerLaPagina = "in uso";
                    break;

                case "manutenzione":
                    statoPerLaPagina = "in manutenzione";
                    break;

                case "richiestamanu":
                    statoPerLaPagina = "in richiesta manutenzione";
                    break;
            }

            return statoPerLaPagina;
        }

        private static string StatoPerDatabase(string? statoVista)
        {
            var stato = string.Empty;
            if (!string.IsNullOrWhiteSpace(statoVista))
            {
                stato = statoVista.Trim().ToLowerInvariant();
            }

            // Fa il passaggio opposto: dallo stato scritto nella pagina a quello salvato nel DB.
            var statoPerIlDatabase = "Disponibile";

            switch (stato)
            {
                case "in uso":
                    statoPerIlDatabase = "InUso";
                    break;

                case "in manutenzione":
                    statoPerIlDatabase = "Manutenzione";
                    break;

                case "in richiesta manutenzione":
                    statoPerIlDatabase = "RichiestaManu";
                    break;
            }

            return statoPerIlDatabase;
        }

        private static string EtichettaCarburante(int livelloCarburante)
        {
            var etichetta = "Riserva";

            switch (livelloCarburante)
            {
                case 2:
                    etichetta = "Alto";
                    break;

                case 1:
                    etichetta = "Medio";
                    break;
            }

            return etichetta;
        }

        private static void SeparaMarcaEModello(string testoModello, out string marca, out string modello)
        {
            var valorePulito = (testoModello ?? string.Empty).Trim();
            var parti = valorePulito.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            if (parti.Length == 0)
            {
                marca = "Veicolo";
                modello = "Senza modello";
                return;
            }

            if (parti.Length == 1)
            {
                marca = parti[0];
                modello = parti[0];
                return;
            }

            marca = parti[0];
            modello = parti[1];
        }

        private static string ScegliUrlImmagine(string? urlImmagine)
        {
            if (string.IsNullOrWhiteSpace(urlImmagine))
            {
                return UrlImmagineBase;
            }

            return urlImmagine;
        }

        private static string PortaInMinuscolo(string? valore)
        {
            if (string.IsNullOrWhiteSpace(valore))
            {
                return string.Empty;
            }

            return valore.Trim().ToLowerInvariant();
        }
    }
}
