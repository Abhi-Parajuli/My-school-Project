namespace SchoolAPI.Models;

public class User
{
    public int    Id         { get; set; }
    public string FirstName  { get; set; } = string.Empty;
    public string LastName   { get; set; } = string.Empty;
    public string Email      { get; set; } = string.Empty;
    public string Password   { get; set; } = string.Empty;
    public bool   IsVerified { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Comment
{
    public int    Id          { get; set; }
    public string UserName    { get; set; } = string.Empty;
    public string UserEmail   { get; set; } = string.Empty;
    public string? UserImage  { get; set; }
    public string CommentText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EmailVerification
{
    public int    Id        { get; set; }
    public int    UserId    { get; set; }
    public string Token     { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool   IsUsed   { get; set; } = false;
    public User?  User     { get; set; }
}

// ── OTP / Password-reset table ────────────────────────────────
public class OtpVerification
{
    public int    Id         { get; set; }
    public string Email      { get; set; } = string.Empty;
    public string Otp        { get; set; } = string.Empty;
    public string? ResetToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool   IsUsed     { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ── Request DTOs ──────────────────────────────────────────────
public class RegisterRequest
{
    public string FName    { get; set; } = string.Empty;
    public string LName    { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SignInRequest
{
    public string Email    { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class GoogleSignInRequest
{
    public string Credential { get; set; } = string.Empty;
}

public class AuthResponse
{
    public bool    Success  { get; set; }
    public string? Token    { get; set; }
    public string? FullName { get; set; }
    public string? Email    { get; set; }
    public string? Message  { get; set; }
}

public class CommentRequest
{
    public string  UserName    { get; set; } = string.Empty;
    public string  UserEmail   { get; set; } = string.Empty;
    public string? UserImage   { get; set; }
    public string  CommentText { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class VerifyOtpRequest
{
    public string Email { get; set; } = string.Empty;
    public string Otp   { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Email       { get; set; } = string.Empty;
    public string ResetToken  { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}