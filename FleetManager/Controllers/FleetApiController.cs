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
            var users = await _context.Utenti
                .AsNoTracking()
                .Where(user => user.Ruolo != "Admin" && user.Ruolo != "admin")
                .OrderBy(user => user.UtenteID)
                .ToListAsync();

            if (!users.Any())
            {
                return BadRequest("Non ci sono utenti disponibili a cui assegnare i veicoli demo.");
            }

            var vehicles = BuildDemoVehicles(users.Select(user => user.UtenteID).ToArray());
            _context.Veicoli.AddRange(vehicles);
            await _context.SaveChangesAsync();

            return Ok(new { inserted = vehicles.Count });
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

        private static List<Veicolo> BuildDemoVehicles(int[] ownerIds)
        {
            var templates = new[]
            {
                new DemoVehicleTemplate("Fiat", "Panda 1.0 Hybrid", "Auto", "HB731RK", 28640, 2, "Benzina", "in uso", "FONDAZIONE SETTORE-1", "2024-01-15", "2024-02-10", "2024-01-20", "2024-03-05", "2024-01-15", "https://images.unsplash.com/photo-1549399542-7e3f8b79c341?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Toyota", "Yaris Hybrid", "Auto", "FX210ML", 51720, 1, "Ibrido", "non in uso", "FONDAZIONE SETTORE-1", "2023-05-08", "2024-05-15", "2024-05-31", "2024-06-20", "2024-05-08", "https://images.unsplash.com/photo-1553440569-bcc63803a83d?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Volkswagen", "Golf 2.0 TDI", "Auto", "GT904PN", 93110, 1, "Diesel", "in manutenzione", "FONDAZIONE SETTORE-2", "2022-01-19", "2024-12-20", "2024-12-31", "2024-08-22", "2024-12-19", "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Fiat", "500e Icon", "Auto", "EV552TS", 18800, 2, "Elettrico", "in uso", "FONDAZIONE SETTORE-3", "2025-02-03", "2025-02-28", "2025-02-28", "2025-02-15", "2025-02-03", "https://images.unsplash.com/photo-1617788138017-80ad40651399?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Renault", "Clio dCi", "Auto", "ZA118KL", 67400, 1, "Diesel", "non in uso", "FONDAZIONE SETTORE-2", "2021-09-30", "2024-09-30", "2024-10-12", "2024-11-01", "2024-09-30", "https://images.unsplash.com/photo-1494976388531-d1058494cdd8?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Ford", "Transit Custom", "Furgone", "VF620AR", 121300, 0, "Diesel", "in uso", "FONDAZIONE SETTORE-2", "2020-06-11", "2024-06-11", "2024-06-30", "2024-07-10", "2024-06-11", "https://images.unsplash.com/photo-1609521263047-f8f205293f24?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Peugeot", "208 BlueHDi", "Auto", "LM406XC", 44280, 2, "Diesel", "in richiesta manutenzione", "FONDAZIONE SETTORE-1", "2023-03-21", "2024-03-25", "2024-03-31", "2024-04-18", "2024-03-21", "https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Jeep", "Renegade 4xe", "Auto", "QW771ED", 35600, 2, "Ibrido Plug-in", "in uso", "FONDAZIONE SETTORE-3", "2024-04-09", "2024-04-30", "2024-04-30", "2024-05-16", "2024-04-09", "https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Citroen", "C3 Aircross", "Auto", "NB284PL", 26450, 1, "Benzina", "non in uso", "FONDAZIONE SETTORE-1", "2024-07-14", "2024-07-20", "2024-07-31", "2024-08-25", "2024-07-14", "https://images.unsplash.com/photo-1511919884226-fd3cad34687c?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Mercedes", "Vito Tourer", "Furgone", "TR992CF", 84550, 1, "Diesel", "in uso", "FONDAZIONE SETTORE-2", "2021-11-18", "2024-11-18", "2024-11-30", "2024-12-02", "2024-11-18", "https://images.unsplash.com/photo-1541899481282-d53bffe3c35d?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Opel", "Corsa Edition", "Auto", "MK330SV", 30870, 2, "Benzina", "non in uso", "FONDAZIONE SETTORE-3", "2024-10-02", "2024-10-08", "2024-10-31", "2024-11-14", "2024-10-02", "https://images.unsplash.com/photo-1502877338535-766e1452684a?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Nissan", "Qashqai e-Power", "Auto", "AS513DL", 22510, 2, "Ibrido", "in uso", "FONDAZIONE SETTORE-1", "2025-01-17", "2025-01-20", "2025-01-31", "2025-02-06", "2025-01-17", "https://images.unsplash.com/photo-1493238792000-8113da705763?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Hyundai", "i20 ConnectLine", "Auto", "PL208FT", 31840, 1, "Benzina", "non in uso", "FONDAZIONE SETTORE-3", "2024-03-12", "2024-03-20", "2024-03-31", "2024-04-10", "2024-03-12", "https://images.unsplash.com/photo-1519641471654-76ce0107ad1b?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Kia", "Sportage HEV", "Auto", "DS641VM", 27110, 2, "Ibrido", "in uso", "FONDAZIONE SETTORE-1", "2024-06-03", "2024-06-12", "2024-06-30", "2024-07-09", "2024-06-03", "https://images.unsplash.com/photo-1504215680853-026ed2a45def?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Peugeot", "Partner BlueHDi", "Furgone", "FR520NB", 76420, 1, "Diesel", "in uso", "FONDAZIONE SETTORE-2", "2022-09-07", "2024-09-10", "2024-09-30", "2024-10-01", "2024-09-07", "https://images.unsplash.com/photo-1494976388901-750d2e7d52a9?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Skoda", "Octavia Wagon", "Auto", "BC174RM", 58230, 1, "Diesel", "non in uso", "FONDAZIONE SETTORE-2", "2023-01-24", "2024-01-31", "2024-02-28", "2024-03-08", "2024-01-24", "https://images.unsplash.com/photo-1542282088-fe8426682b8f?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Renault", "Captur E-Tech", "Auto", "ZE805LU", 34990, 2, "Ibrido", "in uso", "FONDAZIONE SETTORE-3", "2024-02-14", "2024-02-20", "2024-02-29", "2024-03-15", "2024-02-14", "https://images.unsplash.com/photo-1549924231-f129b911e442?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Ford", "Puma ST-Line", "Auto", "GM486WP", 29210, 2, "Benzina", "non in uso", "FONDAZIONE SETTORE-1", "2024-08-01", "2024-08-10", "2024-08-31", "2024-09-06", "2024-08-01", "https://images.unsplash.com/photo-1550355291-bbee04a92027?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Volkswagen", "Caddy Cargo", "Furgone", "RT913ZA", 88920, 1, "Diesel", "in manutenzione", "FONDAZIONE SETTORE-2", "2021-04-20", "2024-04-28", "2024-04-30", "2024-05-12", "2024-04-20", "https://images.unsplash.com/photo-1502161254066-6c74afbf07aa?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Fiat", "Tipo SW", "Auto", "KU338HV", 61230, 1, "Diesel", "in uso", "FONDAZIONE SETTORE-2", "2022-07-11", "2024-07-20", "2024-07-31", "2024-08-18", "2024-07-11", "https://images.unsplash.com/photo-1493238792000-8113da705763?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Jeep", "Compass e-Hybrid", "Auto", "YW604TR", 18450, 2, "Ibrido", "in richiesta manutenzione", "FONDAZIONE SETTORE-3", "2025-01-05", "2025-01-14", "2025-01-31", "2025-02-05", "2025-01-05", "https://images.unsplash.com/photo-1511919884226-fd3cad34687c?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Citroen", "Berlingo Van", "Furgone", "ER129SK", 95410, 0, "Diesel", "in uso", "FONDAZIONE SETTORE-2", "2020-10-13", "2024-10-20", "2024-10-31", "2024-11-11", "2024-10-13", "https://images.unsplash.com/photo-1486496572940-2bb2341fdbdf?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Audi", "A3 Sportback TFSI", "Auto", "LN905CF", 40320, 2, "Benzina", "non in uso", "FONDAZIONE SETTORE-1", "2023-11-08", "2024-11-16", "2024-11-30", "2024-12-04", "2024-11-08", "https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Toyota", "Corolla Touring Sports", "Auto", "TS447EN", 37800, 2, "Ibrido", "in uso", "FONDAZIONE SETTORE-1", "2024-05-22", "2024-05-31", "2024-05-31", "2024-06-18", "2024-05-22", "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Opel", "Combo Cargo", "Furgone", "PP731GL", 102600, 1, "Diesel", "non in uso", "FONDAZIONE SETTORE-2", "2021-02-18", "2024-02-24", "2024-02-29", "2024-03-10", "2024-02-18", "https://images.unsplash.com/photo-1609521263047-f8f205293f24?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Suzuki", "Vitara Hybrid", "Auto", "CL200XT", 26590, 2, "Ibrido", "in uso", "FONDAZIONE SETTORE-3", "2024-09-09", "2024-09-18", "2024-09-30", "2024-10-07", "2024-09-09", "https://images.unsplash.com/photo-1502877338535-766e1452684a?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Nissan", "Juke Hybrid", "Auto", "VV315PH", 15780, 2, "Ibrido", "non in uso", "FONDAZIONE SETTORE-3", "2025-02-10", "2025-02-18", "2025-02-28", "2025-03-06", "2025-02-10", "https://images.unsplash.com/photo-1542282088-fe8426682b8f?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Ford", "Tourneo Courier", "Furgone", "BR420NC", 71650, 1, "Diesel", "in uso", "FONDAZIONE SETTORE-2", "2022-05-06", "2024-05-15", "2024-05-31", "2024-06-11", "2024-05-06", "https://images.unsplash.com/photo-1549399542-7e3f8b79c341?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Hyundai", "Tucson HEV", "Auto", "ME908AL", 33210, 2, "Ibrido", "in uso", "FONDAZIONE SETTORE-1", "2024-03-05", "2024-03-14", "2024-03-31", "2024-04-04", "2024-03-05", "https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Renault", "Kangoo Van", "Furgone", "AA551FE", 110240, 0, "Diesel", "in manutenzione", "FONDAZIONE SETTORE-2", "2020-12-03", "2024-12-12", "2024-12-31", "2025-01-09", "2024-12-03", "https://images.unsplash.com/photo-1519641471654-76ce0107ad1b?auto=format&fit=crop&w=1200&q=80"),
                new DemoVehicleTemplate("Kia", "Ceed SW", "Auto", "XZ274GG", 48770, 1, "Benzina", "non in uso", "FONDAZIONE SETTORE-1", "2023-08-16", "2024-08-24", "2024-08-31", "2024-09-12", "2024-08-16", "https://images.unsplash.com/photo-1504215680853-026ed2a45def?auto=format&fit=crop&w=1200&q=80")
            };

            var vehicles = new List<Veicolo>();
            for (var index = 0; index < templates.Length; index++)
            {
                var template = templates[index];
                var ownerId = ownerIds[index % ownerIds.Length];
                var dataPossesso = DateTime.Parse(template.DataPossesso);
                var revisioneInizio = DateTime.Parse(template.RevisioneInizio);
                var bolloInizio = DateTime.Parse(template.BolloInizio);
                var tagliandoInizio = DateTime.Parse(template.TagliandoInizio);
                var assicurazioneInizio = DateTime.Parse(template.AssicurazioneInizio);

                vehicles.Add(new Veicolo
                {
                    Targa = template.Targa,
                    Marca = template.Marca,
                    Modello = template.Modello,
                    Tipo = template.Tipo,
                    Stato = ToSqlStatus(template.Stato),
                    LivelloCarburante = template.FuelLevel,
                    Chilometraggio = template.Chilometraggio,
                    Carburante = template.FuelType,
                    Gruppo = template.Gruppo,
                    ImageUrl = template.ImageUrl,
                    DataPossesso = dataPossesso,
                    RevisioneInizio = revisioneInizio,
                    RevisioneScadenza = CalculateExpiry(revisioneInizio, 2),
                    BolloInizio = bolloInizio,
                    BolloScadenza = CalculateExpiry(bolloInizio, 1),
                    TagliandoInizio = tagliandoInizio,
                    TagliandoScadenza = CalculateExpiry(tagliandoInizio, 1),
                    AssicurazioneInizio = assicurazioneInizio,
                    AssicurazioneScadenza = CalculateExpiry(assicurazioneInizio, 1),
                    UtentePrenotatoID = ownerId,
                    DataCreazione = DateTime.UtcNow,
                    DataAggiornamento = DateTime.UtcNow
                });
            }

            return vehicles;
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

        private sealed record DemoVehicleTemplate(
            string Marca,
            string Modello,
            string Tipo,
            string Targa,
            int Chilometraggio,
            int FuelLevel,
            string FuelType,
            string Stato,
            string Gruppo,
            string DataPossesso,
            string RevisioneInizio,
            string BolloInizio,
            string TagliandoInizio,
            string AssicurazioneInizio,
            string ImageUrl);
    }
}
