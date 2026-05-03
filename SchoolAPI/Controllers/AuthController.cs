using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAPI.Data;
using SchoolAPI.Models;
using SchoolAPI.Services;

namespace SchoolAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;
    private readonly EmailService _emailService;

    // ✅ Removed IConfiguration — using env variables instead
    public AuthController(AppDbContext db, TokenService tokenService, EmailService emailService)
    {
        _db           = db;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    // ── POST /api/auth/register ───────────────────────────────
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new AuthResponse { Success = false, Message = "Email and password are required." });

        bool exists = await _db.Users.AnyAsync(u => u.Email == req.Email);
        if (exists)
            return BadRequest(new AuthResponse { Success = false, Message = "Email Address Already Exists!" });

        var user = new User
        {
            FirstName  = req.FName,
            LastName   = req.LName,
            Email      = req.Email,
            Password   = BCrypt.Net.BCrypt.HashPassword(req.Password),
            IsVerified = false
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

        _db.EmailVerifications.Add(new EmailVerification
        {
            UserId    = user.Id,
            Token     = token,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed    = false
        });
        await _db.SaveChangesAsync();

        // ✅ Fix 1: Use real Render URL instead of localhost
        var apiUrl     = Environment.GetEnvironmentVariable("API_URL") 
                         ?? "http://localhost:5165";
        var verifyUrl  = $"{apiUrl}/api/auth/verify?token={token}";

        try
        {
            await _emailService.SendVerificationEmailAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}",
                verifyUrl
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Email Error] {ex.Message}");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "Account created but verification email failed. Please contact support."
            });
        }

        return Ok(new AuthResponse
        {
            Success = true,
            Message = $"Account created! A verification email has been sent to {user.Email}."
        });
    }

    // ── GET /api/auth/verify?token=xxx ────────────────────────
    [HttpGet("verify")]
    public async Task<ContentResult> VerifyEmail([FromQuery] string token)
    {
        var verification = await _db.EmailVerifications
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Token == token && !e.IsUsed);

        string html;
        if (verification == null || verification.ExpiresAt < DateTime.UtcNow)
        {
            html = VerifyPage(false, "Link Expired or Invalid",
                "This verification link is invalid or has expired. Please register again.");
            return Content(html, "text/html");
        }

        verification.IsUsed           = true;
        verification.User!.IsVerified = true;
        await _db.SaveChangesAsync();

        html = VerifyPage(true, "Email Verified! 🎉",
            $"Hello {verification.User.FirstName}, your account is now active. You can close this tab and log in.");
        return Content(html, "text/html");
    }

    // ── POST /api/auth/signin ─────────────────────────────────
    [HttpPost("signin")]
    public async Task<ActionResult<AuthResponse>> SignIn([FromBody] SignInRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.Password))
            return Unauthorized(new AuthResponse { Success = false, Message = "Incorrect Email or Password." });

        if (!user.IsVerified)
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Message = "Please verify your email first. Check your inbox for the verification link."
            });

        return Ok(new AuthResponse
        {
            Success  = true,
            Token    = _tokenService.GenerateToken(user),
            FullName = $"{user.FirstName} {user.LastName}",
            Email    = user.Email,
            Message  = "Login successful!"
        });
    }

    // ── POST /api/auth/google ─────────────────────────────────
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> GoogleSignIn([FromBody] GoogleSignInRequest req)
    {
        // ✅ Fix 2: Read Google Client ID from environment variable
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(req.Credential, settings);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Google Auth Error] {ex.Message}");
            return Unauthorized(new AuthResponse { Success = false, Message = "Invalid Google token." });
        }

        // ✅ Fix 3: Wrap DB operations in try/catch so errors return JSON
        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

            if (user == null)
            {
                user = new User
                {
                    FirstName  = payload.GivenName  ?? "Google",
                    LastName   = payload.FamilyName ?? "User",
                    Email      = payload.Email,
                    Password   = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    IsVerified = true
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }
            else if (!user.IsVerified)
            {
                user.IsVerified = true;
                await _db.SaveChangesAsync();
            }

            return Ok(new AuthResponse
            {
                Success  = true,
                Token    = _tokenService.GenerateToken(user),
                FullName = $"{user.FirstName} {user.LastName}",
                Email    = user.Email,
                Message  = "Google login successful!"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Google DB Error] {ex.Message}");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "Login failed. Please try again."
            });
        }
    }

    // ── Verification HTML page ────────────────────────────────
    private static string VerifyPage(bool success, string title, string message)
    {
        // ✅ Fix 4: Use Netlify URL instead of localhost
        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                          ?? "http://127.0.0.1:5500";

        return $@"
<!DOCTYPE html><html><head><meta charset='UTF-8'>
<title>{title}</title>
<style>
  body {{ font-family: Arial, sans-serif; background: #f4f4f4; display: flex;
          align-items: center; justify-content: center; min-height: 100vh; margin: 0; }}
  .card {{ background: #fff; border-radius: 14px; padding: 48px 40px; text-align: center;
           box-shadow: 0 8px 30px rgba(0,0,0,0.1); max-width: 440px; width: 90%; }}
  .icon {{ font-size: 56px; margin-bottom: 16px; }}
  h1 {{ color: {(success ? "#15803d" : "#dc2626")}; font-size: 22px; margin: 0 0 12px; }}
  p {{ color: #555; font-size: 15px; line-height: 1.6; }}
  a {{ display: inline-block; margin-top: 24px; padding: 12px 28px; background: #2563eb;
       color: #fff; border-radius: 8px; text-decoration: none; font-weight: bold; }}
</style></head>
<body>
  <div class='card'>
    <div class='icon'>{(success ? "✅" : "❌")}</div>
    <h1>{title}</h1>
    <p>{message}</p>
    <a href='{frontendUrl}/contact.html'>Go to Login</a>
  </div>
</body></html>";
    }
}