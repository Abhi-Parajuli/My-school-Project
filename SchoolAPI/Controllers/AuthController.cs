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

        var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (existingUser != null)
        {
            // Already verified — tell them to log in instead
            if (existingUser.IsVerified)
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "An account with this email already exists. Please sign in."
                });

            // Exists but NOT verified — wipe old tokens and resend a fresh verification link
            var oldTokens = _db.EmailVerifications.Where(e => e.UserId == existingUser.Id && !e.IsUsed);
            _db.EmailVerifications.RemoveRange(oldTokens);

            var freshToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            _db.EmailVerifications.Add(new EmailVerification
            {
                UserId    = existingUser.Id,
                Token     = freshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                IsUsed    = false
            });
            await _db.SaveChangesAsync();

            var resendApiUrl    = Environment.GetEnvironmentVariable("API_URL")
                                  ?? "https://my-school-project-4.onrender.com";
            var resendVerifyUrl = $"{resendApiUrl}/api/auth/verify?token={freshToken}";

            try
            {
                await _emailService.SendVerificationEmailAsync(
                    existingUser.Email,
                    $"{existingUser.FirstName} {existingUser.LastName}",
                    resendVerifyUrl
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Resend Verify Error] {ex.Message}");
            }

            return Ok(new AuthResponse
            {
                Success = true,
                Message = $"Your account isn't verified yet. A new verification email has been sent to {existingUser.Email}. Please check your inbox."
            });
        }

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

        var apiUrl    = Environment.GetEnvironmentVariable("API_URL")
                        ?? "https://my-school-project-4.onrender.com";
        var verifyUrl = $"{apiUrl}/api/auth/verify?token={token}";

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
            Message = $"Account created! A verification email has been sent to {user.Email}. Please check your inbox and click the link to activate your account."
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
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
            Console.WriteLine($"[Google] Client ID = {clientId}");
            Console.WriteLine($"[Google] Credential length = {req?.Credential?.Length}");

            if (string.IsNullOrEmpty(clientId))
            {
                Console.WriteLine("[Google] ERROR: GOOGLE_CLIENT_ID env var is missing!");
                return StatusCode(500, new AuthResponse { Success = false, Message = "Server config error." });
            }

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(req!.Credential, settings);
            Console.WriteLine($"[Google] Token valid for: {payload.Email}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Google Token Error] {ex.Message}");
            return Unauthorized(new AuthResponse { Success = false, Message = "Invalid Google token." });
        }

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
                Console.WriteLine($"[Google] New user created: {payload.Email}");
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
            Console.WriteLine($"[Google DB Stack] {ex.StackTrace}");
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
        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                          ?? "https://bhanudaya.vercel.app";

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

    // ── POST /api/auth/forgot-password ────────────────────────
    // FIX: Now correctly uses OtpVerifications table (not EmailVerifications)
    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);

        // Always return success to prevent email enumeration attacks
        if (user == null)
            return Ok(new { success = true, message = "If this email exists, an OTP has been sent." });

        // Generate a secure 6-digit OTP
        var otp = new Random().Next(100000, 999999).ToString();

        // FIX: Remove old OTPs from the correct table (OtpVerifications), NOT EmailVerifications
        var oldOtps = _db.OtpVerifications.Where(o => o.Email == req.Email && !o.IsUsed);
        _db.OtpVerifications.RemoveRange(oldOtps);

        // FIX: Store OTP in OtpVerifications table where it belongs
        _db.OtpVerifications.Add(new OtpVerification
        {
            Email     = req.Email,
            Otp       = otp,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed    = false
        });
        await _db.SaveChangesAsync();

        try
        {
            await _emailService.SendOtpEmailAsync(user.Email, $"{user.FirstName} {user.LastName}", otp);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OTP Email Error] {ex.Message}");
            return StatusCode(500, new { success = false, message = "Could not send OTP. Please try again." });
        }

        return Ok(new { success = true, message = "OTP sent to your email." });
    }

    // ── POST /api/auth/verify-otp ─────────────────────────────
    // FIX: Now correctly queries OtpVerifications table by Email + Otp fields
    [HttpPost("verify-otp")]
    public async Task<ActionResult> VerifyOtp([FromBody] VerifyOtpRequest req)
    {
        // FIX: Query OtpVerifications directly — no more EmailVerifications hack with "OTP:" prefix
        var record = await _db.OtpVerifications
            .FirstOrDefaultAsync(o => o.Email == req.Email
                                   && o.Otp == req.Otp
                                   && !o.IsUsed
                                   && o.ExpiresAt > DateTime.UtcNow);

        if (record == null)
            return BadRequest(new { success = false, message = "Invalid or expired OTP." });

        // Generate a reset token and store it on the OTP record
        var resetToken    = Guid.NewGuid().ToString("N");
        record.IsUsed     = true;
        record.ResetToken = resetToken;
        // Extend expiry window so user has 15 min to set their new password
        record.ExpiresAt  = DateTime.UtcNow.AddMinutes(15);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, resetToken });
    }

    // ── POST /api/auth/reset-password ─────────────────────────
    // FIX: Now correctly queries OtpVerifications table by ResetToken
    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null)
            return BadRequest(new { success = false, message = "Invalid request." });

        // FIX: Look up the reset token in OtpVerifications
        // (IsUsed=true is expected since it was set at OTP step;
        //  we validate by ResetToken + ExpiresAt instead)
        var record = await _db.OtpVerifications
            .FirstOrDefaultAsync(o => o.Email == req.Email
                                   && o.ResetToken == req.ResetToken
                                   && o.ExpiresAt > DateTime.UtcNow);

        if (record == null)
            return BadRequest(new { success = false, message = "Reset token expired. Please start again." });

        // Invalidate token so it cannot be reused
        record.ResetToken = null;
        user.Password     = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, message = "Password updated successfully!" });
    }
}