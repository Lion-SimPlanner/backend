using LionSimPlanner.Scheduling.Domain.Entities;
using LionSimPlanner.Scheduling.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LionSimPlanner.Scheduling.Infrastructure;

/// <summary>
/// EF Core DbContext for the Scheduling module.
/// Maps exclusively to the "sched" PostgreSQL schema.
/// Contains NO navigation properties to hr or maint schemas — cross-schema joins are prohibited.
/// CaptainId, FirstOfficerId, InstructorId are stored as plain Guid columns without FK constraints.
/// </summary>
public class SchedulingDbContext(DbContextOptions<SchedulingDbContext> options) : DbContext(options)
{
    public DbSet<SimulatorSession> Sessions => Set<SimulatorSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("sched");

        modelBuilder.Entity<SimulatorSession>(e =>
        {
            e.ToTable("simulator_sessions");
            e.HasKey(s => s.SessionId);
            e.Property(s => s.SessionId).HasColumnName("session_id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(s => s.SimulatorId).HasColumnName("simulator_id").IsRequired();

            // Enums stored as strings for human readability in the DB
            e.Property(s => s.SessionType).HasColumnName("session_type").HasConversion<string>().HasMaxLength(30);
            e.Property(s => s.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);

            e.Property(s => s.StartTime).HasColumnName("start_time");
            e.Property(s => s.EndTime).HasColumnName("end_time");

            // Nullable Guid references — intentionally no FK constraints (cross-schema isolation)
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

            // Computed column is not mapped to DB — ignored by EF
            e.Ignore(s => s.DurationHours);

            // Performance indexes
            e.HasIndex(s => s.SimulatorId).HasDatabaseName("idx_sessions_simulator_id");
            e.HasIndex(s => s.Status).HasDatabaseName("idx_sessions_status");
            e.HasIndex(s => s.StartTime).HasDatabaseName("idx_sessions_start_time");
            e.HasIndex(s => new { s.SimulatorId, s.Status, s.StartTime })
                .HasDatabaseName("idx_sessions_aog_lookup");   // AOG handler fast path
        });
    }
}
