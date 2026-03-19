using FleetManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // Teniamo solo le tabelle che usa davvero la demo.
        public DbSet<Utente> Utenti { get; set; }
        public DbSet<Veicolo> Veicoli { get; set; }
        public DbSet<Prenotazione> Prenotazioni { get; set; }
    }
}
