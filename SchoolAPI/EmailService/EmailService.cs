using System.Net;
using System.Net.Mail;

namespace SchoolAPI.Services;

public class EmailService
{
    private SmtpClient BuildSmtp()
    {
        var fromEmail   = Environment.GetEnvironmentVariable("EMAIL_FROM")!;
        var appPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD")!;
        return new SmtpClient("smtp.gmail.com")
        {
            Port                  = 587,
            Credentials           = new NetworkCredential(fromEmail, appPassword),
            EnableSsl             = true,
            DeliveryMethod        = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
        };
    }

    // ── Verification email ────────────────────────────────────
    public async Task SendVerificationEmailAsync(string toEmail, string toName, string verifyUrl)
    {
        var fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM")!;
        var mail = new MailMessage
        {
            From       = new MailAddress(fromEmail, "Bhanudaya Secondary School"),
            Subject    = "Verify your Bhanudaya School account",
            IsBodyHtml = true,
            Body       = $@"
<!DOCTYPE html><html><head><style>
  body{{font-family:Arial,sans-serif;background:#f4f4f4;}}
  .wrapper{{max-width:520px;margin:40px auto;background:#fff;border-radius:12px;overflow:hidden;}}
  .header{{background:linear-gradient(135deg,#1e3a5f,#2563eb);padding:32px;text-align:center;}}
  .header h1{{color:#fff;margin:0;font-size:22px;}}
  .body{{padding:32px;}}
  .btn{{display:inline-block;padding:14px 32px;background:#2563eb;color:#fff;border-radius:8px;text-decoration:none;font-weight:bold;}}
  .footer{{text-align:center;padding:20px;color:#aaa;font-size:12px;}}
</style></head><body>
  <div class='wrapper'>
    <div class='header'><h1>🎓 Bhanudaya Secondary School</h1></div>
    <div class='body'>
      <h2>Hello {toName}! 👋</h2>
      <p>Please verify your email to activate your account.</p>
      <a href='{verifyUrl}' class='btn'>✅ Verify My Email</a>
      <p style='color:#999;font-size:13px;margin-top:20px;'>Link expires in <strong>24 hours</strong>.</p>
    </div>
    <div class='footer'>© Bhanudaya Secondary School · Built by Abhi Parajuli</div>
  </div>
</body></html>"
        };
        mail.To.Add(new MailAddress(toEmail, toName));
        await BuildSmtp().SendMailAsync(mail);
    }

    // ── OTP email for password reset ──────────────────────────
    public async Task SendOtpEmailAsync(string toEmail, string toName, string otp)
    {
        var fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM")!;
        var mail = new MailMessage
        {
            From       = new MailAddress(fromEmail, "Bhanudaya Secondary School"),
            Subject    = "Your password reset OTP",
            IsBodyHtml = true,
            Body       = $@"
<!DOCTYPE html><html><head><style>
  body{{font-family:Arial,sans-serif;background:#f4f4f4;}}
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
      <p style='color:#94a3b8;font-size:13px;'>This code expires in <strong>10 minutes</strong>.<br>Do not share it with anyone.</p>
    </div>
    <div class='footer'>© Bhanudaya Secondary School · Built by Abhi Parajuli</div>
  </div>
</body></html>"
        };
        mail.To.Add(new MailAddress(toEmail, toName));
        await BuildSmtp().SendMailAsync(mail);
    }
}