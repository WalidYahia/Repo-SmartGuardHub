using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using SmartGuardHub.Configuration;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Infrastructure;
using SmartGuardHub.Protocols;
using SmartGuardHub.Protocols.MQTT;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure Entity Framework with SQLite
builder.Services.AddDbContext<SmartGuardDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("MainDatabaseConnection")));

builder.Services.AddDbContext<SystemLogDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SystemLogDatabaseConnection")));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddScoped<DeviceService>();

builder.Services.AddScoped<LoggingService>();

builder.Services.AddScoped<DeviceCommunicationManager>();

builder.Services.AddHostedService<LogCleanupService>();

builder.Services.AddSingleton<ConfigurationService>();


// HTTP Client for REST protocol
builder.Services.AddHttpClient<RestProtocol>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Protocols
builder.Services.AddScoped<IDeviceProtocol, RestProtocol>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<ISystemDevice, SonOffMiniR>();
builder.Services.AddScoped<IAsyncInitializer, DeviceService>();

builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();

builder.Services.AddMqttService(); // Add this line

// Logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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

// Call async initialization before app starts handling requests
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IAsyncInitializer>();
    await initializer.InitializeAsync();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
