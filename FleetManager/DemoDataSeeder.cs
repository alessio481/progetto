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
            new("Fiat", "Panda 1.0 Hybrid", "Auto", "HB731RK", 28640, 2, "Benzina", "InUso", 1, "2024-01-15", "2024-02-10", "2024-01-20", "2024-03-05", "2024-01-15", true, "https://www.marchiauto.it/files/21028092_O_667d22dbdbe18.jpg"),
            new("Toyota", "Yaris Hybrid", "Auto", "FX210ML", 51720, 1, "Ibrido", "Disponibile", 1, "2023-05-08", "2024-05-15", "2024-05-31", "2024-06-20", "2024-05-08", true, "https://spazio4to.spaziogroup.com/files/2022/07/toyota-yaris-gr-hybrid-768x400-1.jpg"),
            new("Volkswagen", "Golf 2.0 TDI", "Auto", "GT904PN", 93110, 1, "Diesel", "Manutenzione", 2, "2022-01-19", "2024-12-20", "2024-12-31", "2024-08-22", "2024-12-19", true, "https://cdn-datak.motork.net/configurator-cover/cars/it/1600/VOLKSWAGEN/GOLF/43893_BERLINA-5-PORTE/volkswagen-golf-cover.jpg"),
            new("Fiat", "500e Icon", "Auto", "EV552TS", 18800, 2, "Elettrico", "InUso", 3, "2025-02-03", "2025-02-28", "2025-02-28", "2025-02-15", "2025-02-03", true, "https://rcs.cdn.publieditor.it/w640/M1458_02.jpg"),
            new("Renault", "Clio dCi", "Auto", "ZA118KL", 67400, 1, "Diesel", "Disponibile", 2, "2021-09-30", "2024-09-30", "2024-10-12", "2024-11-01", "2024-09-30", true, "https://www.njuskalo.hr/image-xlsize/auti/renault-clio-1.5-dci-slika-174396398.jpg"),
            new("Ford", "Transit Custom", "Furgone", "VF620AR", 121300, 0, "Diesel", "InUso", 2, "2020-06-11", "2024-06-11", "2024-06-30", "2024-07-10", "2024-06-11", true, "https://d2e5b8shawuel2.cloudfront.net/vehicle/299427/hrv/original.jpg"),
            new("Peugeot", "208 BlueHDi", "Auto", "LM406XC", 44280, 2, "Diesel", "RichiestaManu", 1, "2023-03-21", "2024-03-25", "2024-03-31", "2024-04-18", "2024-03-21", true, "https://immagini.alvolante.it/sites/default/files/styles/image_gallery_big/public/prova_lettori_anteprima/2018/08/peugeot_1024x768.jpg"),
            new("Jeep", "Renegade 4xe", "Auto", "QW771ED", 35600, 2, "Ibrido Plug-in", "InUso", 3, "2024-04-09", "2024-04-30", "2024-04-30", "2024-05-16", "2024-04-09", true, "https://rcs.cdn.publieditor.it/w640/M1025_04.jpg"),
            new("Citroen", "C3 Aircross", "Auto", "NB284PL", 26450, 1, "Benzina", "Disponibile", 1, "2024-07-14", "2024-07-20", "2024-07-31", "2024-08-25", "2024-07-14", true, "https://www.newsauto.it/wp-content/uploads/2021/02/Nuovo-Citroen-C3-Aircross-1.jpg"),
            new("Mercedes", "Vito Tourer", "Furgone", "TR992CF", 84550, 1, "Diesel", "InUso", 2, "2021-11-18", "2024-11-18", "2024-11-30", "2024-12-02", "2024-11-18", true, "https://mezzicommerciali.it/wp-content/uploads/bfi_thumb/Mercedes%20Vito%20Tourer-ouv933l1r2z5vi201atbihbrvoh8y25otj3z4a9feg.jpg"),
            new("Opel", "Corsa Edition", "Auto", "MK330SV", 30870, 2, "Benzina", "Disponibile", 3, "2024-10-02", "2024-10-08", "2024-10-31", "2024-11-14", "2024-10-02", true, "https://cdn.brandini.it/cover-prod/upload-6707a72e6ff132.55885846.jpg"),
            new("Nissan", "Qashqai e-Power", "Auto", "AS513DL", 22510, 2, "Ibrido", "InUso", 1, "2025-01-17", "2025-01-20", "2025-01-31", "2025-02-06", "2025-01-17", true, "https://storage.googleapis.com/fp-media/1/2024/03/NISSAN.jpg"),
            new("Hyundai", "i20 ConnectLine", "Auto", "PL208FT", 31840, 1, "Benzina", "Disponibile", 3, "2024-03-12", "2024-03-20", "2024-03-31", "2024-04-10", "2024-03-12", true, "https://immagini.alvolante.it/sites/default/files/styles/image_gallery_big/public/news_galleria/2020/09/hyundai-i20-n-line-2021_6.jpg"),
            new("Kia", "Sportage HEV", "Auto", "DS641VM", 27110, 2, "Ibrido", "InUso", 1, "2024-06-03", "2024-06-12", "2024-06-30", "2024-07-09", "2024-06-03", true, "https://immagini.alvolante.it/sites/default/files/styles/anteprima_970/public/prova_galleria/2023/01/kia-sportage-16-t-gdi-hev-prova-2022-03_09_resize.jpg"),
            new("Peugeot", "Partner BlueHDi", "Furgone", "FR520NB", 76420, 1, "Diesel", "Disponibile", 2, "2022-09-07", "2024-09-10", "2024-09-30", "2024-10-01", "2024-09-07", true, "https://cdn.dealerk.it/dealer/datafiles/vehicle/images/$original$/2431/k0h0a8ljpehXxmHP.jpeg"),
            new("Renault", "Kangoo Van", "Furgone", "AA551FE", 110240, 0, "Diesel", "Manutenzione", 2, "2020-12-03", "2024-12-12", "2024-12-31", "2025-01-09", "2024-12-03", false, "https://immagini.alvolante.it/sites/default/files/styles/image_gallery_big/public/news_galleria/2022/06/renault-kangoo-e-tech-electric-2022-06.jpg"),
            new("Audi", "A3 Sportback TFSI", "Auto", "LN905CF", 40320, 2, "Benzina", "Disponibile", 1, "2023-11-08", "2024-11-16", "2024-11-30", "2024-12-04", "2024-11-08", false, "https://cdn-xy.drivek.com/eyJidWNrZXQiOiJkYXRhay1jZG4teHkiLCJrZXkiOiJjb25maWd1cmF0b3ItY292ZXIvY2Fycy9pdC9vcmlnaW5hbC9BVURJL0EzLVNQT1JUQkFDSy80NDEzNF9IQVRDSEJBQ0stNS1ET09SUy9hdWRpLWEzLXNwb3J0YmFjay1jb3Zlci5qcGciLCJlZGl0cyI6eyJyZXNpemUiOnsid2lkdGgiOjEwMjQsImhlaWdodCI6bnVsbCwiZml0IjoiY292ZXIifX19"),
            new("Suzuki", "Vitara Hybrid", "Auto", "CL200XT", 26590, 2, "Ibrido", "Disponibile", 3, "2024-09-09", "2024-09-18", "2024-09-30", "2024-10-07", "2024-09-09", false, "https://bonaventuramotors.it/wp-content/uploads/2021/12/31916FF2-F493-442D-9653-84330BC7D204-scaled.jpeg")
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
            int gruppo,
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
        public int Gruppo { get; }
        public string DataPossesso { get; }
        public string RevisioneInizio { get; }
        public string BolloInizio { get; }
        public string TagliandoInizio { get; }
        public string AssicurazioneInizio { get; }
        public bool HaAssegnatario { get; }
        public string UrlImmagine { get; }
    }
}
