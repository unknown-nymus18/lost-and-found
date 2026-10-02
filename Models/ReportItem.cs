namespace lost_and_found.Models;

public class ReportItem
{
    public int Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Category { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public string PhotoUrl { get; set; } = string.Empty;
    public int Status { get; set; }
    public string ReportedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public static class ReportImageUrl
{
    private const string BaseUrl = "https://lost-and-found-b3dyanfccqbdaqak.southafricanorth-01.azurewebsites.net/";

    public static string Resolve(string? photoUrl)
    {
        if (string.IsNullOrWhiteSpace(photoUrl))
            return string.Empty;

        if (Uri.TryCreate(photoUrl, UriKind.Absolute, out var absoluteUri) &&
            (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
        {
            return absoluteUri.AbsoluteUri;
        }

        return new Uri(new Uri(BaseUrl), photoUrl.TrimStart('/')).AbsoluteUri;
    }
}


public class NewReportItem
{

}