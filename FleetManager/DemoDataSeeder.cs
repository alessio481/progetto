using FleetManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FleetManager;

public static class DemoDataSeeder
{
    // Se il database e vuoto, carichiamo un piccolo dataset demo.
    public static async Task PreparaDatabaseDemoAsync(ApplicationDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        var ciSonoUtenti = await context.Utenti.AnyAsync();
        if (ciSonoUtenti)
        {
            return;
        }

        await RipristinaDatiDemoAsync(context);
    }

    // Svuota i dati attuali e ricarica gli utenti e i veicoli demo.
    public static async Task<int> RipristinaDatiDemoAsync(ApplicationDbContext context)
    {
        context.Veicoli.RemoveRange(context.Veicoli);
        context.Utenti.RemoveRange(context.Utenti);
        await context.SaveChangesAsync();

        await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Veicoli', RESEED, 0)");
        await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Utenti', RESEED, 0)");

        var utentiDemo = CreaUtentiDemo();
        context.Utenti.AddRange(utentiDemo);
        await context.SaveChangesAsync();

        var listaIdDriver = new List<int>();

        foreach (var utente in utentiDemo)
        {
            if (utente.Ruolo == "Admin")
            {
                continue;
            }

            listaIdDriver.Add(utente.UtenteID);
        }

        var veicoliDemo = CreaVeicoliDemo(listaIdDriver.ToArray());
        context.Veicoli.AddRange(veicoliDemo);
        await context.SaveChangesAsync();

        return veicoliDemo.Count;
    }

    private static List<Utente> CreaUtentiDemo()
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

