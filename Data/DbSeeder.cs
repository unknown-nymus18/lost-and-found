using CampusLostAndFound.Models;
using CampusLostAndFound.Services;
using Microsoft.EntityFrameworkCore;

namespace CampusLostAndFound.Data;

/// <summary>Fills a migrated database with demo data on first run.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Users.AnyAsync()) return; // already seeded

        User NewUser(string name, string email, string password, string role)
        {
            var (hash, salt) = PasswordHasher.Hash(password);
            return new User { Name = name, Email = email, PasswordHash = hash, PasswordSalt = salt, Role = role };
        }

        var admin = NewUser("Campus Admin", "admin@ug.edu.gh", "Admin@123", Roles.Admin);
        var ama = NewUser("Ama Serwaa", "ama@st.ug.edu.gh", "Password123", Roles.Student);
        var kofi = NewUser("Kofi Mensah", "kofi@st.ug.edu.gh", "Password123", Roles.Student);
        var kingsley = NewUser("Kingsley Ofori Atta", "kingsley@st.ug.edu.gh", "Password123", Roles.Student);

        db.Users.AddRange(admin, ama, kofi, kingsley);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;

        // A lost/found pair engineered to match strongly (category + location + date + keywords).
        var lostPhone = new LostReport
        {
            UserId = ama.Id,
            Title = "Lost blue iPhone 13",
            Description = "iPhone 13 with a cracked screen protector and a navy silicone case. Home screen is a photo of a dog.",
            Category = ItemCategory.Electronics,
            Location = "Balme Library, 2nd floor",
            DateLost = now.AddDays(-3),
            Status = ReportStatus.Open
        };

        var foundPhone = new FoundReport
        {
            UserId = kofi.Id,
            Title = "Found iPhone with navy case",
            Description = "Picked up an iPhone in a navy case near the reading desks. Screen protector is cracked at the corner.",
            Category = ItemCategory.Electronics,
            Location = "Balme Library",
            DateFound = now.AddDays(-2),
            Status = ReportStatus.Open
        };

        // Some unrelated reports so browsing/filters have variety.
        var lostId = new LostReport
        {
            UserId = kingsley.Id,
            Title = "Lost student ID card",
            Description = "University of Ghana ID card in the name of Kingsley Ofori Atta.",
            Category = ItemCategory.IdentityCard,
            Location = "N Block Lecture Theatre",
            DateLost = now.AddDays(-1),
            Status = ReportStatus.Open
        };

        var foundKeys = new FoundReport
        {
            UserId = ama.Id,
            Title = "Found bunch of keys",
            Description = "Three keys on a red lanyard, found by the sports stadium entrance.",
            Category = ItemCategory.Keys,
            Location = "Sports Stadium",
            DateFound = now.AddDays(-4),
            Status = ReportStatus.Open
        };

        var foundWallet = new FoundReport
        {
            UserId = kofi.Id,
            Title = "Found brown leather wallet",
            Description = "Brown wallet with some cash and a bank card inside. Handed details to the office.",
            Category = ItemCategory.Wallet,
            Location = "Central Cafeteria",
            DateFound = now.AddDays(-5),
            Status = ReportStatus.Open
        };

        db.LostReports.AddRange(lostPhone, lostId);
        db.FoundReports.AddRange(foundPhone, foundKeys, foundWallet);
        await db.SaveChangesAsync();

        // Run the matching engine once so the demo opens with a real match present.
        var matching = scope.ServiceProvider.GetRequiredService<MatchingService>();
        await matching.ScanForLostAsync(lostPhone);
        await matching.ScanForLostAsync(lostId);
    }
}
