using System.Text;
using Koinon.Api.Middleware;
using Koinon.Api.Services;
using Koinon.Application.Extensions;
using Koinon.Application.Interfaces;
using Koinon.Infrastructure.Data;
using Koinon.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpContext access and user context
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, HttpContextUserContext>();

// Get connection strings (with validation)
var postgresConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("PostgreSQL connection string not configured. Set ConnectionStrings:DefaultConnection.");
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";

// Register DbContext as IApplicationDbContext for Application layer
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<KoinonDbContext>());

// Redis is registered separately from AddKoinonInfrastructure to allow
// for custom per-environment configuration (connection string, instance name)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "Koinon:";
});

// Configure JWT Authentication
// Accept either Jwt:Secret or Jwt:Key for flexibility in CI environments
var jwtSigningKey = builder.Configuration["Jwt:Secret"]?.Trim()
    ?? builder.Configuration["Jwt:Key"]?.Trim();
if (string.IsNullOrEmpty(jwtSigningKey))
{
    throw new InvalidOperationException("JWT signing key not configured. Set Jwt:Secret or Jwt:Key in configuration.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Koinon.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Koinon.Web";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
        ClockSkew = TimeSpan.FromSeconds(30) // 30 second tolerance for clock drift
    };
});

// Add infrastructure services (includes DbContext, SMS/Twilio configuration)
builder.Services.AddKoinonInfrastructure(postgresConnectionString, builder.Configuration, options =>
{
    options.UseNpgsql(postgresConnectionString, npgsqlOptions =>
    {
        npgsqlOptions.UseNetTopologySuite(); // Enable PostGIS support
        npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history");
    });

    // Enable detailed errors in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Add application services (pass configuration for options binding)
builder.Services.AddKoinonApplicationServices(builder.Configuration);

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        postgresConnectionString,
        name: "postgres",
        tags: new[] { "db", "sql", "postgres" })
    .AddRedis(
        redisConnectionString,
        name: "redis",
        tags: new[] { "cache", "redis" })
    .AddDbContextCheck<KoinonDbContext>(
        name: "dbcontext",
        tags: new[] { "db", "efcore" });

var app = builder.Build();

// Configure pipeline
// Middleware order is critical for correct request processing

// 1. Request logging - log all requests
app.UseMiddleware<RequestLoggingMiddleware>();

// 2. Global exception handler - catch and format exceptions
app.UseMiddleware<GlobalExceptionHandler>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure forwarded headers for proper client IP detection behind reverse proxies
// IMPORTANT: This must run BEFORE UseHttpsRedirection for correct scheme detection
// In production, configure KnownProxies/KnownNetworks via environment variables
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1 // Only trust the first proxy in the chain
};

// In production, only trust headers from known proxies to prevent IP spoofing
// Development mode allows all proxies for ease of testing
if (!app.Environment.IsDevelopment())
{
    // Clear default proxy/network lists - only trust explicitly configured proxies
    forwardedHeadersOptions.KnownProxies.Clear();
    forwardedHeadersOptions.KnownNetworks.Clear();

    // Read trusted proxy IPs from configuration
    var trustedProxies = app.Configuration.GetSection("ForwardedHeaders:TrustedProxies").Get<string[]>();
    if (trustedProxies != null)
    {
        foreach (var proxy in trustedProxies)
        {
            if (System.Net.IPAddress.TryParse(proxy, out var ip))
            {
                forwardedHeadersOptions.KnownProxies.Add(ip);
            }
        }
    }

    // Read trusted networks (CIDR notation) from configuration
    var trustedNetworks = app.Configuration.GetSection("ForwardedHeaders:TrustedNetworks").Get<string[]>();
    if (trustedNetworks != null)
    {
        foreach (var network in trustedNetworks)
        {
            var parts = network.Split('/');
            if (parts.Length == 2 && System.Net.IPAddress.TryParse(parts[0], out var prefix) && int.TryParse(parts[1], out var prefixLength))
            {
                forwardedHeadersOptions.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLength));
            }
        }
    }
}

app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseHttpsRedirection();

// Authentication must come before authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map health check endpoint (no authentication required)
app.MapHealthChecks("/health");

await app.RunAsync();
