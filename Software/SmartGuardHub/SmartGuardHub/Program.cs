using Microsoft.EntityFrameworkCore;
using SmartGuardHub.Application;
using SmartGuardHub.Cloud;
using SmartGuardHub.Configuration;
using SmartGuardHub.Features.SensorConfiguration;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Features.UserCommands;
using SmartGuardHub.Features.UserScenarios;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Network;
using SmartGuardHub.Protocols;
using SmartGuardHub.Protocols.MQTT;

var builder = WebApplication.CreateBuilder(args);

// ── Databases ────────────────────────────────────────────────────────────────

builder.Services.AddDbContext<SystemLogDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
        options.UseSqlite(builder.Configuration.GetConnectionString("SystemLogDatabaseConnection"));
    else
    {
        var path = builder.Configuration.GetConnectionString("SystemLogDatabasePath");
        options.UseSqlite($"Data Source={Path.Combine(AppContext.BaseDirectory, path)}");
    }
});

// SmartGuardDbContext — uses DbContextFactory so Singleton repositories can share it safely
builder.Services.AddDbContextFactory<SmartGuardDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
        options.UseSqlite(builder.Configuration.GetConnectionString("MainDatabaseConnection"));
    else
    {
        var path = builder.Configuration.GetConnectionString("MainDatabasePath");
        options.UseSqlite($"Data Source={Path.Combine(AppContext.BaseDirectory, path)}");
    }
});

// ── Repositories ─────────────────────────────────────────────────────────────

builder.Services.AddSingleton<ISensorConfigRepository, DbSensorConfigRepository>();
builder.Services.AddSingleton<ISensorReadingRepository, SensorReadingRepository>();
builder.Services.AddSingleton<IUserScenarioRepository, JsonUserScenarioRepository>();

// ── App services ─────────────────────────────────────────────────────────────

builder.Services.AddScoped<DeviceService>();
builder.Services.AddScoped<LoggingService>();
builder.Services.AddScoped<DeviceCommunicationManager>();
builder.Services.AddScoped<UserCommandHandler>();
builder.Services.AddScoped<NetworkConfigurationManager>();

builder.Services.AddSingleton<ConfigurationService>();

// ── Background services ───────────────────────────────────────────────────────

builder.Services.AddHostedService<LogCleanupService>();
builder.Services.AddHostedService<SensorReadingService>();
builder.Services.AddHostedService<ConfigSyncService>();
builder.Services.AddHostedService<UserScenarioWorker>();

// ── User commands ─────────────────────────────────────────────────────────────

builder.Services.AddScoped<UserCommand, TurnOffCommand>();
builder.Services.AddScoped<UserCommand, TurnOnCommand>();
builder.Services.AddScoped<UserCommand, InchingOnCommand>();
builder.Services.AddScoped<UserCommand, InchingOffCommand>();
builder.Services.AddScoped<UserCommand, CreateDeviceCommand>();
builder.Services.AddScoped<UserCommand, RenameDeviceCommand>();
builder.Services.AddScoped<UserCommand, GetInfoCommand>();
builder.Services.AddScoped<UserCommand, LoadAllUnitsCommand>();

// ── System sensors (factory resolved by SensorType) ───────────────────────────

builder.Services.AddScoped<ISystemSensor, SonOffMiniR3Switch>();

// ── Misc ──────────────────────────────────────────────────────────────────────

builder.Services.AddSingleton<ISensorUnitDefinitionRepository, JsonSensorUnitDefinitionRepository>();
builder.Services.AddScoped<IAsyncInitializer, DeviceService>();
builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();

// ── HTTP clients ──────────────────────────────────────────────────────────────

builder.Services.AddHttpClient<IDeviceProtocol, RestProtocol>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<ISyncroCloudService, SyncroCloudService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["SyncroCloud:BaseUrl"] ?? "http://localhost:5001/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── MQTT ──────────────────────────────────────────────────────────────────────

builder.Services.AddMqttService();
builder.Services.AddSingleton<MqttMessageListener>();

// ── ASP.NET Core ──────────────────────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobileApp", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
    options.AddServerHeader = false;
});

// ── Build ─────────────────────────────────────────────────────────────────────

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await SystemManager.InitSystemEnvironment();

// Ensure database directories exist (SQLite does not create missing directories)
foreach (var dir in new[]
{
    Path.Combine(AppContext.BaseDirectory, "Database", "Production"),   // production
    "Database"                                                           // development (relative CWD)
})
{
    if (!Directory.Exists(dir))
    {
        Directory.CreateDirectory(dir);
        Console.WriteLine($"Created database folder: {dir}");
    }
}

// Migrate SystemLogDb (migration-based)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SystemLogDbContext>();
    await context.Database.MigrateAsync();
}

// Migrate SmartGuardDb.
//
// Older deployments used EnsureCreated() and have no __EFMigrationsHistory table.
// For those databases we seed the history with InitialCreate (which matches the
// schema they already have) so that MigrateAsync only applies new migrations.
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SmartGuardDbContext>>();
    await using var db = factory.CreateDbContext();

    var conn = db.Database.GetDbConnection();
    await conn.OpenAsync();

    using var tableCmd = conn.CreateCommand();
    tableCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='DeviceConfigs'";
    var tableExists = Convert.ToInt64(await tableCmd.ExecuteScalarAsync()) > 0;

    if (tableExists)
    {
        using var historyCmd = conn.CreateCommand();
        historyCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
        var historyExists = Convert.ToInt64(await historyCmd.ExecuteScalarAsync()) > 0;

        if (!historyExists)
        {
            Console.WriteLine("SmartGuardDb: seeding migration history for pre-migration database.");
            using var seedCmd = conn.CreateCommand();
            seedCmd.CommandText = @"
                CREATE TABLE __EFMigrationsHistory (
                    MigrationId TEXT NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY,
                    ProductVersion TEXT NOT NULL
                );
                INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
                VALUES ('20260101000000_InitialCreate', '9.0.0');";
            await seedCmd.ExecuteNonQueryAsync();
        }
    }

    await conn.CloseAsync();
    await db.Database.MigrateAsync();
}

var mqttService = app.Services.GetRequiredService<IMqttService>();
await mqttService.StartAsync();

using (var scope = app.Services.CreateScope())
{
    // Instantiating MqttMessageListener registers its subscriptions
    _ = scope.ServiceProvider.GetRequiredService<MqttMessageListener>();
}

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IAsyncInitializer>();
    await initializer.InitializeAsync();
}

app.UseCors("AllowMobileApp");
app.UseAuthorization();
app.MapControllers();

app.Run();
