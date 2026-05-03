using System.Net;
using System.Net.Mail;

namespace SchoolAPI.Services;

public class EmailService
{
    public async Task SendVerificationEmailAsync(string toEmail, string toName, string verifyUrl)
    {
        var fromEmail   = Environment.GetEnvironmentVariable("EMAIL_FROM")!;
        var appPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD")!;

        var smtp = new SmtpClient("smtp.gmail.com")
        {
            Port                  = 587,
            Credentials           = new NetworkCredential(fromEmail, appPassword),
            EnableSsl             = true,
            DeliveryMethod        = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
        };

        var mail = new MailMessage
        {
            From       = new MailAddress(fromEmail, "Bhanudaya Secondary School"),
            Subject    = "Verify your Bhanudaya School account",
            IsBodyHtml = true,
            Body       = $@"
<!DOCTYPE html><html><head><style>
  body {{ font-family: Arial, sans-serif; background: #f4f4f4; }}
  .wrapper {{ max-width:520px; margin:40px auto; background:#fff; border-radius:12px; overflow:hidden; }}
  .header {{ background:linear-gradient(135deg,#1e3a5f,#2563eb); padding:32px; text-align:center; }}
  .header h1 {{ color:#fff; margin:0; font-size:22px; }}
  .body {{ padding:32px; }}
  .btn {{ display:inline-block; padding:14px 32px; background:#2563eb; color:#fff;
          border-radius:8px; text-decoration:none; font-weight:bold; }}
  .footer {{ text-align:center; padding:20px; color:#aaa; font-size:12px; }}
</style></head><body>
  <div class='wrapper'>
    <div class='header'><h1>🎓 Bhanudaya Secondary School</h1></div>
    <div class='body'>
      <h2>Hello {toName}! 👋</h2>
      <p>Please verify your email to activate your account.</p>
      <a href='{verifyUrl}' class='btn'>✅ Verify My Email</a>
      <p style='color:#999;font-size:13px;margin-top:20px;'>
        This link expires in <strong>24 hours</strong>.
      </p>
    </div>
    <div class='footer'>© Bhanudaya Secondary School · Built by Abhi Parajuli</div>
  </div>
</body></html>"
        };

        mail.To.Add(new MailAddress(toEmail, toName));
        await smtp.SendMailAsync(mail);
    }
}