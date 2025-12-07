using System.Runtime.Versioning;
using Koinon.PrintBridge.Endpoints;
using Koinon.PrintBridge.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var portConfig = builder.Configuration.GetValue<int?>("PrintBridge:Port");
var port = portConfig ?? 9632;

// Register services
builder.Services
    .AddSingleton<PrinterDiscoveryService>()
    .AddSingleton<ZplPrintService>();

// Configure Kestrel to listen only on localhost
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(port);
});

// Add CORS for kiosk access
builder.Services.AddCors(options =>
{
    options.AddPolicy("KioskOrigins", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS
app.UseCors("KioskOrigins");

// Configure endpoints
PrintEndpoints.MapEndpoints(app);

app.Logger.LogInformation("Koinon PrintBridge starting on http://127.0.0.1:{Port}", port);

await app.RunAsync();
