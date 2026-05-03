using Microsoft.EntityFrameworkCore;
using SchoolAPI.Models;

namespace SchoolAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<EmailVerification> EmailVerifications { get; set; }  // NEW

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasColumnName("id");
            entity.Property(u => u.FirstName).HasColumnName("firstName").HasMaxLength(100).IsRequired();
            entity.Property(u => u.LastName).HasColumnName("lastName").HasMaxLength(100).IsRequired();
            entity.Property(u => u.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
            entity.Property(u => u.Password).HasColumnName("password").HasMaxLength(255).IsRequired();
            entity.Property(u => u.IsVerified).HasColumnName("isVerified").HasDefaultValue(false); // NEW
            entity.Property(u => u.CreatedAt).HasColumnName("created_at")..HasDefaultValueSql("NOW()");
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToTable("comments");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasColumnName("id");
            entity.Property(c => c.UserName).HasColumnName("userName").HasMaxLength(100).IsRequired();
            entity.Property(c => c.UserEmail).HasColumnName("userEmail").HasMaxLength(150).IsRequired();
            entity.Property(c => c.UserImage).HasColumnName("userImage").HasMaxLength(500);
            entity.Property(c => c.CommentText).HasColumnName("commentText").IsRequired();
            entity.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        });

        // NEW — email_verifications table
        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.ToTable("email_verifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.Token).HasColumnName("token").HasMaxLength(200).IsRequired();
            entity.Property(e => e.ExpiresAt).HasColumnName("expiresAt");
            entity.Property(e => e.IsUsed).HasColumnName("isUsed").HasDefaultValue(false);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
        });
    }
}
