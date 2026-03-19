using FleetManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FleetManager.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api")]
    public class FleetApiController : ControllerBase
    {
        private const string Gruppo1 = "FONDAZIONE SETTORE-1";
        private const string Gruppo2 = "FONDAZIONE SETTORE-2";
        private const string Gruppo3 = "FONDAZIONE SETTORE-3";

        private readonly ApplicationDbContext _context;

        public FleetApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("session/me")]
        public async Task<IActionResult> Me()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _context.Utenti
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.UtenteID == userId.Value);

            if (user == null)
            {
                return Unauthorized();
            }

            return Ok(new
            {
                isAdmin = User.IsInRole("admin"),
                matricola = user.UtenteID.ToString(),
                displayName = user.NomeCompleto,
                email = user.Email,
                nome = user.Nome,
                cognome = user.Cognome
            });
        }

        [Authorize(Roles = "admin")]
        [HttpGet("fleet/users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Utenti
                .AsNoTracking()
                .OrderBy(user => user.Cognome)
                .ThenBy(user => user.Nome)
                .Select(user => new FleetUserReference
                {
                    Id = user.UtenteID,
                    DisplayName = user.NomeCompleto,
                    Email = user.Email
                })
                .ToListAsync();

            return Ok(new { value = users });
        }

        [HttpGet("fleet/cars")]
        public async Task<IActionResult> GetCars()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("admin");
            var activeBookings = await LoadActiveBookingsAsync();
            var users = await LoadUsersByIdAsync();
            var cars = await _context.Veicoli
                .AsNoTracking()
                .OrderBy(car => car.VeicoloId)
                .ToListAsync();

            var result = cars
                .Select(car => MapCar(car, activeBookings, users))
                .Where(car => isAdmin || car.PossessoreMatricola == userId.Value)
                .ToList();

            return Ok(new { value = result });
        }

        [HttpPatch("fleet/cars/{id:int}")]
        public async Task<IActionResult> UpdateCar(int id, [FromBody] FleetCarUpsertRequest request)
        {
            var car = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == id);
            if (car == null)
            {
                return NotFound();
            }

            var isAdmin = User.IsInRole("admin");
            var activeBookings = await LoadActiveBookingsAsync();

            if (!isAdmin)
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized();
                }

                if (GetOwnerId(car, activeBookings) != userId.Value)
                {
                    return Forbid();
                }
            }

            // Tutti possono aggiornare i campi base della propria auto.
            FillCarFields(car, request);

            // Solo l'admin puo cambiare assegnatario e gruppo.
            if (isAdmin)
            {
                var owner = await ResolveOwnerIdAsync(request);
                if (owner == null)
                {
                    return BadRequest("Assegnatario non valido.");
                }

                car.UtentePrenotatoID = owner.Value;
                car.Gruppo = NormalizeGroup(request.Gruppo, car.Tipo);
            }

            await _context.SaveChangesAsync();

            var users = await LoadUsersByIdAsync();
            return Ok(MapCar(car, activeBookings, users));
        }

        [HttpPatch("fleet/cars/{id:int}/maintenance-request")]
        public async Task<IActionResult> RequestMaintenance(int id)
        {
            var car = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == id);
            if (car == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var activeBookings = await LoadActiveBookingsAsync();
            var isAdmin = User.IsInRole("admin");

            if (!isAdmin && GetOwnerId(car, activeBookings) != userId.Value)
            {
                return Forbid();
            }

            car.Stato = ToSqlStatus("in richiesta manutenzione");
            car.DataAggiornamento = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var users = await LoadUsersByIdAsync();
            return Ok(MapCar(car, activeBookings, users));
        }

        [HttpPatch("fleet/cars/{id:int}/use-now")]
        public async Task<IActionResult> UseNow(int id)
        {
            var car = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == id);
            if (car == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var activeBookings = await LoadActiveBookingsAsync();
            var isAdmin = User.IsInRole("admin");

            if (!isAdmin && GetOwnerId(car, activeBookings) != userId.Value)
            {
                return Forbid();
            }

            var currentStatus = ToUiStatus(car.Stato);
            car.Stato = currentStatus == "in uso"
                ? ToSqlStatus("non in uso")
                : ToSqlStatus("in uso");
            car.DataAggiornamento = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var users = await LoadUsersByIdAsync();
            return Ok(MapCar(car, activeBookings, users));
        }

        [Authorize(Roles = "admin")]
        [HttpPatch("fleet/cars/{id:int}/maintenance-approval")]
        public async Task<IActionResult> ApproveMaintenance(int id)
        {
            var car = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == id);
            if (car == null)
            {
                return NotFound();
            }

            car.Stato = ToSqlStatus("in manutenzione");
            car.DataAggiornamento = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var activeBookings = await LoadActiveBookingsAsync();
            var users = await LoadUsersByIdAsync();
            return Ok(MapCar(car, activeBookings, users));
        }

        [Authorize(Roles = "admin")]
        [HttpPost("fleet/cars")]
        public async Task<IActionResult> CreateCar([FromBody] FleetCarUpsertRequest request)
        {
            var ownerId = await ResolveOwnerIdAsync(request);
            if (ownerId == null)
            {
                return BadRequest("Assegnatario non valido.");
            }

            var car = new Veicolo
            {
                Marca = "Veicolo",
                Modello = "Nuovo",
                Tipo = "Auto",
                Stato = "Disponibile",
                LivelloCarburante = 1,
                Gruppo = NormalizeGroup(request.Gruppo, "Auto"),
                UtentePrenotatoID = ownerId.Value,
                DataCreazione = DateTime.UtcNow
            };

            FillCarFields(car, request);

            _context.Veicoli.Add(car);
            await _context.SaveChangesAsync();

            var activeBookings = await LoadActiveBookingsAsync();
            var users = await LoadUsersByIdAsync();
            return Ok(MapCar(car, activeBookings, users));
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("fleet/cars/{id:int}")]
        public async Task<IActionResult> DeleteCar(int id)
        {
            var car = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == id);
            if (car == null)
            {
                return NotFound();
            }

            _context.Veicoli.Remove(car);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = "admin")]
        [HttpPost("fleet/debug/seed")]
        public async Task<IActionResult> SeedDemoFleet()
        {
            var inserted = await DemoDataSeeder.ResetDemoAsync(_context);
            return Ok(new { inserted });
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("fleet/debug/clear")]
        public async Task<IActionResult> ClearFleetData()
        {
            _context.Prenotazioni.RemoveRange(_context.Prenotazioni);
            _context.Veicoli.RemoveRange(_context.Veicoli);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue("matricola") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }

        // Se esiste una prenotazione ancora aperta, la usiamo come fallback per capire l'assegnazione.
        private async Task<Dictionary<int, ActiveBookingInfo>> LoadActiveBookingsAsync()
        {
            return await _context.Prenotazioni
                .AsNoTracking()
                .Where(booking => booking.OraRilascio == null)
                .GroupBy(booking => booking.VeicoloId)
                .Select(group => new ActiveBookingInfo
                {
                    VeicoloId = group.Key,
                    UtenteId = group.OrderByDescending(booking => booking.OraPrenotazione)
                        .Select(booking => booking.UtenteId)
                        .FirstOrDefault(),
                    DataPrenotazione = group.OrderByDescending(booking => booking.OraPrenotazione)
                        .Select(booking => (DateTime?)booking.OraPrenotazione)
                        .FirstOrDefault()
                })
                .ToDictionaryAsync(item => item.VeicoloId);
        }

        private async Task<Dictionary<int, FleetUserReference>> LoadUsersByIdAsync()
        {
            return await _context.Utenti
                .AsNoTracking()
                .ToDictionaryAsync(
                    user => user.UtenteID,
                    user => new FleetUserReference
                    {
                        Id = user.UtenteID,
                        DisplayName = user.NomeCompleto,
                        Email = user.Email
                    });
        }

        // L'admin puo scrivere sia l'id sia nome/email. Qui traduciamo tutto in UtenteID.
        private async Task<int?> ResolveOwnerIdAsync(FleetCarUpsertRequest request)
        {
            if (request.PossessoreMatricola.HasValue)
            {
                var exists = await _context.Utenti
                    .AsNoTracking()
                    .AnyAsync(user => user.UtenteID == request.PossessoreMatricola.Value);

                if (exists)
                {
                    return request.PossessoreMatricola.Value;
                }
            }

            var query = NormalizeText(request.OwnerQuery);
            if (string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            var users = await _context.Utenti
                .AsNoTracking()
                .Select(user => new FleetUserReference
                {
                    Id = user.UtenteID,
                    DisplayName = user.NomeCompleto,
                    Email = user.Email
                })
                .ToListAsync();

            var exactMatches = users.Where(user =>
                NormalizeText(user.DisplayName) == query ||
                NormalizeText(user.Email) == query ||
                NormalizeText(user.Id.ToString()) == query ||
                NormalizeText(BuildUserLabel(user)) == query)
                .ToList();

            if (exactMatches.Count == 1)
            {
                return exactMatches[0].Id;
            }

            var partialMatches = users.Where(user =>
                NormalizeText(user.DisplayName).Contains(query) ||
                NormalizeText(user.Email).Contains(query))
                .ToList();

            return partialMatches.Count == 1 ? partialMatches[0].Id : null;
        }

        private static FleetCarResponse MapCar(
            Veicolo car,
            IReadOnlyDictionary<int, ActiveBookingInfo> activeBookings,
            IReadOnlyDictionary<int, FleetUserReference> users)
        {
            var ownerId = GetOwnerId(car, activeBookings);
            users.TryGetValue(ownerId ?? -1, out var owner);
            activeBookings.TryGetValue(car.VeicoloId, out var booking);

            return new FleetCarResponse
            {
                Id = car.VeicoloId,
                Modello = BuildModelLabel(car),
                Targa = car.Targa,
                PossessoreMatricola = ownerId,
                PossessoreNome = owner?.DisplayName,
                Gruppo = NormalizeGroup(car.Gruppo, car.Tipo),
                Chilometraggio = car.Chilometraggio,
                FuelLevel = Math.Clamp(car.LivelloCarburante, 0, 2),
                FuelType = car.Carburante,
                Stato = ToUiStatus(car.Stato),
                DataPossesso = car.DataPossesso ?? booking?.DataPrenotazione ?? car.DataAggiornamento ?? car.DataCreazione,
                ImageUrl = car.ImageUrl,
                RevisioneInizio = car.RevisioneInizio,
                RevisioneScadenza = CalculateExpiry(car.RevisioneInizio, 2),
                BolloInizio = car.BolloInizio,
                BolloScadenza = CalculateExpiry(car.BolloInizio, 1),
                TagliandoInizio = car.TagliandoInizio,
                TagliandoScadenza = CalculateExpiry(car.TagliandoInizio, 1),
                AssicurazioneInizio = car.AssicurazioneInizio,
                AssicurazioneScadenza = CalculateExpiry(car.AssicurazioneInizio, 1)
            };
        }

        private static int? GetOwnerId(Veicolo car, IReadOnlyDictionary<int, ActiveBookingInfo> activeBookings)
        {
            if (car.UtentePrenotatoID.HasValue)
            {
                return car.UtentePrenotatoID.Value;
            }

            return activeBookings.TryGetValue(car.VeicoloId, out var booking) ? booking.UtenteId : null;
        }

        // Qui teniamo in un solo punto tutti i campi "modificabili" del veicolo.
        private static void FillCarFields(Veicolo car, FleetCarUpsertRequest request)
        {
            var (marca, modello) = SplitModel(request.Modello, car.Marca, car.Modello);

            car.Marca = marca;
            car.Modello = modello;
            car.Targa = (request.Targa ?? string.Empty).Trim().ToUpperInvariant();
            car.Chilometraggio = Math.Max(request.Chilometraggio, 0);
            car.LivelloCarburante = Math.Clamp(request.FuelLevel, 0, 2);
            car.Carburante = request.FuelType?.Trim();
            car.Stato = ToSqlStatus(request.Stato);
            car.DataPossesso = request.DataPossesso;
            car.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
            car.RevisioneInizio = request.RevisioneInizio;
            car.BolloInizio = request.BolloInizio;
            car.TagliandoInizio = request.TagliandoInizio;
            car.AssicurazioneInizio = request.AssicurazioneInizio;
            car.RevisioneScadenza = CalculateExpiry(request.RevisioneInizio, 2);
            car.BolloScadenza = CalculateExpiry(request.BolloInizio, 1);
            car.TagliandoScadenza = CalculateExpiry(request.TagliandoInizio, 1);
            car.AssicurazioneScadenza = CalculateExpiry(request.AssicurazioneInizio, 1);
            car.Gruppo = NormalizeGroup(request.Gruppo, car.Tipo);
            car.DataAggiornamento = DateTime.UtcNow;
        }

        private static (string Marca, string Modello) SplitModel(string? value, string? oldMarca, string? oldModello)
        {
            var text = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return (oldMarca?.Trim() ?? "Veicolo", oldModello?.Trim() ?? "Senza modello");
            }

            var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                return (oldMarca?.Trim() ?? parts[0], parts[0]);
            }

            return (parts[0], parts[1]);
        }

        private static string BuildModelLabel(Veicolo car)
        {
            var marca = car.Marca?.Trim();
            var modello = car.Modello?.Trim();

            if (string.IsNullOrWhiteSpace(marca))
            {
                return modello ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(modello) || string.Equals(marca, modello, StringComparison.OrdinalIgnoreCase))
            {
                return marca;
            }

            return marca + " " + modello;
        }

        private static string NormalizeGroup(string? group, string? tipo)
        {
            var cleaned = (group ?? string.Empty).Trim().ToUpperInvariant();
            if (cleaned == Gruppo1 || cleaned == Gruppo2 || cleaned == Gruppo3)
            {
                return cleaned;
            }

            var tipoNormale = (tipo ?? string.Empty).Trim().ToLowerInvariant();
            return tipoNormale switch
            {
                "auto" => Gruppo1,
                "furgone" => Gruppo2,
                _ => Gruppo3
            };
        }

        private static string ToUiStatus(string? sqlStatus)
        {
            return NormalizeText(sqlStatus) switch
            {
                "disponibile" => "non in uso",
                "inuso" => "in uso",
                "manutenzione" => "in manutenzione",
                "richiestamanu" => "in richiesta manutenzione",
                "richiestamanutenzione" => "in richiesta manutenzione",
                "richiesta manutenzione" => "in richiesta manutenzione",
                _ => string.IsNullOrWhiteSpace(sqlStatus) ? "non in uso" : sqlStatus.Trim().ToLowerInvariant()
            };
        }

        private static string ToSqlStatus(string? uiStatus)
        {
            return NormalizeText(uiStatus) switch
            {
                "non in uso" => "Disponibile",
                "in uso" => "InUso",
                "in manutenzione" => "Manutenzione",
                "in richiesta manutenzione" => "RichiestaManu",
                _ => string.IsNullOrWhiteSpace(uiStatus) ? "Disponibile" : uiStatus.Trim()
            };
        }

        private static DateTime? CalculateExpiry(DateTime? startDate, int years)
        {
            return startDate?.AddYears(years);
        }

        private static string NormalizeText(string? value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static string BuildUserLabel(FleetUserReference user)
        {
            return string.IsNullOrWhiteSpace(user.Email)
                ? user.DisplayName
                : user.DisplayName + " | " + user.Email;
        }

        public class FleetCarUpsertRequest
        {
            public string? Modello { get; set; }
            public string? Targa { get; set; }
            public int? PossessoreMatricola { get; set; }
            public string? OwnerQuery { get; set; }
            public string? Gruppo { get; set; }
            public int Chilometraggio { get; set; }
            public int FuelLevel { get; set; }
            public string? FuelType { get; set; }
            public string? Stato { get; set; }
            public DateTime? DataPossesso { get; set; }
            public string? ImageUrl { get; set; }
            public DateTime? RevisioneInizio { get; set; }
            public DateTime? BolloInizio { get; set; }
            public DateTime? TagliandoInizio { get; set; }
            public DateTime? AssicurazioneInizio { get; set; }
        }

        private sealed class ActiveBookingInfo
        {
            public int VeicoloId { get; set; }
            public int UtenteId { get; set; }
            public DateTime? DataPrenotazione { get; set; }
        }

        private sealed class FleetUserReference
        {
            public int Id { get; set; }
            public string DisplayName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        private sealed class FleetCarResponse
        {
            public int Id { get; set; }
            public string Modello { get; set; } = string.Empty;
            public string Targa { get; set; } = string.Empty;
            public int? PossessoreMatricola { get; set; }
            public string? PossessoreNome { get; set; }
            public string Gruppo { get; set; } = Gruppo1;
            public int Chilometraggio { get; set; }
            public int FuelLevel { get; set; }
            public string? FuelType { get; set; }
            public string Stato { get; set; } = "non in uso";
            public DateTime? DataPossesso { get; set; }
            public string? ImageUrl { get; set; }
            public DateTime? RevisioneInizio { get; set; }
            public DateTime? RevisioneScadenza { get; set; }
            public DateTime? BolloInizio { get; set; }
            public DateTime? BolloScadenza { get; set; }
            public DateTime? TagliandoInizio { get; set; }
            public DateTime? TagliandoScadenza { get; set; }
            public DateTime? AssicurazioneInizio { get; set; }
            public DateTime? AssicurazioneScadenza { get; set; }
        }
    }
}
