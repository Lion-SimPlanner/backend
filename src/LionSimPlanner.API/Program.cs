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
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Quartz;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeJsonConverter());
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
        []
    }});
});

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

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://localhost:3000"];

builder.Services.AddCors(opts =>
    opts.AddPolicy("Frontend", p => p
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

builder.Services.AddSignalR();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

builder.Services.AddDbContext<PersonnelDbContext>(o =>
    o.UseNpgsql(connectionString,
        npgsql => npgsql.MigrationsHistoryTable("__efmigrations", "hr")));

builder.Services.AddDbContext<SchedulingDbContext>(o =>
    o.UseNpgsql(connectionString,
        npgsql => npgsql.MigrationsHistoryTable("__efmigrations", "sched")));

builder.Services.AddDbContext<AssetDbContext>(o =>
    o.UseNpgsql(connectionString,
        npgsql => npgsql.MigrationsHistoryTable("__efmigrations", "maint")));

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lion SimPlanner API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<SimPlannerHub>("/hub/simplanner");

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var svc = scope.ServiceProvider;
    var log = svc.GetRequiredService<ILogger<Program>>();
    var hrDb    = svc.GetRequiredService<PersonnelDbContext>();
    var schedDb = svc.GetRequiredService<SchedulingDbContext>();
    var maintDb = svc.GetRequiredService<AssetDbContext>();

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
        await LionSimPlannerSeeder.SeedAsync(hrDb, maintDb, schedDb, log);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Database seeding failed on startup.");
    }
}

app.Run();
