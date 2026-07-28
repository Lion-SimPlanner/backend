using LionSimPlanner.Asset.Domain.Entities;
using LionSimPlanner.Asset.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LionSimPlanner.Asset.Infrastructure;

public class AssetDbContext(DbContextOptions<AssetDbContext> options) : DbContext(options)
{
    public DbSet<Engineer>             Engineers       => Set<Engineer>();
    public DbSet<Simulator>            Simulators      => Set<Simulator>();
    public DbSet<MaintenanceLog>       MaintenanceLogs => Set<MaintenanceLog>();
    public DbSet<MaintenanceChecklist> Checklists      => Set<MaintenanceChecklist>();
    public DbSet<SimulatorDefect>      Defects         => Set<SimulatorDefect>();

    public override int SaveChanges()
    {
        NormalizeDateTimes();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        NormalizeDateTimes();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeDateTimes();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        NormalizeDateTimes();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("maint");

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
            e.Property(x => x.CheckoutTime).HasColumnName("checkout_time");
            e.Property(x => x.IsOnCall).HasColumnName("is_on_call").HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Simulator>(e =>
        {
            e.ToTable("simulators");
            e.HasKey(x => x.SimulatorId);
            e.Property(x => x.SimulatorId).HasColumnName("simulator_id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(x => x.BayNumber).HasColumnName("bay_number").HasMaxLength(20);
            e.Property(x => x.AircraftType).HasColumnName("aircraft_type").HasMaxLength(50);
            e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).HasDefaultValue(SimulatorStatus.Ready);
            e.Property(x => x.LastStatusChangedByEngineerId).HasColumnName("last_status_changed_by_engineer_id");
            e.Property(x => x.LastStatusChangedByEngineerCode).HasColumnName("last_status_changed_by_engineer_code").HasMaxLength(50);
            e.Property(x => x.LastStatusChangedAt).HasColumnName("last_status_changed_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<MaintenanceLog>(e =>
        {
            e.ToTable("maintenance_logs");
            e.HasKey(x => x.MaintenanceLogId);
            e.Property(x => x.MaintenanceLogId).HasColumnName("maintenance_log_id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.SimulatorId).HasColumnName("simulator_id").IsRequired();
            e.Property(x => x.Severity).HasColumnName("severity").HasMaxLength(30).IsRequired();
            e.Property(x => x.FaultDescription).HasColumnName("fault_description").HasColumnType("text").IsRequired();
            e.Property(x => x.ResolutionDescription).HasColumnName("resolution_description").HasColumnType("text");
            e.Property(x => x.ResolvedAt).HasColumnName("resolved_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasOne(x => x.Simulator)
                .WithMany(x => x.MaintenanceLogs)
                .HasForeignKey(x => x.SimulatorId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.SimulatorId).HasDatabaseName("idx_maintenance_logs_simulator_id");
            e.HasIndex(x => x.ResolvedAt).HasDatabaseName("idx_maintenance_logs_resolved_at");
        });

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
            e.HasIndex(x => new { x.SimulatorId, x.ChecklistDate })
                .IsUnique()
                .HasDatabaseName("uq_checklist_simulator_date");
        });

        modelBuilder.Entity<SimulatorDefect>(e =>
        {
            e.ToTable("simulator_defects");
            e.HasKey(x => x.DefectId);
            e.Property(x => x.DefectId).HasColumnName("defect_id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.SimulatorId).HasColumnName("simulator_id").IsRequired();
            e.Property(x => x.SessionId).HasColumnName("session_id");
            e.Property(x => x.ReportedBy).HasColumnName("reported_by").HasMaxLength(200).IsRequired();
            e.Property(x => x.SystemAffected).HasColumnName("system_affected").HasMaxLength(100).IsRequired();
            e.Property(x => x.Severity).HasColumnName("severity").HasMaxLength(30).IsRequired();
            e.Property(x => x.InstructorNotes).HasColumnName("instructor_notes").HasColumnType("text").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("Open").IsRequired();
            e.Property(x => x.ResolutionNotes).HasColumnName("resolution_notes").HasColumnType("text");
            e.Property(x => x.ResolvedByEngineerId).HasColumnName("resolved_by_engineer_id");
            e.Property(x => x.ResolvedByEngineerCode).HasColumnName("resolved_by_engineer_code").HasMaxLength(50);
            e.Property(x => x.ResolvedAt).HasColumnName("resolved_at");
            e.Property(x => x.ReportedAt).HasColumnName("reported_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasOne(x => x.Simulator)
                .WithMany(x => x.Defects)
                .HasForeignKey(x => x.SimulatorId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.SimulatorId).HasDatabaseName("idx_defects_simulator_id");
            e.HasIndex(x => x.Severity).HasDatabaseName("idx_defects_severity");
            e.HasIndex(x => x.Status).HasDatabaseName("idx_defects_status");
        });

        ApplyUtcDateTimeConverters(modelBuilder);
    }

    private static void ApplyUtcDateTimeConverters(ModelBuilder modelBuilder)
    {
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => ToUtc(v),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? ToUtc(v.Value) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(utcConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableUtcConverter);
                }
            }
        }
    }

    private void NormalizeDateTimes()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added && entry.State != EntityState.Modified)
            {
                continue;
            }

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.ClrType == typeof(DateTime) && property.CurrentValue is DateTime dt)
                {
                    property.CurrentValue = ToUtc(dt);
                }
                else if (property.Metadata.ClrType == typeof(DateTime?) && property.CurrentValue is DateTime nDt)
                {
                    property.CurrentValue = ToUtc(nDt);
                }
            }
        }
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
