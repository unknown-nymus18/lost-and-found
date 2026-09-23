using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using lost_and_found.Models;

namespace lost_and_found.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private const string ReportsUrl = "https://lost-and-found-b3dyanfccqbdaqak.southafricanorth-01.azurewebsites.net/api/reports/";

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetRawApiDataAsync()
    {
        var response = await _httpClient.GetAsync(ReportsUrl);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        Console.WriteLine(json);

        return json;
    }

    public async Task<List<ReportItem>> GetReportItemsAsync()
    {
        var response = await _httpClient.GetAsync(ReportsUrl);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<ReportItem>>();
        return items ?? new List<ReportItem>();
    }
}