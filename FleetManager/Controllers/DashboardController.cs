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
        private const string Gruppo1 = "FONDAZIONE SETTORE-1";
        private const string Gruppo2 = "FONDAZIONE SETTORE-2";
        private const string Gruppo3 = "FONDAZIONE SETTORE-3";

        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] FiltriDashboardViewModel filtri)
        {
            // La dashboard viene costruita tutta lato server.
            var idUtenteCorrente = OttieniIdUtenteCorrente();
            if (idUtenteCorrente == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var eAdmin = User.IsInRole("admin");
            var utenteCorrente = await _context.Utenti
                .AsNoTracking()
                .FirstOrDefaultAsync(utente => utente.UtenteID == idUtenteCorrente.Value);

            if (utenteCorrente == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var veicoliDb = await _context.Veicoli
                .AsNoTracking()
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
                UrlRitorno = CostruisciUrlRitorno(),
                Filtri = filtri
            };

            model.Veicoli = veicoliDb
                .Where(veicolo => eAdmin || veicolo.UtentePrenotatoID == idUtenteCorrente.Value)
                .Where(veicolo => RispettaFiltri(veicolo, filtri))
                .Select(veicolo => CreaSchedaVeicolo(veicolo, eAdmin, idUtenteCorrente.Value))
                .ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Crea(string? urlRitorno)
        {
            if (!User.IsInRole("admin"))
            {
                return Forbid();
            }

            var model = await PreparaFormVeicoloAsync(new FormVeicoloViewModel
            {
                EAdmin = true,
                ECreazione = true,
                UrlRitorno = NormalizzaUrlRitorno(urlRitorno)
            });

            return View("Edit", model);
        }

        [HttpGet]
        public async Task<IActionResult> Modifica(int id, string? urlRitorno)
        {
            var idUtenteCorrente = OttieniIdUtenteCorrente();
            if (idUtenteCorrente == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var eAdmin = User.IsInRole("admin");
            var veicolo = await _context.Veicoli
                .AsNoTracking()
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
                Id = veicolo.VeicoloId,
                EAdmin = eAdmin,
                ECreazione = false,
                UrlRitorno = NormalizzaUrlRitorno(urlRitorno),
                Modello = CostruisciNomeModello(veicolo),
                Targa = veicolo.Targa,
                IdAssegnatario = veicolo.UtentePrenotatoID,
                Gruppo = NormalizzaGruppo(veicolo.Gruppo, veicolo.Tipo),
                Chilometraggio = veicolo.Chilometraggio,
                LivelloCarburante = Math.Clamp(veicolo.LivelloCarburante, 0, 2),
                TipoCarburante = veicolo.Carburante,
                Stato = StatoPerVista(veicolo.Stato),
                DataPossesso = veicolo.DataPossesso,
                UrlImmagine = veicolo.ImageUrl,
                RevisioneInizio = veicolo.RevisioneInizio,
                BolloInizio = veicolo.BolloInizio,
                TagliandoInizio = veicolo.TagliandoInizio,
                AssicurazioneInizio = veicolo.AssicurazioneInizio
            });

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salva(FormVeicoloViewModel model)
        {
            var idUtenteCorrente = OttieniIdUtenteCorrente();
            if (idUtenteCorrente == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var eAdmin = User.IsInRole("admin");
            model.EAdmin = eAdmin;
            model.ECreazione = !model.Id.HasValue;
            model.UrlRitorno = NormalizzaUrlRitorno(model.UrlRitorno);

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

            if (model.Id.HasValue)
            {
                veicolo = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == model.Id.Value)
                    ?? throw new InvalidOperationException("Veicolo non trovato.");

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
                    DataCreazione = DateTime.UtcNow
                };
                _context.Veicoli.Add(veicolo);
            }

            CopiaDatiFormSuVeicolo(veicolo, model, eAdmin);

            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = model.ECreazione
                ? "Veicolo creato correttamente."
                : "Veicolo salvato correttamente.";

            return Redirect(model.UrlRitorno);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UsaOra(int id, string? urlRitorno)
        {
            var idUtenteCorrente = OttieniIdUtenteCorrente();
            if (idUtenteCorrente == null)
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
            veicolo.Stato = statoAttuale == "in uso" ? "Disponibile" : "InUso";
            veicolo.DataAggiornamento = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Stato veicolo aggiornato.";
            return Redirect(NormalizzaUrlRitorno(urlRitorno));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SegnalaManutenzione(int id, string? urlRitorno)
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

            veicolo.Stato = "RichiestaManu";
            veicolo.DataAggiornamento = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Segnalazione manutenzione inviata.";
            return Redirect(NormalizzaUrlRitorno(urlRitorno));
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovaManutenzione(int id, string? urlRitorno)
        {
            var veicolo = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == id);
            if (veicolo == null)
            {
                TempData["ErrorMessage"] = "Veicolo non trovato.";
                return RedirectToAction(nameof(Index));
            }

            veicolo.Stato = "Manutenzione";
            veicolo.DataAggiornamento = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Veicolo impostato in manutenzione.";
            return Redirect(NormalizzaUrlRitorno(urlRitorno));
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Elimina(int id, string? urlRitorno)
        {
            var veicolo = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == id);
            if (veicolo == null)
            {
                TempData["ErrorMessage"] = "Veicolo non trovato.";
                return RedirectToAction(nameof(Index));
            }

            _context.Veicoli.Remove(veicolo);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Veicolo eliminato correttamente.";
            return Redirect(NormalizzaUrlRitorno(urlRitorno));
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RipristinaDemo(string? urlRitorno)
        {
            await DemoDataSeeder.ResetDemoAsync(_context);
            TempData["StatusMessage"] = "Dataset demo ripristinato.";
            return Redirect(NormalizzaUrlRitorno(urlRitorno));
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SvuotaDatiDemo(string? urlRitorno)
        {
            _context.Prenotazioni.RemoveRange(_context.Prenotazioni);
            _context.Veicoli.RemoveRange(_context.Veicoli);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Parco auto svuotato correttamente.";
            return Redirect(NormalizzaUrlRitorno(urlRitorno));
        }

        private async Task<FormVeicoloViewModel> PreparaFormVeicoloAsync(FormVeicoloViewModel model)
        {
            // Qui prepariamo tutte le select della pagina.
            model.OpzioniAssegnatario = await _context.Utenti
                .AsNoTracking()
                .Where(utente => !string.Equals(utente.Ruolo, "Admin", StringComparison.OrdinalIgnoreCase))
                .OrderBy(utente => utente.Cognome)
                .ThenBy(utente => utente.Nome)
                .Select(utente => new OpzioneSelectViewModel
                {
                    Valore = utente.UtenteID.ToString(),
                    Testo = utente.NomeCompleto + " | " + utente.Email
                })
                .ToListAsync();

            model.OpzioniGruppo = new List<OpzioneSelectViewModel>
            {
                new() { Valore = Gruppo1, Testo = Gruppo1 },
                new() { Valore = Gruppo2, Testo = Gruppo2 },
                new() { Valore = Gruppo3, Testo = Gruppo3 }
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

            return new SchedaVeicoloViewModel
            {
                Id = veicolo.VeicoloId,
                Modello = CostruisciNomeModello(veicolo),
                Targa = veicolo.Targa,
                Stato = stato,
                Gruppo = NormalizzaGruppo(veicolo.Gruppo, veicolo.Tipo),
                IdAssegnatario = veicolo.UtentePrenotatoID,
                NomeAssegnatario = veicolo.UtentePrenotato?.NomeCompleto,
                Chilometraggio = veicolo.Chilometraggio,
                LivelloCarburante = Math.Clamp(veicolo.LivelloCarburante, 0, 2),
                TipoCarburante = veicolo.Carburante,
                UrlImmagine = string.IsNullOrWhiteSpace(veicolo.ImageUrl) ? "https://via.placeholder.com/400x250" : veicolo.ImageUrl!,
                DataPossesso = veicolo.DataPossesso,
                RevisioneInizio = veicolo.RevisioneInizio,
                RevisioneScadenza = CalcolaScadenza(veicolo.RevisioneInizio, 2),
                BolloInizio = veicolo.BolloInizio,
                BolloScadenza = CalcolaScadenza(veicolo.BolloInizio, 1),
                TagliandoInizio = veicolo.TagliandoInizio,
                TagliandoScadenza = CalcolaScadenza(veicolo.TagliandoInizio, 1),
                AssicurazioneInizio = veicolo.AssicurazioneInizio,
                AssicurazioneScadenza = CalcolaScadenza(veicolo.AssicurazioneInizio, 1),
                PuoModificare = eAdmin || eAssegnatario,
                PuoUsareOra = !eAdmin && eAssegnatario && stato != "in manutenzione" && stato != "in richiesta manutenzione",
                PuoSegnalareManutenzione = !eAdmin && eAssegnatario && stato != "in manutenzione" && stato != "in richiesta manutenzione",
                PuoApprovareManutenzione = eAdmin && stato == "in richiesta manutenzione"
            };
        }

        private static bool RispettaFiltri(Veicolo veicolo, FiltriDashboardViewModel filtri)
        {
            var nomeModello = CostruisciNomeModello(veicolo).ToLowerInvariant();
            var nomeAssegnatario = veicolo.UtentePrenotato?.NomeCompleto?.ToLowerInvariant() ?? string.Empty;
            var stato = StatoPerVista(veicolo.Stato);
            var livelloCarburante = Math.Clamp(veicolo.LivelloCarburante, 0, 2);

            if (!string.IsNullOrWhiteSpace(filtri.Ricerca))
            {
                var testoRicerca = filtri.Ricerca.Trim().ToLowerInvariant();
                var testoCompleto = string.Join(' ',
                    nomeModello,
                    veicolo.Targa.ToLowerInvariant(),
                    nomeAssegnatario,
                    NormalizzaGruppo(veicolo.Gruppo, veicolo.Tipo).ToLowerInvariant(),
                    stato,
                    (veicolo.Carburante ?? string.Empty).ToLowerInvariant(),
                    EtichettaCarburante(livelloCarburante).ToLowerInvariant());

                if (!testoCompleto.Contains(testoRicerca))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(filtri.Gruppo) && NormalizzaGruppo(veicolo.Gruppo, veicolo.Tipo) != filtri.Gruppo)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(filtri.Stato) && stato != filtri.Stato)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(filtri.Assegnatario) &&
                !nomeAssegnatario.Contains(filtri.Assegnatario.Trim().ToLowerInvariant()))
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
            var (marca, modello) = SeparaMarcaEModello(model.Modello);

            veicolo.Marca = marca;
            veicolo.Modello = modello;
            veicolo.Targa = model.Targa.Trim().ToUpperInvariant();
            veicolo.Chilometraggio = Math.Max(model.Chilometraggio, 0);
            veicolo.LivelloCarburante = Math.Clamp(model.LivelloCarburante, 0, 2);
            veicolo.Carburante = model.TipoCarburante?.Trim();
            veicolo.DataPossesso = model.DataPossesso;
            veicolo.ImageUrl = string.IsNullOrWhiteSpace(model.UrlImmagine) ? null : model.UrlImmagine.Trim();
            veicolo.RevisioneInizio = model.RevisioneInizio;
            veicolo.BolloInizio = model.BolloInizio;
            veicolo.TagliandoInizio = model.TagliandoInizio;
            veicolo.AssicurazioneInizio = model.AssicurazioneInizio;
            veicolo.RevisioneScadenza = CalcolaScadenza(model.RevisioneInizio, 2);
            veicolo.BolloScadenza = CalcolaScadenza(model.BolloInizio, 1);
            veicolo.TagliandoScadenza = CalcolaScadenza(model.TagliandoInizio, 1);
            veicolo.AssicurazioneScadenza = CalcolaScadenza(model.AssicurazioneInizio, 1);
            veicolo.DataAggiornamento = DateTime.UtcNow;

            if (eAdmin)
            {
                veicolo.UtentePrenotatoID = model.IdAssegnatario;
                veicolo.Gruppo = NormalizzaGruppo(model.Gruppo, veicolo.Tipo);
                veicolo.Stato = StatoPerDatabase(model.Stato);
            }
        }

        private int? OttieniIdUtenteCorrente()
        {
            var valore = User.FindFirstValue("matricola") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(valore, out var idUtente) ? idUtente : null;
        }

        private string CostruisciUrlRitorno()
        {
            var percorso = Request.Path.HasValue ? Request.Path.Value! : Url.Action(nameof(Index), "Dashboard")!;
            var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            return percorso + query;
        }

        private string NormalizzaUrlRitorno(string? urlRitorno)
        {
            if (!string.IsNullOrWhiteSpace(urlRitorno) && Url.IsLocalUrl(urlRitorno))
            {
                return urlRitorno;
            }

            return Url.Action(nameof(Index), "Dashboard")!;
        }

        private static DateTime? CalcolaScadenza(DateTime? dataInizio, int anni)
        {
            return dataInizio?.AddYears(anni);
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

        private static string NormalizzaGruppo(string? gruppo, string? tipo)
        {
            var valorePulito = (gruppo ?? string.Empty).Trim().ToUpperInvariant();
            if (valorePulito == Gruppo1 || valorePulito == Gruppo2 || valorePulito == Gruppo3)
            {
                return valorePulito;
            }

            return string.Equals(tipo, "Furgone", StringComparison.OrdinalIgnoreCase) ? Gruppo2 : Gruppo1;
        }

        private static string StatoPerVista(string? statoDatabase)
        {
            return (statoDatabase ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "disponibile" => "non in uso",
                "inuso" => "in uso",
                "manutenzione" => "in manutenzione",
                "richiestamanu" => "in richiesta manutenzione",
                _ => "non in uso"
            };
        }

        private static string StatoPerDatabase(string? statoVista)
        {
            return (statoVista ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "in uso" => "InUso",
                "in manutenzione" => "Manutenzione",
                "in richiesta manutenzione" => "RichiestaManu",
                _ => "Disponibile"
            };
        }

        private static string EtichettaCarburante(int livelloCarburante)
        {
            return livelloCarburante switch
            {
                2 => "Alto",
                1 => "Medio",
                _ => "Riserva"
            };
        }

        private static (string Marca, string Modello) SeparaMarcaEModello(string testoModello)
        {
            var valorePulito = (testoModello ?? string.Empty).Trim();
            var parti = valorePulito.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            if (parti.Length == 0)
            {
                return ("Veicolo", "Senza modello");
            }

            if (parti.Length == 1)
            {
                return (parti[0], parti[0]);
            }

            return (parti[0], parti[1]);
        }
    }
}
