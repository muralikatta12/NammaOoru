using System;
using Microsoft.EntityFrameworkCore;
using NammaOoru.Entities;

namespace NammaOoru.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportPhoto> ReportPhotos { get; set; }
        public DbSet<OtpVerification> OtpVerifications { get; set; }
    public DbSet<EmailQueue> EmailQueue { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Suppress the pending model changes warning by providing sensible defaults
            modelBuilder.Entity<User>().Property(e => e.CreatedAt).HasDefaultValue(new DateTime(2025, 11, 13, 0, 0, 0, DateTimeKind.Utc));
            modelBuilder.Entity<OtpVerification>().Property(e => e.CreatedAt).HasDefaultValue(new DateTime(2025, 11, 13, 0, 0, 0, DateTimeKind.Utc));

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // OtpVerification configuration
            modelBuilder.Entity<OtpVerification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OtpCode).IsRequired().HasMaxLength(6);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.OtpVerifications)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);  // Set to null when user is deleted
            });

            // ====== Report configuration ======
            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(4000);

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.LocationAddress)
                    .HasMaxLength(500);

                // store enum as int
                entity.Property(e => e.Status)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt);

                entity.Property(e => e.ResolvedAt);

                entity.Property(e => e.UpvoteCount)
                    .HasDefaultValue(0);

                entity.Property(e => e.Priority)
                    .HasDefaultValue(2);

                // Relationships: CreatedByUser (required), AssignedToUser (optional)
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany(u => u.Reports)
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.AssignedToUser)
                    .WithMany(u => u.AssignedReports)
                    .HasForeignKey(e => e.AssignedToUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                // UpdatedByUser relationship (optional) - use NoAction to avoid cascade path conflicts
                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Indexes for common queries
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.CreatedAt);
            });

            // ====== ReportPhoto configuration ======
            modelBuilder.Entity<ReportPhoto>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.PhotoUrl)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.FileName)
                    .HasMaxLength(255);

                entity.Property(e => e.ContentType)
                    .HasMaxLength(100);

                entity.Property(e => e.FileSizeInBytes)
                    .HasDefaultValue(0);

                entity.Property(e => e.UploadedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Relationship: ReportPhoto -> Report (many-to-one)
                entity.HasOne(e => e.Report)
                    .WithMany(r => r.Photos)
                    .HasForeignKey(e => e.ReportId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Index to quickly load photos by report
                entity.HasIndex(e => e.ReportId);
            });

            // ====== EmailQueue configuration ======
            modelBuilder.Entity<EmailQueue>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RecipientEmail).IsRequired().HasMaxLength(255);
                entity.Property(e => e.RecipientName).HasMaxLength(255);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Body).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Pending");
                entity.Property(e => e.Attempts).HasDefaultValue(0);
                entity.Property(e => e.NextRetry).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.NextRetry);
            });
        }
    }

}
