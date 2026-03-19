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
        private readonly ApplicationDbContext _context;

        public FleetApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("session/me")]
        public async Task<IActionResult> Me()
        {
            if (!int.TryParse(GetUserId(), out var userId))
            {
                return Unauthorized();
            }

            var user = await _context.Utenti
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.UtenteID == userId);

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
            if (!int.TryParse(GetUserId(), out var currentUserId))
            {
                return Unauthorized();
            }

            var activeBookings = await GetActiveBookingsAsync();
            var userLookup = await GetUserLookupAsync();
            var cars = await _context.Veicoli
                .AsNoTracking()
                .OrderBy(car => car.VeicoloId)
                .ToListAsync();

            var payload = cars
                .Select(car => ToDto(car, activeBookings, userLookup))
                .Where(car => User.IsInRole("admin") || car.PossessoreMatricola == currentUserId)
                .ToList();

            return Ok(new { value = payload });
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
            var activeBookings = await GetActiveBookingsAsync();
            var currentOwnerId = ResolveAssignedUserId(car, activeBookings);

            if (!isAdmin)
            {
                if (!int.TryParse(GetUserId(), out var userId))
                {
                    return Unauthorized();
                }

                if (currentOwnerId != userId)
                {
                    return Forbid();
                }
            }

            ApplySharedFields(car, request);

            if (isAdmin)
            {
                var ownerResolution = await ResolveOwnerAsync(request);
                if (!ownerResolution.OwnerId.HasValue)
                {
                    return BadRequest(ownerResolution.ErrorMessage ?? "Assegnatario non valido.");
                }

                car.UtentePrenotatoID = ownerResolution.OwnerId.Value;
                car.Gruppo = NormalizeGroup(request.Gruppo, car.Tipo);
            }

            await _context.SaveChangesAsync();

            var userLookup = await GetUserLookupAsync();
            return Ok(ToDto(car, activeBookings, userLookup));
        }

        [HttpPatch("fleet/cars/{id:int}/maintenance-request")]
        public async Task<IActionResult> RequestMaintenance(int id)
        {
            var car = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == id);
            if (car == null)
            {
                return NotFound();
            }

            var isAdmin = User.IsInRole("admin");
            var activeBookings = await GetActiveBookingsAsync();
            var currentOwnerId = ResolveAssignedUserId(car, activeBookings);

            if (!int.TryParse(GetUserId(), out var userId))
            {
                return Unauthorized();
            }

            if (!isAdmin && currentOwnerId != userId)
            {
                return Forbid();
            }

            car.Stato = ToSqlStatus("in richiesta manutenzione");
            car.DataAggiornamento = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var userLookup = await GetUserLookupAsync();
            return Ok(ToDto(car, activeBookings, userLookup));
        }

        [HttpPatch("fleet/cars/{id:int}/use-now")]
        public async Task<IActionResult> UseNow(int id)
        {
            var car = await _context.Veicoli.FirstOrDefaultAsync(item => item.VeicoloId == id);
            if (car == null)
            {
                return NotFound();
            }

            if (!int.TryParse(GetUserId(), out var userId))
            {
                return Unauthorized();
            }

            var activeBookings = await GetActiveBookingsAsync();
            var currentOwnerId = ResolveAssignedUserId(car, activeBookings);
            if (currentOwnerId != userId && !User.IsInRole("admin"))
            {
                return Forbid();
            }

            var currentStatus = ToUiStatus(car.Stato);
            car.Stato = currentStatus == "in uso"
                ? ToSqlStatus("non in uso")
                : ToSqlStatus("in uso");
            car.DataAggiornamento = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var userLookup = await GetUserLookupAsync();
            return Ok(ToDto(car, activeBookings, userLookup));
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

            var activeBookings = await GetActiveBookingsAsync();
            var userLookup = await GetUserLookupAsync();
            return Ok(ToDto(car, activeBookings, userLookup));
        }

        [Authorize(Roles = "admin")]
        [HttpPost("fleet/cars")]
        public async Task<IActionResult> CreateCar([FromBody] FleetCarUpsertRequest request)
        {
            var ownerResolution = await ResolveOwnerAsync(request);
            if (!ownerResolution.OwnerId.HasValue)
            {
                return BadRequest(ownerResolution.ErrorMessage ?? "Assegnatario non valido.");
            }

            var car = new Veicolo
            {
                Targa = string.Empty,
                Marca = "Veicolo",
                Modello = "Nuovo",
                Tipo = "Auto",
                Stato = "Disponibile",
                LivelloCarburante = 1,
                Chilometraggio = 0,
                UtentePrenotatoID = ownerResolution.OwnerId.Value,
                Gruppo = NormalizeGroup(request.Gruppo, "Auto"),
                DataCreazione = DateTime.UtcNow
            };

            ApplySharedFields(car, request);

            _context.Veicoli.Add(car);
            await _context.SaveChangesAsync();

            var activeBookings = await GetActiveBookingsAsync();
            var userLookup = await GetUserLookupAsync();
            return Ok(ToDto(car, activeBookings, userLookup));
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

        private string? GetUserId()
        {
            return User.FindFirstValue("matricola") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private async Task<Dictionary<int, ActiveBookingInfo>> GetActiveBookingsAsync()
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

        private async Task<Dictionary<int, FleetUserReference>> GetUserLookupAsync()
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

        private async Task<OwnerResolution> ResolveOwnerAsync(FleetCarUpsertRequest request)
        {
            if (request.PossessoreMatricola.HasValue)
            {
                var owner = await _context.Utenti
                    .AsNoTracking()
                    .FirstOrDefaultAsync(user => user.UtenteID == request.PossessoreMatricola.Value);

                if (owner != null)
                {
                    return new OwnerResolution { OwnerId = owner.UtenteID };
                }
            }

            var query = NormalizeLookup(request.OwnerQuery);
            if (string.IsNullOrWhiteSpace(query))
            {
                return new OwnerResolution { ErrorMessage = "Inserisci il nome o l'email dell'assegnatario." };
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

            var exactMatches = users
                .Where(user =>
                    NormalizeLookup(user.DisplayName) == query ||
                    NormalizeLookup(user.Email) == query ||
                    NormalizeLookup(BuildOwnerChoice(user)) == query ||
                    NormalizeLookup(user.Id.ToString()) == query)
                .ToList();

            if (exactMatches.Count == 1)
            {
                return new OwnerResolution { OwnerId = exactMatches[0].Id };
            }

            if (exactMatches.Count > 1)
            {
                return new OwnerResolution { ErrorMessage = "Assegnatario ambiguo: scrivi nome e cognome completi oppure scegli dal suggerimento." };
            }

            var partialMatches = users
                .Where(user =>
                    NormalizeLookup(user.DisplayName).Contains(query) ||
                    NormalizeLookup(user.Email).Contains(query))
                .ToList();

            if (partialMatches.Count == 1)
            {
                return new OwnerResolution { OwnerId = partialMatches[0].Id };
            }

            if (partialMatches.Count > 1)
            {
                return new OwnerResolution { ErrorMessage = "Ho trovato piu utenti simili. Scegli un suggerimento piu preciso." };
            }

            return new OwnerResolution { ErrorMessage = "Utente assegnatario non trovato." };
        }

        private static FleetCarResponse ToDto(
            Veicolo car,
            IReadOnlyDictionary<int, ActiveBookingInfo> activeBookings,
            IReadOnlyDictionary<int, FleetUserReference> userLookup)
        {
            activeBookings.TryGetValue(car.VeicoloId, out var activeBooking);
            var ownerId = ResolveAssignedUserId(car, activeBookings);
            userLookup.TryGetValue(ownerId ?? -1, out var owner);

            return new FleetCarResponse
            {
                Id = car.VeicoloId,
                Modello = BuildDisplayModel(car),
                Targa = car.Targa,
                PossessoreMatricola = ownerId,
                PossessoreNome = owner?.DisplayName,
                Gruppo = NormalizeGroup(car.Gruppo, car.Tipo),
                Chilometraggio = car.Chilometraggio,
                FuelLevel = Math.Clamp(car.LivelloCarburante, 0, 2),
                FuelType = car.Carburante,
                Stato = ToUiStatus(car.Stato),
                DataPossesso = car.DataPossesso ?? activeBooking?.DataPrenotazione ?? car.DataAggiornamento ?? car.DataCreazione,
                ImageUrl = car.ImageUrl,
                RevisioneInizio = car.RevisioneInizio,
                RevisioneScadenza = car.RevisioneScadenza,
                BolloInizio = car.BolloInizio,
                BolloScadenza = car.BolloScadenza,
                TagliandoInizio = car.TagliandoInizio,
                TagliandoScadenza = car.TagliandoScadenza,
                AssicurazioneInizio = car.AssicurazioneInizio,
                AssicurazioneScadenza = car.AssicurazioneScadenza
            };
        }

        private static int? ResolveAssignedUserId(Veicolo car, IReadOnlyDictionary<int, ActiveBookingInfo> activeBookings)
        {
            if (car.UtentePrenotatoID.HasValue)
            {
                return car.UtentePrenotatoID.Value;
            }

            return activeBookings.TryGetValue(car.VeicoloId, out var activeBooking)
                ? activeBooking.UtenteId
                : null;
        }

        private static void ApplySharedFields(Veicolo car, FleetCarUpsertRequest request)
        {
            var (marca, modello) = SplitModelInput(request.Modello, car.Marca, car.Modello);

            car.Marca = marca;
            car.Modello = modello;
            car.Targa = request.Targa?.Trim().ToUpperInvariant() ?? string.Empty;
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

        private static DateTime? CalculateExpiry(DateTime? startDate, int years)
        {
            return startDate?.AddYears(years);
        }

        private static (string Marca, string Modello) SplitModelInput(string? input, string? fallbackMarca, string? fallbackModello)
        {
            var value = (input ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return (fallbackMarca?.Trim() ?? "Veicolo", fallbackModello?.Trim() ?? "Senza modello");
            }

            var parts = value.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                return (fallbackMarca?.Trim() ?? parts[0], parts[0]);
            }

            return (parts[0], parts[1]);
        }

        private static string BuildDisplayModel(Veicolo car)
        {
            var marca = car.Marca?.Trim();
            var modello = car.Modello?.Trim();

            if (string.IsNullOrWhiteSpace(marca))
            {
                return modello ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(modello))
            {
                return marca;
            }

            if (string.Equals(marca, modello, StringComparison.OrdinalIgnoreCase))
            {
                return marca;
            }

            return marca + " " + modello;
        }

        private static string NormalizeGroup(string? explicitGroup, string? tipo)
        {
            var group = (explicitGroup ?? string.Empty).Trim().ToUpperInvariant();
            if (group is "FONDAZIONE SETTORE-1" or "FONDAZIONE SETTORE-2" or "FONDAZIONE SETTORE-3")
            {
                return group;
            }

            var vehicleType = (tipo ?? string.Empty).Trim().ToLowerInvariant();
            return vehicleType switch
            {
                "auto" => "FONDAZIONE SETTORE-1",
                "furgone" => "FONDAZIONE SETTORE-2",
                _ => "FONDAZIONE SETTORE-3"
            };
        }

        private static string ToUiStatus(string? sqlStatus)
        {
            var status = (sqlStatus ?? string.Empty).Trim().ToLowerInvariant();
            return status switch
            {
                "disponibile" => "non in uso",
                "inuso" => "in uso",
                "manutenzione" => "in manutenzione",
                "richiestamanu" => "in richiesta manutenzione",
                "richiestamanutenzione" => "in richiesta manutenzione",
                "richiesta manutenzione" => "in richiesta manutenzione",
                _ => string.IsNullOrWhiteSpace(status) ? "non in uso" : status
            };
        }

        private static string ToSqlStatus(string? uiStatus)
        {
            var status = (uiStatus ?? string.Empty).Trim().ToLowerInvariant();
            return status switch
            {
                "non in uso" => "Disponibile",
                "in uso" => "InUso",
                "in manutenzione" => "Manutenzione",
                "in richiesta manutenzione" => "RichiestaManu",
                _ => string.IsNullOrWhiteSpace(uiStatus) ? "Disponibile" : uiStatus.Trim()
            };
        }

        private static string NormalizeLookup(string? value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static string BuildOwnerChoice(FleetUserReference user)
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

        private sealed class OwnerResolution
        {
            public int? OwnerId { get; set; }
            public string? ErrorMessage { get; set; }
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
            public string Gruppo { get; set; } = "E-ONE";
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
