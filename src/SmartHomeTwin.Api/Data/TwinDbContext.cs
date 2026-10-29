using Microsoft.EntityFrameworkCore;

namespace SmartHomeTwin.Api.Data;

public class TwinDbContext : DbContext
{
    public TwinDbContext(DbContextOptions<TwinDbContext> options) : base(options)
    {
    }

    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();
    public DbSet<ScenarioEntity> Scenarios => Set<ScenarioEntity>();
    public DbSet<SimulationRunEntity> Runs => Set<SimulationRunEntity>();
    public DbSet<ReadingEntity> Readings => Set<ReadingEntity>();
    public DbSet<HomeSnapshotEntity> HomeSnapshots => Set<HomeSnapshotEntity>();
    public DbSet<AlertEntity> Alerts => Set<AlertEntity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<DeviceEntity>(e =>
        {
            e.ToTable("Devices");
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(128);
            e.Property(x => x.Type).HasMaxLength(64);
            e.Property(x => x.Category).HasMaxLength(32);
        });

        b.Entity<ScenarioEntity>(e =>
        {
            e.ToTable("Scenarios");
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(128);
            e.Property(x => x.Description).HasMaxLength(1000);
        });

        b.Entity<SimulationRunEntity>(e =>
        {
            e.ToTable("SimulationRuns");
            e.Property(x => x.ScenarioId).HasMaxLength(64);
            e.HasOne<ScenarioEntity>().WithMany().HasForeignKey(x => x.ScenarioId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ReadingEntity>(e =>
        {
            e.ToTable("Readings");
            e.Property(x => x.DeviceId).HasMaxLength(64);
            // Readings are queried by run, device and time range.
            e.HasIndex(x => new { x.RunId, x.DeviceId, x.Timestamp }).IsUnique();
            e.HasOne<SimulationRunEntity>().WithMany().HasForeignKey(x => x.RunId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<DeviceEntity>().WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<HomeSnapshotEntity>(e =>
        {
            e.ToTable("HomeSnapshots");
            e.HasIndex(x => new { x.RunId, x.Timestamp }).IsUnique();
            e.HasOne<SimulationRunEntity>().WithMany().HasForeignKey(x => x.RunId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<AlertEntity>(e =>
        {
            e.ToTable("Alerts");
            e.Property(x => x.Type).HasMaxLength(64);
            e.Property(x => x.Message).HasMaxLength(500);
            e.HasIndex(x => new { x.RunId, x.Timestamp });
            e.HasOne<SimulationRunEntity>().WithMany().HasForeignKey(x => x.RunId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
