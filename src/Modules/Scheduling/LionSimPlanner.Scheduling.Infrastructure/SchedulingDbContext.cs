using LionSimPlanner.Scheduling.Domain.Entities;
using LionSimPlanner.Scheduling.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LionSimPlanner.Scheduling.Infrastructure;

public class SchedulingDbContext(DbContextOptions<SchedulingDbContext> options) : DbContext(options)
{
    public DbSet<SimulatorSession> Sessions => Set<SimulatorSession>();

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
        modelBuilder.HasDefaultSchema("sched");

        modelBuilder.Entity<SimulatorSession>(e =>
        {
            e.ToTable("simulator_sessions");
            e.HasKey(s => s.SessionId);
            e.Property(s => s.SessionId).HasColumnName("session_id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(s => s.SimulatorId).HasColumnName("simulator_id").IsRequired();

            e.Property(s => s.SessionType).HasColumnName("session_type").HasConversion<string>().HasMaxLength(30);
            e.Property(s => s.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);

            e.Property(s => s.StartTime).HasColumnName("start_time");
            e.Property(s => s.EndTime).HasColumnName("end_time");

            e.Property(s => s.CaptainId).HasColumnName("captain_id");
            e.Property(s => s.FirstOfficerId).HasColumnName("first_officer_id");
            e.Property(s => s.InstructorId).HasColumnName("instructor_id");
            e.Property(s => s.EngineerId).HasColumnName("engineer_id");

            e.Property(s => s.SyllabusId).HasColumnName("syllabus_id").HasMaxLength(200);
            e.Property(s => s.TraineeEmployeeCode).HasColumnName("trainee_employee_code").HasMaxLength(50);
            e.Property(s => s.IsGraded).HasColumnName("is_graded").HasDefaultValue(false);
            e.Property(s => s.GradeStatus).HasColumnName("grade_status").HasMaxLength(20);
            e.Property(s => s.InstructorNotes).HasColumnName("instructor_notes").HasColumnType("text");
            e.Property(s => s.CancellationReason).HasColumnName("cancellation_reason").HasColumnType("text");
            e.Property(s => s.CreatedAt).HasColumnName("created_at");
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at");

            e.Ignore(s => s.DurationHours);

            e.HasIndex(s => s.SimulatorId).HasDatabaseName("idx_sessions_simulator_id");
            e.HasIndex(s => s.Status).HasDatabaseName("idx_sessions_status");
            e.HasIndex(s => s.StartTime).HasDatabaseName("idx_sessions_start_time");
            e.HasIndex(s => new { s.SimulatorId, s.Status, s.StartTime })
                .HasDatabaseName("idx_sessions_aog_lookup");
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
