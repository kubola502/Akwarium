using Microsoft.EntityFrameworkCore;

namespace Akwarium.Models
{
    public partial class AkwariumDbContext : DbContext
    {
        // DI-only ctor (połączenie podajesz w Program.cs)
        public AkwariumDbContext(DbContextOptions<AkwariumDbContext> options)
            : base(options) { }

        public virtual DbSet<Aquarium> Aquariums { get; set; } = null!;
        public virtual DbSet<Sensor> Sensors { get; set; } = null!;
        public virtual DbSet<SensorData> SensorData { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;
        public virtual DbSet<SensorThreshold> SensorThresholds { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ===== Users
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCAC"); // nazwa PK nie jest krytyczna
                // (opcjonalnie) unikalność Email, jeśli masz to w DB:
                // entity.HasIndex(e => e.Email).IsUnique();
            });

            // ===== Aquariums -> Users (NO ACTION)
            modelBuilder.Entity<Aquarium>(entity =>
            {
                entity.HasKey(e => e.AquariumId).HasName("PK__Aquarium__F223424F");

                entity.HasOne(d => d.User)
                      .WithMany(p => p.Aquaria)               // Uwaga: kolekcja w modelu to "Aquaria"
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.NoAction)      // zgodnie z NO ACTION w SQL
                      .HasConstraintName("FK__Aquariums__UserI__3A81B327");
            });

            // ===== Sensors -> Aquariums (NO ACTION)
            modelBuilder.Entity<Sensor>(entity =>
            {
                entity.HasKey(e => e.SensorId).HasName("PK__Sensors__D809841A");

                entity.HasOne(d => d.Aquarium)
                      .WithMany(p => p.Sensors)
                      .HasForeignKey(d => d.AquariumId)
                      .OnDelete(DeleteBehavior.NoAction)
                      .HasConstraintName("FK__Sensors__Aquariu__3D5E1FD2");
            });

            // ===== SensorData -> Sensors (NO ACTION), domyślna data i indeks
            modelBuilder.Entity<SensorData>(entity =>
            {
                entity.HasKey(e => e.SensorDataId).HasName("PK__SensorDa__14C885F0");

                entity.Property(e => e.TimeAdded)
                      .HasDefaultValueSql("(getdate())")
                      .ValueGeneratedOnAdd();

                entity.HasOne(d => d.Sensor)
                      .WithMany(p => p.SensorData)
                      .HasForeignKey(d => d.SensorId)
                      .OnDelete(DeleteBehavior.NoAction)
                      .HasConstraintName("FK__SensorDat__Senso__412EB0B6");

                entity.HasIndex(e => new { e.SensorId, e.TimeAdded });
            });

            modelBuilder.Entity<SensorThreshold>(entity =>
            {
                entity.HasKey(e => e.SensorThresholdId);
                entity.ToTable("SensorThresholds");

                entity.Property(e => e.MinValue);
                entity.Property(e => e.MaxValue);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
