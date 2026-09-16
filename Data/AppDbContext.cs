using CampusLostAndFound.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusLostAndFound.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<LostReport> LostReports => Set<LostReport>();
    public DbSet<FoundReport> FoundReports => Set<FoundReport>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Claim> Claims => Set<Claim>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<User>().HasIndex(u => u.Email).IsUnique();

        // Store enums as readable text in the database rather than integers.
        b.Entity<LostReport>().Property(r => r.Category).HasConversion<string>();
        b.Entity<LostReport>().Property(r => r.Status).HasConversion<string>();
        b.Entity<FoundReport>().Property(r => r.Category).HasConversion<string>();
        b.Entity<FoundReport>().Property(r => r.Status).HasConversion<string>();
        b.Entity<Claim>().Property(c => c.Status).HasConversion<string>();

        // A match points at one lost and one found report; deleting a report
        // removes its matches but never cascades into the other side's data.
        b.Entity<Match>()
            .HasOne(m => m.LostReport)
            .WithMany(r => r.Matches)
            .HasForeignKey(m => m.LostReportId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Match>()
            .HasOne(m => m.FoundReport)
            .WithMany(r => r.Matches)
            .HasForeignKey(m => m.FoundReportId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Claim>()
            .HasOne(c => c.FoundReport)
            .WithMany(r => r.Claims)
            .HasForeignKey(c => c.FoundReportId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Claim>()
            .HasOne(c => c.Claimer)
            .WithMany(u => u.Claims)
            .HasForeignKey(c => c.ClaimerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
