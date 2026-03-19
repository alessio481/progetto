using FleetManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FleetManager;

public static class DemoDataSeeder
{
    public static async Task EnsureReadyAsync(ApplicationDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (await context.Utenti.AnyAsync())
        {
            return;
        }

        await ResetDemoAsync(context);
    }

    public static async Task<int> ResetDemoAsync(ApplicationDbContext context)
    {
        context.Prenotazioni.RemoveRange(context.Prenotazioni);
        context.Veicoli.RemoveRange(context.Veicoli);
        context.Utenti.RemoveRange(context.Utenti);
        await context.SaveChangesAsync();

        await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Prenotazioni', RESEED, 0)");
        await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Veicoli', RESEED, 0)");
        await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Utenti', RESEED, 0)");

        var users = CreateDemoUsers();
        context.Utenti.AddRange(users);
        await context.SaveChangesAsync();

        var driverIds = users
            .Where(user => user.Ruolo != "Admin")
            .Select(user => user.UtenteID)
            .ToArray();

        var vehicles = CreateDemoVehicles(driverIds);
        context.Veicoli.AddRange(vehicles);
        await context.SaveChangesAsync();

        return vehicles.Count;
    }

    private static List<Utente> CreateDemoUsers()
    {
        return new List<Utente>
        {
            new() { Nome = "Elena", Cognome = "Rinaldi", Email = "elena.rinaldi@fondazionesimonini.it", Password = "admin123", DataNascita = new DateTime(1987, 4, 18), Ruolo = "Admin", DataRegistrazione = DateTime.Now },
            new() { Nome = "Matteo", Cognome = "Sala", Email = "matteo.sala@fondazionesimonini.it", Password = "pass01", DataNascita = new DateTime(1991, 1, 12), Ruolo = "Driver", DataRegistrazione = DateTime.Now },
            new() { Nome = "Giulia", Cognome = "Conti", Email = "giulia.conti@fondazionesimonini.it", Password = "pass02", DataNascita = new DateTime(1993, 2, 27), Ruolo = "Driver", DataRegistrazione = DateTime.Now },
            new() { Nome = "Sara", Cognome = "Verdi", Email = "sara.verdi@fondazionesimonini.it", Password = "pass03", DataNascita = new DateTime(1990, 6, 9), Ruolo = "Driver", DataRegistrazione = DateTime.Now },
            new() { Nome = "Luca", Cognome = "Neri", Email = "luca.neri@fondazionesimonini.it", Password = "pass04", DataNascita = new DateTime(1988, 11, 23), Ruolo = "Driver", DataRegistrazione = DateTime.Now },
            new() { Nome = "Chiara", Cognome = "Rossi", Email = "chiara.rossi@fondazionesimonini.it", Password = "pass05", DataNascita = new DateTime(1994, 3, 15), Ruolo = "Driver", DataRegistrazione = DateTime.Now },
            new() { Nome = "Davide", Cognome = "Moretti", Email = "davide.moretti@fondazionesimonini.it", Password = "pass06", DataNascita = new DateTime(1989, 8, 30), Ruolo = "Driver", DataRegistrazione = DateTime.Now },
            new() { Nome = "Martina", Cognome = "Pellegrini", Email = "martina.pellegrini@fondazionesimonini.it", Password = "pass07", DataNascita = new DateTime(1992, 5, 11), Ruolo = "Driver", DataRegistrazione = DateTime.Now },
            new() { Nome = "Andrea", Cognome = "Gallo", Email = "andrea.gallo@fondazionesimonini.it", Password = "pass08", DataNascita = new DateTime(1987, 9, 20), Ruolo = "Driver", DataRegistrazione = DateTime.Now },
            new() { Nome = "Francesca", Cognome = "Villa", Email = "francesca.villa@fondazionesimonini.it", Password = "pass09", DataNascita = new DateTime(1995, 7, 14), Ruolo = "Driver", DataRegistrazione = DateTime.Now },
            new() { Nome = "Paolo", Cognome = "Riva", Email = "paolo.riva@fondazionesimonini.it", Password = "pass10", DataNascita = new DateTime(1986, 10, 4), Ruolo = "Driver", DataRegistrazione = DateTime.Now },
            new() { Nome = "Elisa", Cognome = "Marchetti", Email = "elisa.marchetti@fondazionesimonini.it", Password = "pass11", DataNascita = new DateTime(1991, 12, 17), Ruolo = "Driver", DataRegistrazione = DateTime.Now },
            new() { Nome = "Marco", Cognome = "De Santis", Email = "marco.desantis@fondazionesimonini.it", Password = "pass12", DataNascita = new DateTime(1985, 1, 29), Ruolo = "Driver", DataRegistrazione = DateTime.Now },
            new() { Nome = "Silvia", Cognome = "Fontana", Email = "silvia.fontana@fondazionesimonini.it", Password = "pass13", DataNascita = new DateTime(1993, 4, 3), Ruolo = "Driver", DataRegistrazione = DateTime.Now },
            new() { Nome = "Riccardo", Cognome = "Greco", Email = "riccardo.greco@fondazionesimonini.it", Password = "pass14", DataNascita = new DateTime(1988, 6, 26), Ruolo = "Driver", DataRegistrazione = DateTime.Now },
            new() { Nome = "Valentina", Cognome = "Ferretti", Email = "valentina.ferretti@fondazionesimonini.it", Password = "pass15", DataNascita = new DateTime(1994, 9, 8), Ruolo = "Driver", DataRegistrazione = DateTime.Now }
        };
    }

