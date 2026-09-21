namespace lost_and_found.Models;

public sealed record LostFoundItem(
    int Id,
    string Title,
    string Category,
    string Status,
    string Location,
    string TimeAgo,
    string Description,
    string Reporter,
    string HoldingLocation,
    int? PotentialMatchPercent = null,
    string? MatchingReportTitle = null,
    string? ImageUrl = null
    );

public static class LostFoundData
{
    public static IReadOnlyList<LostFoundItem> Items { get; } = new[]
    {
        new LostFoundItem(
            1,
            "Black Leather Wallet",
            "Wallet",
            "Lost",
            "Balme Library",
            "2 days ago",
            "Black leather wallet with three cards inside.",
            "Kwame A.",
            "Student Affairs Office"),
        new LostFoundItem(
            2,
            "iPhone 13 (Blue)",
            "Electronics",
            "Found",
            "Night Market",
            "1 day ago",
            "Blue iPhone 13 in a clear protective case.",
            "Nana E.",
            "Campus Security Office"),
        new LostFoundItem(
            3,
            "Blue Water Bottle",
            "Personal Item",
            "Found",
            "JQB Complex",
            "3 days ago",
            "A steel blue water bottle with a UG Alumni Association sticker on the side and a small dent near the base. Found near the vending machines and handed in to the JQB porters' lodge.",
            "Ama K.",
            "Campus Security Office - Legon",
            91,
            "Blue Steel Bottle"),
        new LostFoundItem(
            4,
            "Student ID Card",
            "ID Card",
            "Lost",
            "Commonwealth Hall",
            "Today",
            "University student ID card found near the main entrance.",
            "Yaw B.",
            "Commonwealth Hall Front Desk"),
        new LostFoundItem(
            5,
            "Silver Car Keys",
            "Keys",
            "Found",
            "Night Market",
            "4 days ago",
            "Silver car keys on a plain black key ring.",
            "Esi M.",
            "Night Market Security Post"),
        new LostFoundItem(
            6,
            "Grey Backpack",
            "Bag",
            "Lost",
            "JQB Complex",
            "5 days ago",
            "Grey backpack containing books and stationery.",
            "Kojo P.",
            "JQB Porters' Lodge")
    };

    public static LostFoundItem GetItem(int id) =>
        Items.FirstOrDefault(item => item.Id == id) ?? Items[2];
}
