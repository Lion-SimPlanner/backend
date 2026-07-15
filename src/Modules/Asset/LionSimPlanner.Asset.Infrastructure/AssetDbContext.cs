using LionSimPlanner.Asset.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LionSimPlanner.Asset.Infrastructure;

/// <summary>
/// EF Core DbContext for the Asset module.
/// Maps exclusively to the "maint" PostgreSQL schema.
/// No cross-schema references — EngineerID in SimulatorSession is a plain Guid, not an EF FK.
/// </summary>
public class AssetDbContext(DbContextOptions<AssetDbContext> options) : DbContext(options)
{
    public DbSet<Engineer>            Engineers   => Set<Engineer>();
    public DbSet<Simulator>           Simulators  => Set<Simulator>();
    public DbSet<MaintenanceChecklist> Checklists => Set<MaintenanceChecklist>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("maint");

        // ── Engineer ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Engineer>(e =>
        {
            e.ToTable("engineers");
            e.HasKey(x => x.EngineerID);
            e.Property(x => x.EngineerID).HasColumnName("engineer_id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.EmployeeCode).HasColumnName("employee_code").HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.EmployeeCode).IsUnique();
            e.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
            e.Property(x => x.ClearanceLevel).HasColumnName("clearance_level").HasMaxLength(50);
            e.Property(x => x.HardwareRatings)
                .HasColumnName("hardware_ratings")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());
            e.Property(x => x.ShiftStartTime).HasColumnName("shift_start_time");
            e.Property(x => x.ShiftEndTime).HasColumnName("shift_end_time");
            e.Property(x => x.IsOnCall).HasColumnName("is_on_call").HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        // ── Simulator ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Simulator>(e =>
        {
            e.ToTable("simulators");
            e.HasKey(x => x.SimulatorId);
            e.Property(x => x.SimulatorId).HasColumnName("simulator_id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(x => x.BayNumber).HasColumnName("bay_number").HasMaxLength(20);
            e.Property(x => x.AircraftType).HasColumnName("aircraft_type").HasMaxLength(50);
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("Ready");
            e.Property(x => x.LastStatusChangedByEngineerId).HasColumnName("last_status_changed_by_engineer_id");
            e.Property(x => x.LastStatusChangedByEngineerCode).HasColumnName("last_status_changed_by_engineer_code").HasMaxLength(50);
            e.Property(x => x.LastStatusChangedAt).HasColumnName("last_status_changed_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        // ── MaintenanceChecklist ──────────────────────────────────────────────
        modelBuilder.Entity<MaintenanceChecklist>(e =>
        {
            e.ToTable("maintenance_checklists");
            e.HasKey(x => x.ChecklistId);
            e.Property(x => x.ChecklistId).HasColumnName("checklist_id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.SimulatorId).HasColumnName("simulator_id").IsRequired();
            e.Property(x => x.EngineerIdRef).HasColumnName("engineer_id_ref");
            e.Property(x => x.EngineerCode).HasColumnName("engineer_code").HasMaxLength(50);
            e.Property(x => x.ChecklistDate).HasColumnName("checklist_date");
            e.Property(x => x.IsCleared).HasColumnName("is_cleared").HasDefaultValue(false);
            e.Property(x => x.Notes).HasColumnName("notes").HasColumnType("text");
            e.Property(x => x.SignedOffAt).HasColumnName("signed_off_at");
            e.Property(x => x.BlockingReason).HasColumnName("blocking_reason").HasColumnType("text");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            // One checklist per simulator per day
            e.HasIndex(x => new { x.SimulatorId, x.ChecklistDate })
                .IsUnique()
                .HasDatabaseName("uq_checklist_simulator_date");
        });
    }
}
