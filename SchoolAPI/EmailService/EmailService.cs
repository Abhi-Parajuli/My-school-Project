using System.Text.Json;

namespace SchoolAPI.Services;

public class EmailService
{
    private readonly HttpClient _http;

    // Elastic Email free tier: 100 emails/day, no SMTP needed
    // API docs: https://elasticemail.com/developers/api-documentation
    private const string ApiEndpoint = "https://api.elasticemail.com/v2/email/send";

    public EmailService()
    {
        _http = new HttpClient();
    }

    // ── Core send method (Elastic Email v2 form-encoded API) ──
    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var apiKey    = Environment.GetEnvironmentVariable("ELASTIC_EMAIL_API_KEY")
                        ?? throw new Exception("ELASTIC_EMAIL_API_KEY is not set!");
        var fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM")
                        ?? throw new Exception("EMAIL_FROM is not set!");
        var fromName  = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME")
                        ?? "Bhanudaya Secondary School";

        // Elastic Email v2 uses application/x-www-form-urlencoded
        var fields = new Dictionary<string, string>
        {
            ["apikey"]          = apiKey,
            ["from"]            = fromEmail,
            ["fromName"]        = fromName,
            ["to"]              = $"{toName} <{toEmail}>",
            ["subject"]         = subject,
            ["bodyHtml"]        = htmlBody,
            ["isTransactional"] = "true"  // bypasses unsubscribe list — required for auth emails
        };

        using var content  = new FormUrlEncodedContent(fields);
        using var response = await _http.PostAsync(ApiEndpoint, content);
        var body = await response.Content.ReadAsStringAsync();

        // Elastic Email returns JSON: { "success": true } or { "success": false, "error": "..." }
        using var doc = JsonDocument.Parse(body);
        var success = doc.RootElement.GetProperty("success").GetBoolean();
        if (!success)
        {
            var error = doc.RootElement.TryGetProperty("error", out var e) ? e.GetString() : "Unknown error";
            throw new Exception($"Elastic Email API error: {error}");
        }
    }

    // ── Verification email ────────────────────────────────────
    public async Task SendVerificationEmailAsync(string toEmail, string toName, string verifyUrl)
    {
        var html = $@"
<!DOCTYPE html><html><head><style>
  body{{font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:0;}}
  .wrapper{{max-width:520px;margin:40px auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);}}
  .header{{background:linear-gradient(135deg,#1e3a5f,#2563eb);padding:32px;text-align:center;}}
  .header h1{{color:#fff;margin:0;font-size:22px;}}
  .body{{padding:36px 32px;}}
  .body h2{{color:#0f1923;margin:0 0 12px;}}
  .body p{{color:#4a5568;font-size:15px;line-height:1.6;}}
  .btn{{display:inline-block;margin-top:24px;padding:14px 32px;
        background:linear-gradient(135deg,#2563eb,#1e3a5f);
        color:#fff;border-radius:8px;text-decoration:none;font-weight:bold;font-size:15px;}}
  .note{{color:#94a3b8;font-size:13px;margin-top:20px;}}
  .footer{{text-align:center;padding:20px;color:#aaa;font-size:12px;border-top:1px solid #f0f0f0;}}
</style></head><body>
  <div class='wrapper'>
    <div class='header'><h1>🎓 Bhanudaya Secondary School</h1></div>
    <div class='body'>
      <h2>Hello {toName}! 👋</h2>
      <p>Thank you for registering. Click the button below to verify your email and activate your account.</p>
      <a href='{verifyUrl}' class='btn'>✅ Verify My Email</a>
      <p class='note'>This link expires in <strong>24 hours</strong>. If you did not create an account, ignore this email.</p>
    </div>
    <div class='footer'>© Bhanudaya Secondary School · Built by Abhi Parajuli</div>
  </div>
</body></html>";

        await SendAsync(toEmail, toName, "Verify your Bhanudaya School account", html);
    }

    // ── OTP / password-reset email ────────────────────────────
    public async Task SendOtpEmailAsync(string toEmail, string toName, string otp)
    {
        var html = $@"
<!DOCTYPE html><html><head><style>
  body{{font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:0;}}
  .wrapper{{max-width:480px;margin:40px auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);}}
  .header{{background:linear-gradient(135deg,#1e3a5f,#2563eb);padding:28px;text-align:center;}}
  .header h1{{color:#fff;margin:0;font-size:20px;}}
  .body{{padding:36px 32px;text-align:center;}}
  .body p{{color:#4a5568;font-size:15px;line-height:1.6;}}
  .otp{{font-size:44px;font-weight:900;letter-spacing:14px;color:#0f1923;
        background:#f8f6f0;border-radius:12px;padding:20px 32px;
        display:inline-block;margin:20px 0;border:2px dashed #c9a84c;}}
  .warning{{color:#94a3b8;font-size:13px;margin-top:16px;}}
  .footer{{text-align:center;padding:20px;color:#aaa;font-size:12px;border-top:1px solid #f0f0f0;}}
</style></head><body>
  <div class='wrapper'>
    <div class='header'><h1>🔐 Password Reset</h1></div>
    <div class='body'>
      <p>Hello <strong>{toName}</strong>,</p>
      <p>Your 6-digit OTP code for password reset is:</p>
      <div class='otp'>{otp}</div>
      <p class='warning'>
        Expires in <strong>10 minutes</strong>.<br>
        Do not share it with anyone.
      </p>
    </div>
    <div class='footer'>© Bhanudaya Secondary School · Built by Abhi Parajuli</div>
  </div>
</body></html>";

        await SendAsync(toEmail, toName, "Your password reset OTP — Bhanudaya School", html);
    }
}