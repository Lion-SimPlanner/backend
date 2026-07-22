using LionSimPlanner.Personnel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LionSimPlanner.Personnel.Infrastructure;

public class PersonnelDbContext(DbContextOptions<PersonnelDbContext> options) : DbContext(options)
{
    public DbSet<Pilot> Pilots => Set<Pilot>();
    public DbSet<Instructor> Instructors => Set<Instructor>();

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
        modelBuilder.HasDefaultSchema("hr");

        modelBuilder.Entity<Pilot>(e =>
        {
            e.ToTable("pilots");
            e.HasKey(p => p.PilotId);
            e.Property(p => p.PilotId).HasColumnName("pilot_id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(p => p.EmployeeCode).HasColumnName("employee_code").HasMaxLength(50).IsRequired();
            e.HasIndex(p => p.EmployeeCode).IsUnique();
            e.Property(p => p.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
            e.Property(p => p.CorporateEmail).HasColumnName("corporate_email").HasMaxLength(300);
            e.Property(p => p.CompanyName).HasColumnName("company_name").HasMaxLength(200);
            e.Property(p => p.ContactNumber).HasColumnName("contact_number").HasMaxLength(50);
            e.Property(p => p.IsExternalUser).HasColumnName("is_external_user").HasDefaultValue(false).IsRequired();
            e.Property(p => p.FtlStatus).HasColumnName("ftl_status").HasMaxLength(100);
            e.Property(p => p.Rank).HasColumnName("rank").HasConversion<string>().HasMaxLength(30);
            e.Property(p => p.TypeRatings)
                .HasColumnName("type_ratings")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => string.IsNullOrWhiteSpace(v)
                        ? null
                        : System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null));
            e.Property(p => p.MedicalExpiry).HasColumnName("medical_expiry");
            e.Property(p => p.LastTrainingDate).HasColumnName("last_training_date");
            e.Property(p => p.NextTrainingDue).HasColumnName("next_training_due");
            e.Property(p => p.RequiredSyllabus).HasColumnName("required_syllabus").HasMaxLength(100);
            e.Property(p => p.LastDutyEndTime).HasColumnName("last_duty_end_time");
            e.Property(p => p.NextDutyStartTime).HasColumnName("next_duty_start_time");
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        });

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
