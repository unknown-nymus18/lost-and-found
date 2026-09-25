namespace lost_and_found.Models;

public static class CategoryOptions
{
    public const string AllCategories = "All Categories";

    public static readonly IReadOnlyList<string> Values = new[]
    {
        "Electronics",
        "ID Cards",
        "Wallet",
        "Keys",
        "Bags",
        "Books",
        "Clothing",
        "Jewellery",
        "Others"
    };

    public static string GetCategoryName(int category)
    {
        return category >= 0 && category < Values.Count ? Values[category] : Values[^1];
    }

    public static IReadOnlyDictionary<string, int> Lookup { get; } =
        Values.Select((name, index) => new KeyValuePair<string, int>(name, index))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
}
