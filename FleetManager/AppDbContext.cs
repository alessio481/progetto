using FleetManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // DbSets - rappresentano le tabelle del database
        public DbSet<Veicolo> Veicoli { get; set; }
        public DbSet<Utente> Utenti { get; set; }
        public DbSet<PostoAuto> PostiAuto { get; set; }
        public DbSet<Prenotazione> Prenotazioni { get; set; }
        public DbSet<Manutenzione> Manutenzioni { get; set; }
        public DbSet<Segnalazione> Segnalazioni { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Veicolo -> PostoAuto (FK)
            modelBuilder.Entity<Veicolo>()
                .HasOne(v => v.PostoAuto)
                .WithMany()
                .HasForeignKey(v => v.PostoID)
                .OnDelete(DeleteBehavior.Restrict);

            // Prenotazione -> Utente
            modelBuilder.Entity<Prenotazione>()
                .HasOne(p => p.Utente)
                .WithMany()
                .HasForeignKey(p => p.UtenteID);

            // Prenotazione -> Veicolo
            modelBuilder.Entity<Prenotazione>()
                .HasOne(p => p.Veicolo)
                .WithMany()
                .HasForeignKey(p => p.VeicoloID);

            // Manutenzione -> Veicolo
            modelBuilder.Entity<Manutenzione>()
                .HasOne(m => m.Veicolo)
                .WithMany()
                .HasForeignKey(m => m.VeicoloID);

            // Segnalazione -> Utente
            modelBuilder.Entity<Segnalazione>()
                .HasOne(s => s.Utente)
                .WithMany()
                .HasForeignKey(s => s.UtenteID);

            // Segnalazione -> Veicolo
            modelBuilder.Entity<Segnalazione>()
                .HasOne(s => s.Veicolo)
                .WithMany()
                .HasForeignKey(s => s.VeicoloID);

            // Segnalazione -> PostoAuto
            modelBuilder.Entity<Segnalazione>()
                .HasOne(s => s.PostoAuto)
                .WithMany()
                .HasForeignKey(s => s.PostoID);
        }

    }
}