    private static List<Veicolo> CreateDemoVehicles(int[] ownerIds)
    {
        var templates = new[]
        {
            new DemoVehicleTemplate("Fiat", "Panda 1.0 Hybrid", "Auto", "HB731RK", 28640, 2, "Benzina", "InUso", "FONDAZIONE SETTORE-1", "2024-01-15", "2024-02-10", "2024-01-20", "2024-03-05", "2024-01-15", "https://images.unsplash.com/photo-1549399542-7e3f8b79c341?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Toyota", "Yaris Hybrid", "Auto", "FX210ML", 51720, 1, "Ibrido", "Disponibile", "FONDAZIONE SETTORE-1", "2023-05-08", "2024-05-15", "2024-05-31", "2024-06-20", "2024-05-08", "https://images.unsplash.com/photo-1553440569-bcc63803a83d?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Volkswagen", "Golf 2.0 TDI", "Auto", "GT904PN", 93110, 1, "Diesel", "Manutenzione", "FONDAZIONE SETTORE-2", "2022-01-19", "2024-12-20", "2024-12-31", "2024-08-22", "2024-12-19", "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Fiat", "500e Icon", "Auto", "EV552TS", 18800, 2, "Elettrico", "InUso", "FONDAZIONE SETTORE-3", "2025-02-03", "2025-02-28", "2025-02-28", "2025-02-15", "2025-02-03", "https://images.unsplash.com/photo-1617788138017-80ad40651399?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Renault", "Clio dCi", "Auto", "ZA118KL", 67400, 1, "Diesel", "Disponibile", "FONDAZIONE SETTORE-2", "2021-09-30", "2024-09-30", "2024-10-12", "2024-11-01", "2024-09-30", "https://images.unsplash.com/photo-1494976388531-d1058494cdd8?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Ford", "Transit Custom", "Furgone", "VF620AR", 121300, 0, "Diesel", "InUso", "FONDAZIONE SETTORE-2", "2020-06-11", "2024-06-11", "2024-06-30", "2024-07-10", "2024-06-11", "https://images.unsplash.com/photo-1609521263047-f8f205293f24?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Peugeot", "208 BlueHDi", "Auto", "LM406XC", 44280, 2, "Diesel", "RichiestaManu", "FONDAZIONE SETTORE-1", "2023-03-21", "2024-03-25", "2024-03-31", "2024-04-18", "2024-03-21", "https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Jeep", "Renegade 4xe", "Auto", "QW771ED", 35600, 2, "Ibrido Plug-in", "InUso", "FONDAZIONE SETTORE-3", "2024-04-09", "2024-04-30", "2024-04-30", "2024-05-16", "2024-04-09", "https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Citroen", "C3 Aircross", "Auto", "NB284PL", 26450, 1, "Benzina", "Disponibile", "FONDAZIONE SETTORE-1", "2024-07-14", "2024-07-20", "2024-07-31", "2024-08-25", "2024-07-14", "https://images.unsplash.com/photo-1511919884226-fd3cad34687c?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Mercedes", "Vito Tourer", "Furgone", "TR992CF", 84550, 1, "Diesel", "InUso", "FONDAZIONE SETTORE-2", "2021-11-18", "2024-11-18", "2024-11-30", "2024-12-02", "2024-11-18", "https://images.unsplash.com/photo-1541899481282-d53bffe3c35d?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Opel", "Corsa Edition", "Auto", "MK330SV", 30870, 2, "Benzina", "Disponibile", "FONDAZIONE SETTORE-3", "2024-10-02", "2024-10-08", "2024-10-31", "2024-11-14", "2024-10-02", "https://images.unsplash.com/photo-1502877338535-766e1452684a?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Nissan", "Qashqai e-Power", "Auto", "AS513DL", 22510, 2, "Ibrido", "InUso", "FONDAZIONE SETTORE-1", "2025-01-17", "2025-01-20", "2025-01-31", "2025-02-06", "2025-01-17", "https://images.unsplash.com/photo-1493238792000-8113da705763?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Hyundai", "i20 ConnectLine", "Auto", "PL208FT", 31840, 1, "Benzina", "Disponibile", "FONDAZIONE SETTORE-3", "2024-03-12", "2024-03-20", "2024-03-31", "2024-04-10", "2024-03-12", "https://images.unsplash.com/photo-1519641471654-76ce0107ad1b?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Kia", "Sportage HEV", "Auto", "DS641VM", 27110, 2, "Ibrido", "InUso", "FONDAZIONE SETTORE-1", "2024-06-03", "2024-06-12", "2024-06-30", "2024-07-09", "2024-06-03", "https://images.unsplash.com/photo-1504215680853-026ed2a45def?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Peugeot", "Partner BlueHDi", "Furgone", "FR520NB", 76420, 1, "Diesel", "InUso", "FONDAZIONE SETTORE-2", "2022-09-07", "2024-09-10", "2024-09-30", "2024-10-01", "2024-09-07", "https://images.unsplash.com/photo-1494976388901-750d2e7d52a9?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Skoda", "Octavia Wagon", "Auto", "BC174RM", 58230, 1, "Diesel", "Disponibile", "FONDAZIONE SETTORE-2", "2023-01-24", "2024-01-31", "2024-02-28", "2024-03-08", "2024-01-24", "https://images.unsplash.com/photo-1542282088-fe8426682b8f?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Renault", "Captur E-Tech", "Auto", "ZE805LU", 34990, 2, "Ibrido", "InUso", "FONDAZIONE SETTORE-3", "2024-02-14", "2024-02-20", "2024-02-29", "2024-03-15", "2024-02-14", "https://images.unsplash.com/photo-1549924231-f129b911e442?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Ford", "Puma ST-Line", "Auto", "GM486WP", 29210, 2, "Benzina", "Disponibile", "FONDAZIONE SETTORE-1", "2024-08-01", "2024-08-10", "2024-08-31", "2024-09-06", "2024-08-01", "https://images.unsplash.com/photo-1550355291-bbee04a92027?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Volkswagen", "Caddy Cargo", "Furgone", "RT913ZA", 88920, 1, "Diesel", "Manutenzione", "FONDAZIONE SETTORE-2", "2021-04-20", "2024-04-28", "2024-04-30", "2024-05-12", "2024-04-20", "https://images.unsplash.com/photo-1502161254066-6c74afbf07aa?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Fiat", "Tipo SW", "Auto", "KU338HV", 61230, 1, "Diesel", "InUso", "FONDAZIONE SETTORE-2", "2022-07-11", "2024-07-20", "2024-07-31", "2024-08-18", "2024-07-11", "https://images.unsplash.com/photo-1493238792000-8113da705763?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Jeep", "Compass e-Hybrid", "Auto", "YW604TR", 18450, 2, "Ibrido", "RichiestaManu", "FONDAZIONE SETTORE-3", "2025-01-05", "2025-01-14", "2025-01-31", "2025-02-05", "2025-01-05", "https://images.unsplash.com/photo-1511919884226-fd3cad34687c?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Citroen", "Berlingo Van", "Furgone", "ER129SK", 95410, 0, "Diesel", "InUso", "FONDAZIONE SETTORE-2", "2020-10-13", "2024-10-20", "2024-10-31", "2024-11-11", "2024-10-13", "https://images.unsplash.com/photo-1486496572940-2bb2341fdbdf?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Audi", "A3 Sportback TFSI", "Auto", "LN905CF", 40320, 2, "Benzina", "Disponibile", "FONDAZIONE SETTORE-1", "2023-11-08", "2024-11-16", "2024-11-30", "2024-12-04", "2024-11-08", "https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Toyota", "Corolla Touring Sports", "Auto", "TS447EN", 37800, 2, "Ibrido", "InUso", "FONDAZIONE SETTORE-1", "2024-05-22", "2024-05-31", "2024-05-31", "2024-06-18", "2024-05-22", "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Opel", "Combo Cargo", "Furgone", "PP731GL", 102600, 1, "Diesel", "Disponibile", "FONDAZIONE SETTORE-2", "2021-02-18", "2024-02-24", "2024-02-29", "2024-03-10", "2024-02-18", "https://images.unsplash.com/photo-1609521263047-f8f205293f24?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Suzuki", "Vitara Hybrid", "Auto", "CL200XT", 26590, 2, "Ibrido", "InUso", "FONDAZIONE SETTORE-3", "2024-09-09", "2024-09-18", "2024-09-30", "2024-10-07", "2024-09-09", "https://images.unsplash.com/photo-1502877338535-766e1452684a?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Nissan", "Juke Hybrid", "Auto", "VV315PH", 15780, 2, "Ibrido", "Disponibile", "FONDAZIONE SETTORE-3", "2025-02-10", "2025-02-18", "2025-02-28", "2025-03-06", "2025-02-10", "https://images.unsplash.com/photo-1542282088-fe8426682b8f?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Ford", "Tourneo Courier", "Furgone", "BR420NC", 71650, 1, "Diesel", "InUso", "FONDAZIONE SETTORE-2", "2022-05-06", "2024-05-15", "2024-05-31", "2024-06-11", "2024-05-06", "https://images.unsplash.com/photo-1549399542-7e3f8b79c341?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Hyundai", "Tucson HEV", "Auto", "ME908AL", 33210, 2, "Ibrido", "InUso", "FONDAZIONE SETTORE-1", "2024-03-05", "2024-03-14", "2024-03-31", "2024-04-04", "2024-03-05", "https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Renault", "Kangoo Van", "Furgone", "AA551FE", 110240, 0, "Diesel", "Manutenzione", "FONDAZIONE SETTORE-2", "2020-12-03", "2024-12-12", "2024-12-31", "2025-01-09", "2024-12-03", "https://images.unsplash.com/photo-1519641471654-76ce0107ad1b?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Kia", "Ceed SW", "Auto", "XZ274GG", 48770, 1, "Benzina", "Disponibile", "FONDAZIONE SETTORE-1", "2023-08-16", "2024-08-24", "2024-08-31", "2024-09-12", "2024-08-16", "https://images.unsplash.com/photo-1504215680853-026ed2a45def?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Mazda", "CX-30 Skyactiv", "Auto", "TY881KR", 29880, 2, "Benzina", "InUso", "FONDAZIONE SETTORE-3", "2024-06-18", "2024-06-25", "2024-06-30", "2024-07-19", "2024-06-18", "https://images.unsplash.com/photo-1549399542-7e3f8b79c341?auto=format&fit=crop&w=1200&q=80"),
            new DemoVehicleTemplate("Dacia", "Duster ECO-G", "Auto", "UF442MB", 36610, 1, "GPL", "Disponibile", "FONDAZIONE SETTORE-1", "2023-04-05", "2024-04-12", "2024-04-30", "2024-05-08", "2024-04-05", "https://images.unsplash.com/photo-1553440569-bcc63803a83d?auto=format&fit=crop&w=1200&q=80")
        };

        var vehicles = new List<Veicolo>();
        for (var index = 0; index < templates.Length; index++)
        {
            var item = templates[index];
            var ownerId = ownerIds[index % ownerIds.Length];
            var dataPossesso = DateTime.Parse(item.DataPossesso);
            var revisioneInizio = DateTime.Parse(item.RevisioneInizio);
            var bolloInizio = DateTime.Parse(item.BolloInizio);
            var tagliandoInizio = DateTime.Parse(item.TagliandoInizio);
            var assicurazioneInizio = DateTime.Parse(item.AssicurazioneInizio);

            vehicles.Add(new Veicolo
            {
                Targa = item.Targa,
                Marca = item.Marca,
                Modello = item.Modello,
                Tipo = item.Tipo,
                Stato = item.Stato,
                LivelloCarburante = item.FuelLevel,
                Chilometraggio = item.Chilometraggio,
                Carburante = item.FuelType,
                Gruppo = item.Gruppo,
                ImageUrl = item.ImageUrl,
                DataPossesso = dataPossesso,
                RevisioneInizio = revisioneInizio,
                RevisioneScadenza = revisioneInizio.AddYears(2),
                BolloInizio = bolloInizio,
                BolloScadenza = bolloInizio.AddYears(1),
                TagliandoInizio = tagliandoInizio,
                TagliandoScadenza = tagliandoInizio.AddYears(1),
                AssicurazioneInizio = assicurazioneInizio,
                AssicurazioneScadenza = assicurazioneInizio.AddYears(1),
                UtentePrenotatoID = ownerId,
                DataCreazione = DateTime.Now,
                DataAggiornamento = DateTime.Now
            });
        }

        return vehicles;
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
