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

        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<Driver> Drivers => Set<Driver>();
        public DbSet<Assignment> Assignments => Set<Assignment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Vehicle>()
                .HasIndex(v => v.Targa)
                .IsUnique();

            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Vehicle)
                .WithMany(v => v.Assegnazioni)
                .HasForeignKey(a => a.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Driver)
                .WithMany(d => d.Assegnazioni)
                .HasForeignKey(a => a.DriverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
