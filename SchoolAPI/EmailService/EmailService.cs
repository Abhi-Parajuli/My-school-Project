using System.Net;
using System.Net.Mail;

namespace SchoolAPI.Services;

/// <summary>
/// Sends verification emails via Gmail SMTP.
/// Uses your Gmail App Password — not your real Gmail password.
/// </summary>
public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string toName, string verifyUrl)
    {
        var fromEmail   = _config["EMAIL_USER"]!;
        var appPassword = _config["EMAIL_PASS"]!;

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
<!DOCTYPE html>
<html>
<head>
  <meta charset='UTF-8'>
  <style>
    body {{ font-family: Arial, sans-serif; background: #f4f4f4; margin: 0; padding: 0; }}
    .wrapper {{ max-width: 520px; margin: 40px auto; background: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
    .header {{ background: linear-gradient(135deg, #1e3a5f, #2563eb); padding: 32px; text-align: center; }}
    .header h1 {{ color: #fff; margin: 0; font-size: 22px; }}
    .header p {{ color: rgba(255,255,255,0.8); margin: 6px 0 0; font-size: 13px; }}
    .body {{ padding: 32px; }}
    .body h2 {{ color: #111; font-size: 20px; margin: 0 0 12px; }}
    .body p {{ color: #555; font-size: 15px; line-height: 1.6; margin: 0 0 24px; }}
    .btn {{ display: inline-block; padding: 14px 32px; background: #2563eb; color: #fff !important; border-radius: 8px; text-decoration: none; font-weight: bold; font-size: 15px; }}
    .footer {{ text-align: center; padding: 20px; color: #aaa; font-size: 12px; border-top: 1px solid #f0f0f0; }}
    .note {{ font-size: 13px; color: #999; margin-top: 20px; }}
  </style>
</head>
<body>
  <div class='wrapper'>
    <div class='header'>
      <h1>🎓 Bhanudaya Secondary School</h1>
      <p>Dumkibas, Nawalpur, Nepal</p>
    </div>
    <div class='body'>
      <h2>Hello {toName}! 👋</h2>
      <p>Thank you for registering. Please verify your email address to activate your account and start using all features of our school website.</p>
      <a href='{verifyUrl}' class='btn'>✅ Verify My Email</a>
      <p class='note'>This link expires in <strong>24 hours</strong>. If you did not create an account, you can safely ignore this email.</p>
    </div>
    <div class='footer'>
      © Bhanudaya Secondary School · Built with ❤️ by Abhi Parajuli
    </div>
  </div>
</body>
</html>"
        };

        mail.To.Add(new MailAddress(toEmail, toName));
        await smtp.SendMailAsync(mail);
    }
}
