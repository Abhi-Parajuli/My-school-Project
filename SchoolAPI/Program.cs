using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolAPI.Data;
using SchoolAPI.Services;

Environment.SetEnvironmentVariable("DOTNET_GCConserveMemory", "9");
Environment.SetEnvironmentVariable("DOTNET_GCHeapHardLimit", "400000000");

var builder = WebApplication.CreateBuilder(args);

// ── Port: always read from Render's PORT env var ──────────────────────────────
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
Console.WriteLine($"Starting on port {port}");

// ── Database ──────────────────────────────────────────────────────────────────
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// ── JWT ───────────────────────────────────────────────────────────────────────
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "fallback-dev-key-change-in-production";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = false,
            ValidateAudience         = false,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── Auto-create DB tables ─────────────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    Console.WriteLine("Database connected and tables ready");
}
catch (Exception ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
}

// ── Middleware — order matters ────────────────────────────────────────────────
app.UseCors("AllowFrontend");          // CORS first
app.UseDefaultFiles();                 // serves index.html at /
app.UseStaticFiles();                  // serves wwwroot/ — html, css, js, photo
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();                  // API routes: /api/auth/..., /api/comments

app.Run();