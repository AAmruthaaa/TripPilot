using Microsoft.EntityFrameworkCore;
using TripPilot.Models;

namespace TripPilot.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Trip> Trips { get; set; }
        public DbSet<VehicleProfile> VehicleProfiles { get; set; }
        public DbSet<TripStop> TripStops { get; set; }
        public DbSet<ExpenseItem> ExpenseItems { get; set; }
        public DbSet<ChecklistItem> ChecklistItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Vehicle)
                .WithOne(v => v.Trip)
                .HasForeignKey<VehicleProfile>(v => v.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Trip>()
                .HasMany(t => t.Stops)
                .WithOne(s => s.Trip)
                .HasForeignKey(s => s.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Trip>()
                .HasMany(t => t.Expenses)
                .WithOne(e => e.Trip)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Trip>()
                .HasMany(t => t.ChecklistItems)
                .WithOne(c => c.Trip)
                .HasForeignKey(c => c.TripId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}