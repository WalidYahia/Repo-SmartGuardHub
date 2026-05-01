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
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<SystemLogDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("SystemLogDatabaseConnection")));
}
else
{
    var systemLogDatabasePath = builder.Configuration.GetConnectionString("SystemLogDatabasePath");
    builder.Services.AddDbContext<SystemLogDbContext>(options =>
        options.UseSqlite($"Data Source={Path.Combine(AppContext.BaseDirectory, systemLogDatabasePath)}"));
}

builder.Services.AddSingleton<IUserScenarioRepository, JsonUserScenarioRepository>();
builder.Services.AddSingleton<ISensorConfigRepository, JsonSensorConfigRepository>();

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

// HTTP Client for local device REST protocol (Sonoff units)
builder.Services.AddHttpClient<IDeviceProtocol, RestProtocol>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// HTTP Client for SyncroCloud API communication
builder.Services.AddHttpClient<ISyncroCloudService, SyncroCloudService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["SyncroCloud:BaseUrl"] ?? "http://localhost:5001/");
    client.Timeout = TimeSpan.FromSeconds(30);
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


builder.Services.AddScoped<ISystemSensor, SonOffMiniR3Switch>();
builder.Services.AddSingleton<ISensorUnitDefinitionRepository, JsonSensorUnitDefinitionRepository>();
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
