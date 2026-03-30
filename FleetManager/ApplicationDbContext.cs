using Microsoft.EntityFrameworkCore;

namespace FleetManager.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Queste sono le uniche tabelle usate davvero
        public DbSet<Utente> Utenti { get; set; }
        public DbSet<Veicolo> Veicoli { get; set; }
    }
}
