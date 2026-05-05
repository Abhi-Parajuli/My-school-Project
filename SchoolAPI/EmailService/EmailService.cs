using System.Net;
using System.Net.Mail;

namespace SchoolAPI.Services;

public class EmailService
{
    private SmtpClient BuildSmtp()
    {
        var fromEmail   = Environment.GetEnvironmentVariable("EMAIL_FROM");
        var appPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");

        Console.WriteLine($"[Email] EMAIL_FROM     = {(string.IsNullOrEmpty(fromEmail)   ? "❌ NOT SET" : fromEmail)}");
        Console.WriteLine($"[Email] EMAIL_PASSWORD = {(string.IsNullOrEmpty(appPassword) ? "❌ NOT SET" : $"✅ set ({appPassword!.Length} chars)")}");

        if (string.IsNullOrEmpty(fromEmail))
            throw new InvalidOperationException("EMAIL_FROM is not set.");
        if (string.IsNullOrEmpty(appPassword))
            throw new InvalidOperationException("EMAIL_PASSWORD is not set.");

        // Port 465 + SSL=true works on Render (port 587 is often blocked)
        return new SmtpClient("smtp.gmail.com")
        {
            Port                  = 465,
            Credentials           = new NetworkCredential(fromEmail, appPassword),
            EnableSsl             = true,
            DeliveryMethod        = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
        };
    }

    public async Task SendVerificationEmailAsync(string toEmail, string toName, string verifyUrl)
    {
        var fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM")!;
        Console.WriteLine($"[Email] Sending verification email to: {toEmail}");

        var mail = new MailMessage
        {
            From       = new MailAddress(fromEmail, "Bhanudaya Secondary School"),
            Subject    = "Verify your Bhanudaya School account",
            IsBodyHtml = true,
            Body       = $@"
<!DOCTYPE html><html><head><style>
  body{{font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:0;}}
  .wrapper{{max-width:520px;margin:40px auto;background:#fff;border-radius:12px;overflow:hidden;}}
  .header{{background:linear-gradient(135deg,#1e3a5f,#2563eb);padding:32px;text-align:center;}}
  .header h1{{color:#fff;margin:0;font-size:22px;}}
  .body{{padding:32px;}}
  .btn{{display:inline-block;padding:14px 32px;background:#2563eb;color:#fff;border-radius:8px;text-decoration:none;font-weight:bold;margin-top:16px;}}
  .footer{{text-align:center;padding:20px;color:#aaa;font-size:12px;}}
</style></head><body>
  <div class='wrapper'>
    <div class='header'><h1>🎓 Bhanudaya Secondary School</h1></div>
    <div class='body'>
      <h2>Hello {toName}! 👋</h2>
      <p>Please click the button below to verify your email and activate your account.</p>
      <a href='{verifyUrl}' class='btn'>✅ Verify My Email</a>
      <p style='color:#999;font-size:13px;margin-top:24px;'>Link expires in <strong>24 hours</strong>.</p>
    </div>
    <div class='footer'>© Bhanudaya Secondary School · Built by Abhi Parajuli</div>
  </div>
</body></html>"
        };
        mail.To.Add(new MailAddress(toEmail, toName));

        try
        {
            await BuildSmtp().SendMailAsync(mail);
            Console.WriteLine($"[Email] ✅ Sent to {toEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Email] ❌ FAILED: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"[Email] ❌ Inner: {ex.InnerException.Message}");
            throw;
        }
    }

    public async Task SendOtpEmailAsync(string toEmail, string toName, string otp)
    {
        var fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM")!;
        Console.WriteLine($"[Email] Sending OTP to: {toEmail}");

        var mail = new MailMessage
        {
            From       = new MailAddress(fromEmail, "Bhanudaya Secondary School"),
            Subject    = "Your password reset OTP — Bhanudaya School",
            IsBodyHtml = true,
            Body       = $@"
<!DOCTYPE html><html><head><style>
  body{{font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:0;}}
  .wrapper{{max-width:480px;margin:40px auto;background:#fff;border-radius:12px;overflow:hidden;}}
  .header{{background:linear-gradient(135deg,#1e3a5f,#2563eb);padding:28px;text-align:center;}}
  .header h1{{color:#fff;margin:0;font-size:20px;}}
  .body{{padding:32px;text-align:center;}}
  .otp{{font-size:42px;font-weight:900;letter-spacing:12px;color:#0f1923;
        background:#f8f6f0;border-radius:12px;padding:20px 32px;
        display:inline-block;margin:20px 0;border:2px dashed #e8e4dc;}}
  .footer{{text-align:center;padding:20px;color:#aaa;font-size:12px;}}
</style></head><body>
  <div class='wrapper'>
    <div class='header'><h1>🔐 Password Reset</h1></div>
    <div class='body'>
      <p style='font-size:16px;color:#4a5568;'>Hello {toName},</p>
      <p style='color:#4a5568;'>Your 6-digit OTP code is:</p>
      <div class='otp'>{otp}</div>
      <p style='color:#94a3b8;font-size:13px;'>Expires in <strong>10 minutes</strong>. Do not share it.</p>
    </div>
    <div class='footer'>© Bhanudaya Secondary School · Built by Abhi Parajuli</div>
  </div>
</body></html>"
        };
        mail.To.Add(new MailAddress(toEmail, toName));

        try
        {
            await BuildSmtp().SendMailAsync(mail);
            Console.WriteLine($"[Email] ✅ OTP sent to {toEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Email] ❌ FAILED: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"[Email] ❌ Inner: {ex.InnerException.Message}");
            throw;
        }
    }
}