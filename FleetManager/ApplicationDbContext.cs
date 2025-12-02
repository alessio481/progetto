using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace FleetManager.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Utente> Utenti { get; set; }
        public DbSet<Veicolo> Veicoli { get; set; }
        public DbSet<Prenotazione> Prenotazioni { get; set; }
        public DbSet<Segnalazione> Segnalazioni { get; set; }
        public DbSet<Manutenzione> Manutenzioni { get; set; }
        public DbSet<DashboardSnapshot> DashboardSnapshots { get; set; }
    }
}
