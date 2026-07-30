using LionSimPlanner.Shared.Hubs;
using LionSimPlanner.API.Seeding;
using LionSimPlanner.API.Infrastructure;
using LionSimPlanner.Asset.Infrastructure;
using LionSimPlanner.Notifications;
using LionSimPlanner.Personnel.Infrastructure;
using LionSimPlanner.Personnel.Infrastructure.CmsSync;
using LionSimPlanner.Scheduling.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Quartz;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. JSON Serializer Configuration
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Lion SimPlanner API",
        Version     = "v1",
        Description = "Validation-driven scheduling platform for Level D Full Flight Simulator operations."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.ApiKey,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter: Bearer {your JWT token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        },
        Array.Empty<string>()
    }});
});

// 2. JWT Authentication Setup
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer           = builder.Configuration.GetValue<bool>("Jwt:ValidateIssuer"),
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidateAudience         = builder.Configuration.GetValue<bool>("Jwt:ValidateAudience"),
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };

        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub/simplanner"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("AdminOnly",      p => p.RequireRole("Admin"));
    opts.AddPolicy("EngineerOnly",   p => p.RequireRole("Engineer"));
    opts.AddPolicy("InstructorOnly", p => p.RequireRole("Instructor"));
    opts.AddPolicy("PilotOrAbove",   p => p.RequireRole("Pilot", "Instructor", "Admin"));
});

// 3. CORS Configuration (Handles both Localhost and Vercel Deployment)
var configOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
var railwayOrigins = (builder.Configuration["RailwayCorsOrigins"] ?? "")
    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
var allOrigins = (configOrigins ?? []).Concat(railwayOrigins).ToArray();
if (allOrigins.Length == 0)
    allOrigins = new[] { "http://localhost:3000" };

builder.Services.AddCors(opts =>
    opts.AddPolicy("Frontend", p => p
        .WithOrigins(allOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

builder.Services.AddSignalR();

// 4. Database Contexts Setup
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

builder.Services.AddDbContext<PersonnelDbContext>(o =>
    o.UseNpgsql(connectionString,
        npgsql => npgsql.MigrationsHistoryTable("__efmigrations", "hr"))
     .ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning))
     .UseSnakeCaseNamingConvention());

builder.Services.AddDbContext<SchedulingDbContext>(o =>
    o.UseNpgsql(connectionString,
        npgsql => npgsql.MigrationsHistoryTable("__efmigrations", "sched"))
     .ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning))
     .UseSnakeCaseNamingConvention());

builder.Services.AddDbContext<AssetDbContext>(o =>
    o.UseNpgsql(connectionString,
        npgsql => npgsql.MigrationsHistoryTable("__efmigrations", "maint"))
     .ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning))
     .UseSnakeCaseNamingConvention());

// 5. MediatR, Notifications, and Services
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(PersonnelDbContext).Assembly,
        typeof(SchedulingDbContext).Assembly,
        typeof(AssetDbContext).Assembly
    );
});

builder.Services.Configure<GmailOptions>(builder.Configuration.GetSection("Notifications:Gmail"));
builder.Services.AddSingleton<IEmailNotificationService, EmailNotificationService>();

builder.Services.Configure<CmsOptions>(builder.Configuration.GetSection("Cms"));
builder.Services.AddHttpClient<CmsApiClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CmsOptions>>().Value;
    client.BaseAddress = new Uri(string.IsNullOrEmpty(opts.BaseUrl) ? "https://localhost" : opts.BaseUrl);
    if (!string.IsNullOrEmpty(opts.ApiKey))
        client.DefaultRequestHeaders.Add("X-API-Key", opts.ApiKey);
});

// 6. Quartz Background Jobs
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("CmsSyncJob", "CmsSync");
    q.AddJob<CmsSyncJob>(o => o.WithIdentity(jobKey));
    q.AddTrigger(o => o
        .ForJob(jobKey)
        .WithIdentity("CmsSyncTrigger", "CmsSync")
        .WithCronSchedule(
            builder.Configuration.GetValue("Quartz:CmsSyncCron", "0 0 0 * * ?")!,
            x => x.InTimeZone(TimeZoneInfo.Utc))
        .WithDescription("Daily midnight CMS roster sync"));
});
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

// Enable Swagger in Development AND Production staging for API testing
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lion SimPlanner API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseRouting();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<SimPlannerHub>("/hub/simplanner");

// 7. Decoupled Auto-Migration and Seeding Logic
// Runs in Development OR in Production if Database:AutoMigrateOnStartup is set to true
var shouldInitializeDb = app.Environment.IsDevelopment() 
    || builder.Configuration.GetValue<bool>("Database:AutoMigrateOnStartup");

