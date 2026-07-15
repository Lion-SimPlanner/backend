using LionSimPlanner.Personnel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LionSimPlanner.Personnel.Infrastructure;

/// <summary>
/// EF Core DbContext for the Personnel module.
/// Maps exclusively to the "hr" PostgreSQL schema.
/// Contains NO references to Scheduling or Asset entities — schema isolation is structural.
/// </summary>
public class PersonnelDbContext(DbContextOptions<PersonnelDbContext> options) : DbContext(options)
{
    public DbSet<Pilot> Pilots => Set<Pilot>();
    public DbSet<Instructor> Instructors => Set<Instructor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("hr");

        // ── Pilot ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Pilot>(e =>
        {
            e.ToTable("pilots");
            e.HasKey(p => p.PilotId);
            e.Property(p => p.PilotId).HasColumnName("pilot_id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(p => p.EmployeeCode).HasColumnName("employee_code").HasMaxLength(50).IsRequired();
            e.HasIndex(p => p.EmployeeCode).IsUnique();
            e.Property(p => p.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
            e.Property(p => p.CorporateEmail).HasColumnName("corporate_email").HasMaxLength(300);
            e.Property(p => p.Rank).HasColumnName("rank").HasConversion<string>().HasMaxLength(30);
            e.Property(p => p.TypeRatings)
                .HasColumnName("type_ratings")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());
            e.Property(p => p.MedicalExpiry).HasColumnName("medical_expiry");
            e.Property(p => p.LastTrainingDate).HasColumnName("last_training_date");
            e.Property(p => p.NextTrainingDue).HasColumnName("next_training_due");
            e.Property(p => p.RequiredSyllabus).HasColumnName("required_syllabus").HasMaxLength(100);
            e.Property(p => p.LastDutyEndTime).HasColumnName("last_duty_end_time");
            e.Property(p => p.NextDutyStartTime).HasColumnName("next_duty_start_time");
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        });

        // ── Instructor ───────────────────────────────────────────────────────
        modelBuilder.Entity<Instructor>(e =>
        {
            e.ToTable("instructors");
            e.HasKey(i => i.InstructorId);
            e.Property(i => i.InstructorId).HasColumnName("instructor_id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(i => i.EmployeeCode).HasColumnName("employee_code").HasMaxLength(50).IsRequired();
            e.HasIndex(i => i.EmployeeCode).IsUnique();
            e.Property(i => i.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
            e.Property(i => i.CorporateEmail).HasColumnName("corporate_email").HasMaxLength(300);
            e.Property(i => i.RoleLevel).HasColumnName("role_level").HasConversion<string>().HasMaxLength(10);
            e.Property(i => i.CertifiedTypes)
                .HasColumnName("certified_types")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());
            e.Property(i => i.AuthorizedSyllabi)
                .HasColumnName("authorized_syllabi")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());
            e.Property(i => i.LicenseExpiry).HasColumnName("license_expiry");
            e.Property(i => i.LastDutyEndTime).HasColumnName("last_duty_end_time");
            e.Property(i => i.NextDutyStartTime).HasColumnName("next_duty_start_time");
            e.Property(i => i.CurrentMonthlyHours).HasColumnName("current_monthly_hours");
            e.Property(i => i.MaxMonthlyHours).HasColumnName("max_monthly_hours");
            e.Property(i => i.CreatedAt).HasColumnName("created_at");
            e.Property(i => i.UpdatedAt).HasColumnName("updated_at");
        });
    }
}