    private static List<Veicolo> CreaVeicoliDemo(int[] idDriver)
    {
        var righeDemo = new List<RigaVeicoloDemo>
        {
            new("Fiat", "Panda 1.0 Hybrid", "Auto", "HB731RK", 28640, 2, "Benzina", "InUso", "FONDAZIONE SETTORE-1", "2024-01-15", "2024-02-10", "2024-01-20", "2024-03-05", "2024-01-15", true, "https://images.unsplash.com/photo-1549399542-7e3f8b79c341?auto=format&fit=crop&w=1200&q=80"),
            new("Toyota", "Yaris Hybrid", "Auto", "FX210ML", 51720, 1, "Ibrido", "Disponibile", "FONDAZIONE SETTORE-1", "2023-05-08", "2024-05-15", "2024-05-31", "2024-06-20", "2024-05-08", true, "https://images.unsplash.com/photo-1553440569-bcc63803a83d?auto=format&fit=crop&w=1200&q=80"),
            new("Volkswagen", "Golf 2.0 TDI", "Auto", "GT904PN", 93110, 1, "Diesel", "Manutenzione", "FONDAZIONE SETTORE-2", "2022-01-19", "2024-12-20", "2024-12-31", "2024-08-22", "2024-12-19", true, "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?auto=format&fit=crop&w=1200&q=80"),
            new("Fiat", "500e Icon", "Auto", "EV552TS", 18800, 2, "Elettrico", "InUso", "FONDAZIONE SETTORE-3", "2025-02-03", "2025-02-28", "2025-02-28", "2025-02-15", "2025-02-03", true, "https://images.unsplash.com/photo-1617788138017-80ad40651399?auto=format&fit=crop&w=1200&q=80"),
            new("Renault", "Clio dCi", "Auto", "ZA118KL", 67400, 1, "Diesel", "Disponibile", "FONDAZIONE SETTORE-2", "2021-09-30", "2024-09-30", "2024-10-12", "2024-11-01", "2024-09-30", true, "https://images.unsplash.com/photo-1494976388531-d1058494cdd8?auto=format&fit=crop&w=1200&q=80"),
            new("Ford", "Transit Custom", "Furgone", "VF620AR", 121300, 0, "Diesel", "InUso", "FONDAZIONE SETTORE-2", "2020-06-11", "2024-06-11", "2024-06-30", "2024-07-10", "2024-06-11", true, "https://images.unsplash.com/photo-1609521263047-f8f205293f24?auto=format&fit=crop&w=1200&q=80"),
            new("Peugeot", "208 BlueHDi", "Auto", "LM406XC", 44280, 2, "Diesel", "RichiestaManu", "FONDAZIONE SETTORE-1", "2023-03-21", "2024-03-25", "2024-03-31", "2024-04-18", "2024-03-21", true, "https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?auto=format&fit=crop&w=1200&q=80"),
            new("Jeep", "Renegade 4xe", "Auto", "QW771ED", 35600, 2, "Ibrido Plug-in", "InUso", "FONDAZIONE SETTORE-3", "2024-04-09", "2024-04-30", "2024-04-30", "2024-05-16", "2024-04-09", true, "https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1200&q=80"),
            new("Citroen", "C3 Aircross", "Auto", "NB284PL", 26450, 1, "Benzina", "Disponibile", "FONDAZIONE SETTORE-1", "2024-07-14", "2024-07-20", "2024-07-31", "2024-08-25", "2024-07-14", true, "https://images.unsplash.com/photo-1511919884226-fd3cad34687c?auto=format&fit=crop&w=1200&q=80"),
            new("Mercedes", "Vito Tourer", "Furgone", "TR992CF", 84550, 1, "Diesel", "InUso", "FONDAZIONE SETTORE-2", "2021-11-18", "2024-11-18", "2024-11-30", "2024-12-02", "2024-11-18", true, "https://images.unsplash.com/photo-1541899481282-d53bffe3c35d?auto=format&fit=crop&w=1200&q=80"),
            new("Opel", "Corsa Edition", "Auto", "MK330SV", 30870, 2, "Benzina", "Disponibile", "FONDAZIONE SETTORE-3", "2024-10-02", "2024-10-08", "2024-10-31", "2024-11-14", "2024-10-02", true, "https://images.unsplash.com/photo-1502877338535-766e1452684a?auto=format&fit=crop&w=1200&q=80"),
            new("Nissan", "Qashqai e-Power", "Auto", "AS513DL", 22510, 2, "Ibrido", "InUso", "FONDAZIONE SETTORE-1", "2025-01-17", "2025-01-20", "2025-01-31", "2025-02-06", "2025-01-17", true, "https://images.unsplash.com/photo-1493238792000-8113da705763?auto=format&fit=crop&w=1200&q=80"),
            new("Hyundai", "i20 ConnectLine", "Auto", "PL208FT", 31840, 1, "Benzina", "Disponibile", "FONDAZIONE SETTORE-3", "2024-03-12", "2024-03-20", "2024-03-31", "2024-04-10", "2024-03-12", true, "https://images.unsplash.com/photo-1519641471654-76ce0107ad1b?auto=format&fit=crop&w=1200&q=80"),
            new("Kia", "Sportage HEV", "Auto", "DS641VM", 27110, 2, "Ibrido", "InUso", "FONDAZIONE SETTORE-1", "2024-06-03", "2024-06-12", "2024-06-30", "2024-07-09", "2024-06-03", true, "https://images.unsplash.com/photo-1504215680853-026ed2a45def?auto=format&fit=crop&w=1200&q=80"),
            new("Peugeot", "Partner BlueHDi", "Furgone", "FR520NB", 76420, 1, "Diesel", "Disponibile", "FONDAZIONE SETTORE-2", "2022-09-07", "2024-09-10", "2024-09-30", "2024-10-01", "2024-09-07", true, "https://images.unsplash.com/photo-1494976388901-750d2e7d52a9?auto=format&fit=crop&w=1200&q=80"),
            new("Renault", "Kangoo Van", "Furgone", "AA551FE", 110240, 0, "Diesel", "Manutenzione", "FONDAZIONE SETTORE-2", "2020-12-03", "2024-12-12", "2024-12-31", "2025-01-09", "2024-12-03", false, "https://images.unsplash.com/photo-1519641471654-76ce0107ad1b?auto=format&fit=crop&w=1200&q=80"),
            new("Audi", "A3 Sportback TFSI", "Auto", "LN905CF", 40320, 2, "Benzina", "Disponibile", "FONDAZIONE SETTORE-1", "2023-11-08", "2024-11-16", "2024-11-30", "2024-12-04", "2024-11-08", false, "https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1200&q=80"),
            new("Suzuki", "Vitara Hybrid", "Auto", "CL200XT", 26590, 2, "Ibrido", "Disponibile", "FONDAZIONE SETTORE-3", "2024-09-09", "2024-09-18", "2024-09-30", "2024-10-07", "2024-09-09", false, "https://images.unsplash.com/photo-1502877338535-766e1452684a?auto=format&fit=crop&w=1200&q=80")
        };

        var veicoli = new List<Veicolo>();

        for (var indice = 0; indice < righeDemo.Count; indice++)
        {
            var riga = righeDemo[indice];
            int? idAssegnatario = null;

            if (riga.HaAssegnatario && indice < idDriver.Length)
            {
                idAssegnatario = idDriver[indice];
            }

            veicoli.Add(new Veicolo
            {
                Targa = riga.Targa,
                Marca = riga.Marca,
                Modello = riga.Modello,
                Tipo = riga.Tipo,
                Stato = riga.Stato,
                LivelloCarburante = riga.LivelloCarburante,
                Chilometraggio = riga.Chilometraggio,
                Carburante = riga.TipoCarburante,
                Gruppo = riga.Gruppo,
                ImageUrl = riga.UrlImmagine,
                DataPossesso = DateTime.Parse(riga.DataPossesso),
                RevisioneInizio = DateTime.Parse(riga.RevisioneInizio),
                BolloInizio = DateTime.Parse(riga.BolloInizio),
                TagliandoInizio = DateTime.Parse(riga.TagliandoInizio),
                AssicurazioneInizio = DateTime.Parse(riga.AssicurazioneInizio),
                UtentePrenotatoID = idAssegnatario,
                DataCreazione = DateTime.Now,
                DataAggiornamento = DateTime.Now
            });
        }

        return veicoli;
    }

