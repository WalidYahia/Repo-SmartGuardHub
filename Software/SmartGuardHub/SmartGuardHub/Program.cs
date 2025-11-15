using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using SmartGuardHub.Configuration;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Features.UserCommands;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Network;
using SmartGuardHub.Protocols;
using SmartGuardHub.Protocols.MQTT;

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


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddScoped<DeviceService>();

builder.Services.AddScoped<LoggingService>();

builder.Services.AddScoped<DeviceCommunicationManager>();

builder.Services.AddHostedService<LogCleanupService>();

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
    options.ListenAnyIP(5000); // HTTP
    //options.ListenAnyIP(5001, listenOptions =>
    //{
    //    listenOptions.UseHttps(); // HTTPS if you configured a cert
    //});
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await SystemManager.InitSystemEnvironment();

// Ensure database folder exists
var dbPath = Path.Combine(AppContext.BaseDirectory, "Database", "Production");
if (!Directory.Exists(dbPath))
{
    Directory.CreateDirectory(dbPath);
    Console.WriteLine($"********************* Created missing database folder: {dbPath}");
}

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SmartGuardDbContext>();
    context.Database.Migrate(); // Apply migrations
    DatabaseSeeder.SeedData(context); // Add seed data
}

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SystemLogDbContext>();
    context.Database.Migrate(); // Apply migrations
}

// Start MQTT service
var mqttService = app.Services.GetRequiredService<IMqttService>();
await mqttService.StartAsync();

// force resolve at startup
using (var scope = app.Services.CreateScope())
{
    var handler = scope.ServiceProvider.GetRequiredService<MqttMessageListener>();
}

// Call async initialization before app starts handling requests
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IAsyncInitializer>();
    await initializer.InitializeAsync();
}

Console.WriteLine($"********************* app.UseHttpsRedirection(): {dbPath}");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
