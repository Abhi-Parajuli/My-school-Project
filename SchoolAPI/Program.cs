using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolAPI.Data;
using SchoolAPI.Services;

Environment.SetEnvironmentVariable("DOTNET_GCConserveMemory", "9");
Environment.SetEnvironmentVariable("DOTNET_GCHeapHardLimit", "400000000");

var builder = WebApplication.CreateBuilder(args);

// ── Port ──────────────────────────────────────────────────────
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ── Database ──────────────────────────────────────────────────
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
if (string.IsNullOrEmpty(connectionString))
    throw new Exception("❌ CONNECTION_STRING is not set!");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// ── JWT ───────────────────────────────────────────────────────
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET");
if (string.IsNullOrEmpty(jwtKey))
    throw new Exception("❌ JWT_SECRET is not set!");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = false,
            ValidateAudience         = false,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// ── CORS ──────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── Reset and recreate DB tables with new column names ────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureDeleted();  // ← drops old tables with wrong column names
    db.Database.EnsureCreated(); // ← recreates with correct lowercase names
    Console.WriteLine("✅ Database reset and tables ready");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Database error: {ex.Message}");
}

app.UseCors("AllowFrontend");
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine($"🚀 App running on port {port}");

app.Run();