    // Contiene una singola riga del dataset demo.
    private sealed class RigaVeicoloDemo
    {
        public RigaVeicoloDemo(
            string marca,
            string modello,
            string tipo,
            string targa,
            int chilometraggio,
            int livelloCarburante,
            string tipoCarburante,
            string stato,
            string gruppo,
            string dataPossesso,
            string revisioneInizio,
            string bolloInizio,
            string tagliandoInizio,
            string assicurazioneInizio,
            bool haAssegnatario,
            string urlImmagine)
        {
            Marca = marca;
            Modello = modello;
            Tipo = tipo;
            Targa = targa;
            Chilometraggio = chilometraggio;
            LivelloCarburante = livelloCarburante;
            TipoCarburante = tipoCarburante;
            Stato = stato;
            Gruppo = gruppo;
            DataPossesso = dataPossesso;
            RevisioneInizio = revisioneInizio;
            BolloInizio = bolloInizio;
            TagliandoInizio = tagliandoInizio;
            AssicurazioneInizio = assicurazioneInizio;
            HaAssegnatario = haAssegnatario;
            UrlImmagine = urlImmagine;
        }

        public string Marca { get; }
        public string Modello { get; }
        public string Tipo { get; }
        public string Targa { get; }
        public int Chilometraggio { get; }
        public int LivelloCarburante { get; }
        public string TipoCarburante { get; }
        public string Stato { get; }
        public string Gruppo { get; }
        public string DataPossesso { get; }
        public string RevisioneInizio { get; }
        public string BolloInizio { get; }
        public string TagliandoInizio { get; }
        public string AssicurazioneInizio { get; }
        public bool HaAssegnatario { get; }
        public string UrlImmagine { get; }
    }
}