if (shouldInitializeDb)
{
    using var scope = app.Services.CreateScope();
    var svc = scope.ServiceProvider;
    var log = svc.GetRequiredService<ILogger<Program>>();
    var hrDb    = svc.GetRequiredService<PersonnelDbContext>();
    var schedDb = svc.GetRequiredService<SchedulingDbContext>();
    var maintDb = svc.GetRequiredService<AssetDbContext>();

    log.LogInformation("Starting automated database migrations and seeding...");

    try
    {
        await EnsureMigrationHistorySnakeCaseAsync(hrDb.Database, "hr");
        await EnsureMigrationHistorySnakeCaseAsync(schedDb.Database, "sched");
        await EnsureMigrationHistorySnakeCaseAsync(maintDb.Database, "maint");
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Migration history normalization failed on startup.");
    }

    try
    {
        await hrDb.Database.MigrateAsync();
    }
    catch (PostgresException ex) when (ex.SqlState == "42P07")
    {
        log.LogWarning("Personnel migration skipped because relation already exists.");
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Personnel migration failed on startup.");
    }

    try
    {
        await schedDb.Database.MigrateAsync();
    }
    catch (PostgresException ex) when (ex.SqlState == "42P07")
    {
        log.LogWarning("Scheduling migration skipped because relation already exists.");
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Scheduling migration failed on startup.");
    }

    try
    {
        await EnsureAssetMigrationHistoryBaselineAsync(maintDb);
        await maintDb.Database.MigrateAsync();
    }
    catch (PostgresException ex) when (ex.SqlState == "42P07")
    {
        log.LogWarning("Asset migration skipped because relation already exists.");
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Asset migration failed on startup.");
    }

    try
    {
        await maintDb.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE maint.engineers
            ADD COLUMN IF NOT EXISTS checkout_time timestamp with time zone;

            CREATE TABLE IF NOT EXISTS maint.maintenance_logs (
                maintenance_log_id uuid NOT NULL DEFAULT gen_random_uuid(),
                simulator_id uuid NOT NULL,
                severity character varying(30) NOT NULL,
                fault_description text NOT NULL,
                resolution_description text NULL,
                resolved_at timestamp with time zone NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_maintenance_logs PRIMARY KEY (maintenance_log_id),
                CONSTRAINT fk_maintenance_logs_simulators_simulator_id FOREIGN KEY (simulator_id)
                    REFERENCES maint.simulators(simulator_id)
                    ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_maintenance_logs_simulator_id
                ON maint.maintenance_logs (simulator_id);

            CREATE INDEX IF NOT EXISTS idx_maintenance_logs_resolved_at
                ON maint.maintenance_logs (resolved_at);
        ");
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Asset schema bootstrap failed on startup.");
    }

    try
    {
        await LionSimPlannerSeeder.SeedAsync(hrDb, maintDb, schedDb, log);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Database seeding failed on startup.");
    }
}

// Ensure web server binds dynamically to PORT env var (required by Render/Railway)
var port = Environment.GetEnvironmentVariable("PORT");
if (port is not null)
    app.Run($"http://0.0.0.0:{port}");
else
    app.Run();

#region Migration Helper Methods
static async Task EnsureMigrationHistorySnakeCaseAsync(Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade db, string schema)
{
    await db.ExecuteSqlRawAsync($@"
        CREATE SCHEMA IF NOT EXISTS {schema};

        CREATE TABLE IF NOT EXISTS {schema}.__efmigrations (
            migration_id character varying(150) NOT NULL,
            product_version character varying(32) NOT NULL,
            CONSTRAINT pk___efmigrations PRIMARY KEY (migration_id)
        );

        DO $$
        BEGIN
            IF EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_schema = '{schema}'
                  AND table_name = '__efmigrations'
                  AND column_name = 'MigrationId'
            ) AND NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_schema = '{schema}'
                  AND table_name = '__efmigrations'
                  AND column_name = 'migration_id'
            ) THEN
                ALTER TABLE {schema}.__efmigrations RENAME COLUMN ""MigrationId"" TO migration_id;
            END IF;

            IF EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_schema = '{schema}'
                  AND table_name = '__efmigrations'
                  AND column_name = 'ProductVersion'
            ) AND NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_schema = '{schema}'
                  AND table_name = '__efmigrations'
                  AND column_name = 'product_version'
            ) THEN
                ALTER TABLE {schema}.__efmigrations RENAME COLUMN ""ProductVersion"" TO product_version;
            END IF;
        END$$;
    ");
}

static async Task EnsureAssetMigrationHistoryBaselineAsync(AssetDbContext db)
{
    await db.Database.ExecuteSqlRawAsync(@"
        CREATE SCHEMA IF NOT EXISTS maint;

        CREATE TABLE IF NOT EXISTS maint.__efmigrations (
            migration_id character varying(150) NOT NULL,
            product_version character varying(32) NOT NULL,
            CONSTRAINT pk___efmigrations PRIMARY KEY (migration_id)
        );

        INSERT INTO maint.__efmigrations (migration_id, product_version)
        SELECT '20260714102801_InitialCreate', '9.0.4'
        WHERE EXISTS (
            SELECT 1
            FROM information_schema.tables
            WHERE table_schema = 'maint' AND table_name = 'engineers'
        )
        ON CONFLICT (migration_id) DO NOTHING;

        INSERT INTO maint.__efmigrations (migration_id, product_version)
        SELECT '20260719164844_AddMaintenanceLifecycleEntities', '9.0.4'
        WHERE EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'maint'
              AND table_name = 'engineers'
              AND column_name = 'checkout_time'
        )
          AND EXISTS (
            SELECT 1
            FROM information_schema.tables
            WHERE table_schema = 'maint' AND table_name = 'maintenance_logs'
        )
        ON CONFLICT (migration_id) DO NOTHING;
    ");
}
#endregion