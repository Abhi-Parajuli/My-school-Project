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
    private readonly AppDbContext  _db;
    private readonly TokenService  _tokenService;
    private readonly EmailService  _emailService;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, TokenService tokenService,
                          EmailService emailService, IConfiguration config)
    {
        _db           = db;
        _tokenService = tokenService;
        _emailService = emailService;
        _config       = config;
    }

    // ── POST /api/auth/register ───────────────────────────────────────────────
    // Creates account but does NOT log in — sends verification email first
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new AuthResponse { Success = false, Message = "Email and password are required." });

        // Check email already exists
        bool exists = await _db.Users.AnyAsync(u => u.Email == req.Email);
        if (exists)
            return BadRequest(new AuthResponse { Success = false, Message = "Email Address Already Exists!" });

        // Create user — not verified yet
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

        // Generate a unique verification token
        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"); // 64 chars

        _db.EmailVerifications.Add(new EmailVerification
        {
            UserId    = user.Id,
            Token     = token,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed    = false
        });
        await _db.SaveChangesAsync();

        // Build the verification URL — points back to this API
        var verifyUrl = $"http://localhost:5165/api/auth/verify?token={token}";

        // Send the email
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
            // Don't expose SMTP errors to the user — log and return friendly message
            Console.WriteLine($"[Email Error] {ex.Message}");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "Account created but we could not send the verification email. Check your Gmail App Password in appsettings.json."
            });
        }

        return Ok(new AuthResponse
        {
            Success = true,
            Message = $"Account created! A verification email has been sent to {user.Email}. Please check your inbox and click the link to activate your account."
        });
    }

    // ── GET /api/auth/verify?token=xxx ────────────────────────────────────────
    // User clicks the link in their email — this activates their account
    [HttpGet("verify")]
    public async Task<ContentResult> VerifyEmail([FromQuery] string token)
    {
        var verification = await _db.EmailVerifications
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Token == token && !e.IsUsed);

        // Failure page
        string html;
        if (verification == null || verification.ExpiresAt < DateTime.UtcNow)
        {
            html = VerifyPage(
                success: false,
                title:   "Link Expired or Invalid",
                message: "This verification link is invalid or has expired. Please register again."
            );
            return Content(html, "text/html");
        }

        // Mark token as used and user as verified
        verification.IsUsed       = true;
        verification.User!.IsVerified = true;
        await _db.SaveChangesAsync();

        html = VerifyPage(
            success: true,
            title:   "Email Verified! 🎉",
            message: $"Hello {verification.User.FirstName}, your account is now active. You can close this tab and log in."
        );
        return Content(html, "text/html");
    }

    // ── POST /api/auth/signin ─────────────────────────────────────────────────
    [HttpPost("signin")]
    public async Task<ActionResult<AuthResponse>> SignIn([FromBody] SignInRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.Password))
            return Unauthorized(new AuthResponse { Success = false, Message = "Incorrect Email or Password." });

        // Block login if email not verified
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

    // ── POST /api/auth/google ─────────────────────────────────────────────────
    // Google already verified the email — no extra verification needed
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> GoogleSignIn([FromBody] GoogleSignInRequest req)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _config["Google:ClientId"] }
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(req.Credential, settings);
        }
        catch
        {
            return Unauthorized(new AuthResponse { Success = false, Message = "Invalid Google token." });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

        if (user == null)
        {
            // Auto-register Google users — already verified by Google
            user = new User
            {
                FirstName  = payload.GivenName  ?? "Google",
                LastName   = payload.FamilyName ?? "User",
                Email      = payload.Email,
                Password   = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                IsVerified = true   // Google verified — skip email step
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        else if (!user.IsVerified)
        {
            // If they signed up manually before but never verified,
            // Google login verifies them now
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

    // ── Verification result HTML page ─────────────────────────────────────────
    private static string VerifyPage(bool success, string title, string message) => $@"
<!DOCTYPE html><html><head><meta charset='UTF-8'>
<title>{title}</title>
<style>
  body {{ font-family: Arial, sans-serif; background: #f4f4f4; display: flex; align-items: center; justify-content: center; min-height: 100vh; margin: 0; }}
  .card {{ background: #fff; border-radius: 14px; padding: 48px 40px; text-align: center; box-shadow: 0 8px 30px rgba(0,0,0,0.1); max-width: 440px; width: 90%; }}
  .icon {{ font-size: 56px; margin-bottom: 16px; }}
  h1 {{ color: {(success ? "#15803d" : "#dc2626")}; font-size: 22px; margin: 0 0 12px; }}
  p {{ color: #555; font-size: 15px; line-height: 1.6; }}
  a {{ display: inline-block; margin-top: 24px; padding: 12px 28px; background: #2563eb; color: #fff; border-radius: 8px; text-decoration: none; font-weight: bold; }}
</style></head>
<body>
  <div class='card'>
    <div class='icon'>{(success ? "✅" : "❌")}</div>
    <h1>{title}</h1>
    <p>{message}</p>
    <a href='http://127.0.0.1:5500/contact.html'>Go to Login</a>
  </div>
</body></html>";
}
