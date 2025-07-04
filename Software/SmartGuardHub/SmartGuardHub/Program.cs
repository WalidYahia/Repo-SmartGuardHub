using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Protocols;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Device Management
builder.Services.AddScoped<DeviceService>();

// HTTP Client for REST protocol
builder.Services.AddHttpClient<RestProtocol>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Protocols
builder.Services.AddScoped<IDeviceProtocol, RestProtocol>();

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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
