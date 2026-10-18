using EHRPlatform.Services.Patient.Persistence;
using EHRPlatform.Services.Patient.Application.Services;
using EHRPlatform.Services.Patient.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MediatR;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database — connection string from env / User Secrets / Key Vault (never commit secrets)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not configured. " +
        "Set via environment, User Secrets, or Key Vault.");
}
builder.Services.AddDbContext<PatientDbContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("Patient.Persistence")));
builder.Services.AddScoped<IPatientDbContext>(sp => sp.GetRequiredService<PatientDbContext>());

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    Assembly.Load("Patient.Application")));

// Infrastructure Services
builder.Services.AddScoped<IMrnGenerationService, MrnGenerationService>();
builder.Services.AddScoped<IPatientCacheService, PatientCacheService>();

// Elasticsearch
var elasticsearchUri = builder.Configuration["Elasticsearch:Uri"] ?? "http://localhost:9200";
builder.Services.AddSingleton(sp =>
    new Elastic.Clients.Elasticsearch.ElasticsearchClient(new Uri(elasticsearchUri)));
builder.Services.AddScoped<IElasticsearchService, ElasticsearchService>();

// Redis Cache
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
});

// JWT Authentication — fail fast outside Development if signing material is missing
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtKeyPath = builder.Configuration["Jwt:PublicKeyPath"];
var jwtSecret = builder.Configuration["Jwt:Secret"];
var isDev = builder.Environment.IsDevelopment();

var hasRsaKey = !string.IsNullOrWhiteSpace(jwtKeyPath) && File.Exists(jwtKeyPath);
var hasHsSecret = !string.IsNullOrWhiteSpace(jwtSecret) && jwtSecret.Length >= 32;

if (!hasRsaKey && !hasHsSecret)
{
    if (!isDev)
    {
        throw new InvalidOperationException(
            "JWT validation material is required in non-Development environments. " +
            "Provide Jwt:PublicKeyPath (PEM) or Jwt:Secret (HS256, >= 32 chars) via env/Key Vault.");
    }
    // Development: log warning but still configure a disposable key so the app can start locally
    Console.WriteLine("WARNING: No JWT key configured. Using ephemeral development key. Do not use in production.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true
        };

        if (hasRsaKey)
        {
            var publicKey = File.ReadAllText(jwtKeyPath!);
            var rsa = System.Security.Cryptography.RSA.Create();
            rsa.ImportFromPem(publicKey.ToCharArray());
            parameters.IssuerSigningKey = new RsaSecurityKey(rsa);
        }
        else if (hasHsSecret)
        {
            parameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret!));
        }
        else
        {
            // Ephemeral key for pure local Development only
            var ephemeral = Encoding.UTF8.GetBytes("dev-only-ephemeral-key-do-not-use-in-prod!!");
            parameters.IssuerSigningKey = new SymmetricSecurityKey(ephemeral);
        }

        options.TokenValidationParameters = parameters;
    });

// Authorization
builder.Services.AddAuthorization();

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PatientDbContext>();

// CORS — config-driven allowlist (never AllowAnyOrigin in production posture)
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Database migrations — only when explicitly enabled (prefer CI/Helm job in multi-replica)
var runMigrations = string.Equals(
    Environment.GetEnvironmentVariable("RUN_MIGRATIONS"),
    "true",
    StringComparison.OrdinalIgnoreCase)
    || (isDev && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RUN_MIGRATIONS")));

if (runMigrations)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PatientDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!isDev && builder.Configuration.GetValue("Security:RequireHttps", true))
{
    app.UseHttpsRedirection();
}
else if (isDev)
{
    // optional in Development
    app.UseHttpsRedirection();
}

app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
