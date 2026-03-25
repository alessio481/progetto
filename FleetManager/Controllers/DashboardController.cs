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
        public async Task<IActionResult> Index([FromQuery] DashboardFiltersViewModel filters)
        {
            // La dashboard ora viene costruita tutta lato server.
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var isAdmin = User.IsInRole("admin");
            var currentUser = await _context.Utenti
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.UtenteID == userId.Value);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cars = await _context.Veicoli
                .AsNoTracking()
                .Include(car => car.UtentePrenotato)
                .OrderBy(car => car.Gruppo)
                .ThenBy(car => car.Marca)
                .ThenBy(car => car.Modello)
                .ToListAsync();

            var model = new DashboardIndexViewModel
            {
                IsAdmin = isAdmin,
                CurrentUserName = currentUser.NomeCompleto,
                StatusMessage = TempData["StatusMessage"]?.ToString(),
                ErrorMessage = TempData["ErrorMessage"]?.ToString(),
                ReturnUrl = BuildReturnUrl(),
                Filters = filters
            };

            model.Cars = cars
                .Where(car => isAdmin || car.UtentePrenotatoID == userId.Value)
                .Where(car => MatchesFilters(car, filters))
                .Select(car => ToCardViewModel(car, isAdmin, userId.Value))
                .ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create(string? returnUrl)
        {
            if (!User.IsInRole("admin"))
            {
                return Forbid();
            }

            var model = await BuildCarFormAsync(new DashboardCarFormViewModel
            {
                IsAdmin = true,
                IsCreate = true,
                ReturnUrl = NormalizeReturnUrl(returnUrl)
            });

            return View("Edit", model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? returnUrl)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var isAdmin = User.IsInRole("admin");
            var car = await _context.Veicoli
                .AsNoTracking()
                .Include(item => item.UtentePrenotato)
                .FirstOrDefaultAsync(item => item.VeicoloId == id);

            if (car == null)
            {
                TempData["ErrorMessage"] = "Veicolo non trovato.";
                return RedirectToAction("Index");
            }

            if (!isAdmin && car.UtentePrenotatoID != userId.Value)
            {
                return Forbid();
            }

            var model = await BuildCarFormAsync(new DashboardCarFormViewModel
            {
                Id = car.VeicoloId,
                IsAdmin = isAdmin,
                IsCreate = false,
                ReturnUrl = NormalizeReturnUrl(returnUrl),
                Modello = BuildModelLabel(car),
                Targa = car.Targa,
                OwnerId = car.UtentePrenotatoID,
                Gruppo = NormalizeGroup(car.Gruppo, car.Tipo),
                Chilometraggio = car.Chilometraggio,
                FuelLevel = Math.Clamp(car.LivelloCarburante, 0, 2),
                FuelType = car.Carburante,
                Stato = ToUiStatus(car.Stato),
                DataPossesso = car.DataPossesso,
                ImageUrl = car.ImageUrl,
                RevisioneInizio = car.RevisioneInizio,
                BolloInizio = car.BolloInizio,
                TagliandoInizio = car.TagliandoInizio,
                AssicurazioneInizio = car.AssicurazioneInizio
            });

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(DashboardCarFormViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var isAdmin = User.IsInRole("admin");
            model.IsAdmin = isAdmin;
            model.IsCreate = !model.Id.HasValue;
            model.ReturnUrl = NormalizeReturnUrl(model.ReturnUrl);

            if (!ModelState.IsValid)
            {
                model = await BuildCarFormAsync(model);
                return View("Edit", model);
            }

            if (model.IsCreate && !isAdmin)
            {
                return Forbid();
            }

            // Un solo metodo salva sia creazione sia modifica.
            Veicolo car;

            if (model.Id.HasValue)
            {
                car = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == model.Id.Value)
                    ?? throw new InvalidOperationException("Veicolo non trovato.");

                if (!isAdmin && car.UtentePrenotatoID != userId.Value)
                {
                    return Forbid();
                }
            }
            else
            {
                car = new Veicolo
                {
                    Tipo = "Auto",
                    DataCreazione = DateTime.UtcNow
                };
                _context.Veicoli.Add(car);
            }

            ApplyFormToCar(car, model, isAdmin);

            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = model.IsCreate
                ? "Veicolo creato correttamente."
                : "Veicolo salvato correttamente.";

            return Redirect(model.ReturnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UseNow(int id, string? returnUrl)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var car = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == id);
            if (car == null)
            {
                TempData["ErrorMessage"] = "Veicolo non trovato.";
                return RedirectToAction("Index");
            }

            if (car.UtentePrenotatoID != userId.Value)
            {
                return Forbid();
            }

            var currentStatus = ToUiStatus(car.Stato);
            car.Stato = currentStatus == "in uso" ? "Disponibile" : "InUso";
            car.DataAggiornamento = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Stato veicolo aggiornato.";
            return Redirect(NormalizeReturnUrl(returnUrl));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestMaintenance(int id, string? returnUrl)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (User.IsInRole("admin"))
            {
                return Forbid();
            }

            var car = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == id);
            if (car == null)
            {
                TempData["ErrorMessage"] = "Veicolo non trovato.";
                return RedirectToAction("Index");
            }

            if (car.UtentePrenotatoID != userId.Value)
            {
                return Forbid();
            }

            car.Stato = "RichiestaManu";
            car.DataAggiornamento = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Segnalazione manutenzione inviata.";
            return Redirect(NormalizeReturnUrl(returnUrl));
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveMaintenance(int id, string? returnUrl)
        {
            var car = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == id);
            if (car == null)
            {
                TempData["ErrorMessage"] = "Veicolo non trovato.";
                return RedirectToAction("Index");
            }

            car.Stato = "Manutenzione";
            car.DataAggiornamento = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Veicolo impostato in manutenzione.";
            return Redirect(NormalizeReturnUrl(returnUrl));
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl)
        {
            var car = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == id);
            if (car == null)
            {
                TempData["ErrorMessage"] = "Veicolo non trovato.";
                return RedirectToAction("Index");
            }

            _context.Veicoli.Remove(car);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Veicolo eliminato correttamente.";
            return Redirect(NormalizeReturnUrl(returnUrl));
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetDemo(string? returnUrl)
        {
            await DemoDataSeeder.ResetDemoAsync(_context);
            TempData["StatusMessage"] = "Dataset demo ripristinato.";
            return Redirect(NormalizeReturnUrl(returnUrl));
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearDemo(string? returnUrl)
        {
            _context.Prenotazioni.RemoveRange(_context.Prenotazioni);
            _context.Veicoli.RemoveRange(_context.Veicoli);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Parco auto svuotato correttamente.";
            return Redirect(NormalizeReturnUrl(returnUrl));
        }

        private async Task<DashboardCarFormViewModel> BuildCarFormAsync(DashboardCarFormViewModel model)
        {
            // Prepariamo le select del form in modo semplice e leggibile.
            model.OwnerOptions = await _context.Utenti
                .AsNoTracking()
                .Where(user => !string.Equals(user.Ruolo, "Admin", StringComparison.OrdinalIgnoreCase))
                .OrderBy(user => user.Cognome)
                .ThenBy(user => user.Nome)
                .Select(user => new SelectItemViewModel
                {
                    Value = user.UtenteID.ToString(),
                    Label = user.NomeCompleto + " | " + user.Email
                })
                .ToListAsync();

            model.GroupOptions = new List<SelectItemViewModel>
            {
                new() { Value = Gruppo1, Label = Gruppo1 },
                new() { Value = Gruppo2, Label = Gruppo2 },
                new() { Value = Gruppo3, Label = Gruppo3 }
            };

            model.FuelOptions = new List<SelectItemViewModel>
            {
                new() { Value = "2", Label = "Alto" },
                new() { Value = "1", Label = "Medio" },
                new() { Value = "0", Label = "Riserva" }
            };

            model.StatusOptions = new List<SelectItemViewModel>
            {
                new() { Value = "non in uso", Label = "Non in uso" },
                new() { Value = "in uso", Label = "In uso" },
                new() { Value = "in richiesta manutenzione", Label = "In richiesta manutenzione" },
                new() { Value = "in manutenzione", Label = "In manutenzione" }
            };

            return model;
        }

        private DashboardCarCardViewModel ToCardViewModel(Veicolo car, bool isAdmin, int currentUserId)
        {
            // Questo e il passaggio dal modello database ai dati che la view stampa.
            var uiStatus = ToUiStatus(car.Stato);
            var isOwner = car.UtentePrenotatoID == currentUserId;

            return new DashboardCarCardViewModel
            {
                Id = car.VeicoloId,
                Modello = BuildModelLabel(car),
                Targa = car.Targa,
                Stato = uiStatus,
                Gruppo = NormalizeGroup(car.Gruppo, car.Tipo),
                OwnerId = car.UtentePrenotatoID,
                OwnerName = car.UtentePrenotato?.NomeCompleto,
                Chilometraggio = car.Chilometraggio,
                FuelLevel = Math.Clamp(car.LivelloCarburante, 0, 2),
                FuelType = car.Carburante,
                ImageUrl = string.IsNullOrWhiteSpace(car.ImageUrl) ? "https://via.placeholder.com/400x250" : car.ImageUrl!,
                DataPossesso = car.DataPossesso,
                RevisioneInizio = car.RevisioneInizio,
                RevisioneScadenza = CalculateExpiry(car.RevisioneInizio, 2),
                BolloInizio = car.BolloInizio,
                BolloScadenza = CalculateExpiry(car.BolloInizio, 1),
                TagliandoInizio = car.TagliandoInizio,
                TagliandoScadenza = CalculateExpiry(car.TagliandoInizio, 1),
                AssicurazioneInizio = car.AssicurazioneInizio,
                AssicurazioneScadenza = CalculateExpiry(car.AssicurazioneInizio, 1),
                CanEdit = isAdmin || isOwner,
                CanUseNow = !isAdmin && isOwner && uiStatus != "in manutenzione" && uiStatus != "in richiesta manutenzione",
                CanRequestMaintenance = !isAdmin && isOwner && uiStatus != "in manutenzione" && uiStatus != "in richiesta manutenzione",
                CanApproveMaintenance = isAdmin && uiStatus == "in richiesta manutenzione"
            };
        }

        private static bool MatchesFilters(Veicolo car, DashboardFiltersViewModel filters)
        {
            var modelLabel = BuildModelLabel(car).ToLowerInvariant();
            var ownerName = car.UtentePrenotato?.NomeCompleto?.ToLowerInvariant() ?? string.Empty;
            var uiStatus = ToUiStatus(car.Stato);
            var fuelLevel = Math.Clamp(car.LivelloCarburante, 0, 2);

            if (!string.IsNullOrWhiteSpace(filters.Search))
            {
                var query = filters.Search.Trim().ToLowerInvariant();
                var searchText = string.Join(' ',
                    modelLabel,
                    car.Targa.ToLowerInvariant(),
                    ownerName,
                    NormalizeGroup(car.Gruppo, car.Tipo).ToLowerInvariant(),
                    uiStatus,
                    (car.Carburante ?? string.Empty).ToLowerInvariant(),
                    FuelLabel(fuelLevel).ToLowerInvariant());

                if (!searchText.Contains(query))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(filters.Group) && NormalizeGroup(car.Gruppo, car.Tipo) != filters.Group)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(filters.Status) && uiStatus != filters.Status)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(filters.Owner) && !ownerName.Contains(filters.Owner.Trim().ToLowerInvariant()))
            {
                return false;
            }

            if (filters.FuelLevel.HasValue && fuelLevel != filters.FuelLevel.Value)
            {
                return false;
            }

            return true;
        }

        private void ApplyFormToCar(Veicolo car, DashboardCarFormViewModel model, bool isAdmin)
        {
            // Qui teniamo in un solo punto la copia dei campi dal form al database.
            var (marca, modello) = SplitModel(model.Modello);

            car.Marca = marca;
            car.Modello = modello;
            car.Targa = model.Targa.Trim().ToUpperInvariant();
            car.Chilometraggio = Math.Max(model.Chilometraggio, 0);
            car.LivelloCarburante = Math.Clamp(model.FuelLevel, 0, 2);
            car.Carburante = model.FuelType?.Trim();
            car.DataPossesso = model.DataPossesso;
            car.ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim();
            car.RevisioneInizio = model.RevisioneInizio;
            car.BolloInizio = model.BolloInizio;
            car.TagliandoInizio = model.TagliandoInizio;
            car.AssicurazioneInizio = model.AssicurazioneInizio;
            car.RevisioneScadenza = CalculateExpiry(model.RevisioneInizio, 2);
            car.BolloScadenza = CalculateExpiry(model.BolloInizio, 1);
            car.TagliandoScadenza = CalculateExpiry(model.TagliandoInizio, 1);
            car.AssicurazioneScadenza = CalculateExpiry(model.AssicurazioneInizio, 1);
            car.DataAggiornamento = DateTime.UtcNow;

            if (isAdmin)
            {
                car.UtentePrenotatoID = model.OwnerId;
                car.Gruppo = NormalizeGroup(model.Gruppo, car.Tipo);
                car.Stato = ToSqlStatus(model.Stato);
            }
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue("matricola") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }

        private string BuildReturnUrl()
        {
            var path = Request.Path.HasValue ? Request.Path.Value! : Url.Action("Index", "Dashboard")!;
            var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            return path + query;
        }

        private string NormalizeReturnUrl(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return returnUrl;
            }

            return Url.Action("Index", "Dashboard")!;
        }

        private static DateTime? CalculateExpiry(DateTime? startDate, int years)
        {
            return startDate?.AddYears(years);
        }

        private static string BuildModelLabel(Veicolo car)
        {
            return string.IsNullOrWhiteSpace(car.Marca)
                ? car.Modello
                : car.Marca + " " + car.Modello;
        }

        private static string NormalizeGroup(string? group, string? tipo)
        {
            var cleaned = (group ?? string.Empty).Trim().ToUpperInvariant();
            if (cleaned == Gruppo1 || cleaned == Gruppo2 || cleaned == Gruppo3)
            {
                return cleaned;
            }

            return string.Equals(tipo, "Furgone", StringComparison.OrdinalIgnoreCase) ? Gruppo2 : Gruppo1;
        }

        private static string ToUiStatus(string? sqlStatus)
        {
            return (sqlStatus ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "disponibile" => "non in uso",
                "inuso" => "in uso",
                "manutenzione" => "in manutenzione",
                "richiestamanu" => "in richiesta manutenzione",
                _ => "non in uso"
            };
        }

        private static string ToSqlStatus(string? uiStatus)
        {
            return (uiStatus ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "in uso" => "InUso",
                "in manutenzione" => "Manutenzione",
                "in richiesta manutenzione" => "RichiestaManu",
                _ => "Disponibile"
            };
        }

        private static string FuelLabel(int fuelLevel)
        {
            return fuelLevel switch
            {
                2 => "Alto",
                1 => "Medio",
                _ => "Riserva"
            };
        }

        private static (string Marca, string Modello) SplitModel(string modelText)
        {
            var cleaned = (modelText ?? string.Empty).Trim();
            var parts = cleaned.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                return ("Veicolo", "Senza modello");
            }

            if (parts.Length == 1)
            {
                return (parts[0], parts[0]);
            }

            return (parts[0], parts[1]);
        }
    }
}
