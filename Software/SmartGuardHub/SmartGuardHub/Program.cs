using Microsoft.EntityFrameworkCore;
using SmartGuardHub.Application;
using SmartGuardHub.Configuration;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Features.UserCommands;
using SmartGuardHub.Features.UserScenarios;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Network;
using SmartGuardHub.Protocols;
using SmartGuardHub.Protocols.MQTT;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<SmartGuardDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("MainDatabaseConnection")));

    builder.Services.AddDbContext<SystemLogDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("SystemLogDatabaseConnection")));
}
else
{
    // Resolve full absolute paths for both databases
    var mainDatabasePath = builder.Configuration.GetConnectionString("MainDatabasePath");
    var systemLogDatabasePath = builder.Configuration.GetConnectionString("SystemLogDatabasePath");

    // Register contexts with corrected absolute paths
    builder.Services.AddDbContext<SmartGuardDbContext>(options =>
        options.UseSqlite($"Data Source={Path.Combine(AppContext.BaseDirectory, mainDatabasePath)}"));

    builder.Services.AddDbContext<SystemLogDbContext>(options =>
        options.UseSqlite($"Data Source={Path.Combine(AppContext.BaseDirectory, systemLogDatabasePath)}"));
}

builder.Services.AddSingleton<IUserScenarioRepository, JsonUserScenarioRepository>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS for mobile app access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobileApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


builder.Services.AddScoped<DeviceService>();

builder.Services.AddScoped<LoggingService>();

builder.Services.AddScoped<DeviceCommunicationManager>();

builder.Services.AddHostedService<LogCleanupService>();

builder.Services.AddHostedService<DevicesScanner>();

builder.Services.AddHostedService<UserScenarioWorker>();

//builder.Services.AddHostedService<ApplicationStartupService>();

builder.Services.AddSingleton<ConfigurationService>();

builder.Services.AddScoped<NetworkConfigurationManager>();

// HTTP Client for REST protocol
builder.Services.AddHttpClient<IDeviceProtocol, RestProtocol>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// user command handler
builder.Services.AddScoped<UserCommandHandler>();

// user commands
builder.Services.AddScoped<UserCommand, TurnOffCommand>();
builder.Services.AddScoped<UserCommand, TurnOnCommand>();
builder.Services.AddScoped<UserCommand, InchingOnCommand>();
builder.Services.AddScoped<UserCommand, InchingOffCommand>();
builder.Services.AddScoped<UserCommand, CreateDeviceCommand>();
builder.Services.AddScoped<UserCommand, RenameDeviceCommand>();
builder.Services.AddScoped<UserCommand, GetInfoCommand>();
builder.Services.AddScoped<UserCommand, LoadAllUnitsCommand>();


// Protocols
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<ISystemUnit, SonOffMiniR>();
builder.Services.AddScoped<IAsyncInitializer, DeviceService>();

builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();

builder.Services.AddMqttService(); // Add this line
builder.Services.AddSingleton<MqttMessageListener>(); // runs once for app lifetime


// Logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});


builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000); // HTTP - accessible from network

    options.AddServerHeader = false;
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Initialize everything BEFORE starting the app
await SystemManager.InitSystemEnvironment();

var dbPath = Path.Combine(AppContext.BaseDirectory, "Database", "Production");
if (!Directory.Exists(dbPath))
{
    Directory.CreateDirectory(dbPath);
    Console.WriteLine($"Created missing database folder: {dbPath}");
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SmartGuardDbContext>();
    await context.Database.MigrateAsync();
    DatabaseSeeder.SeedData(context);
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SystemLogDbContext>();
    await context.Database.MigrateAsync();
}

var mqttService = app.Services.GetRequiredService<IMqttService>();
await mqttService.StartAsync();

using (var scope = app.Services.CreateScope())
{
    var handler = scope.ServiceProvider.GetRequiredService<MqttMessageListener>();
}

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IAsyncInitializer>();
    await initializer.InitializeAsync();
}

// Enable CORS before other middleware
app.UseCors("AllowMobileApp");

app.UseAuthorization();

app.MapControllers();

app.Run();
