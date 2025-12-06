using System.Text;
using Koinon.Api.Middleware;
using Koinon.Api.Services;
using Koinon.Application.Extensions;
using Koinon.Application.Interfaces;
using Koinon.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

// Configure PostgreSQL DbContext
builder.Services.AddDbContext<KoinonDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString, npgsqlOptions =>
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

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT Secret not configured");
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.FromSeconds(60) // 60 second tolerance for clock drift
    };
});

// Add application services
builder.Services.AddKoinonApplicationServices();

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

app.UseHttpsRedirection();

// Authentication must come before authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
