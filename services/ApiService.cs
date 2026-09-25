using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using lost_and_found.Models;

namespace lost_and_found.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private const string ReportsUrl = "https://lost-and-found-b3dyanfccqbdaqak.southafricanorth-01.azurewebsites.net/api/reports/";
    private const string RegisterUrl = "https://lost-and-found-b3dyanfccqbdaqak.southafricanorth-01.azurewebsites.net/api/auth/register/";
    private const string LoginUrl = "https://lost-and-found-b3dyanfccqbdaqak.southafricanorth-01.azurewebsites.net/api/auth/login/";

    public class RegisterRequest
    {
        public required string Name { get; set; } = string.Empty;
        public required string Email { get; set; } = string.Empty;
        public required string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }

    }

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetRawApiDataAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(ReportsUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine(json);

            return json;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request error while fetching raw API data: {ex.Message}");
            return string.Empty;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"Request timed out while fetching raw API data: {ex.Message}");
            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error while fetching raw API data: {ex.Message}");
            return string.Empty;
        }
    }

    public async Task<List<ReportItem>> GetReportItemsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(ReportsUrl);
            response.EnsureSuccessStatusCode();

            var items = await response.Content.ReadFromJsonAsync<List<ReportItem>>();
            return items ?? new List<ReportItem>();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request error while fetching report items: {ex.Message}");
            return new List<ReportItem>();
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"Request timed out while fetching report items: {ex.Message}");
            return new List<ReportItem>();
        }
        catch (System.Text.Json.JsonException ex)
        {
            Console.WriteLine($"Error parsing report items JSON: {ex.Message}");
            return new List<ReportItem>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error while fetching report items: {ex.Message}");
            return new List<ReportItem>();
        }
    }

    public async Task<UserModels?> RegisterUser(string name, string email, string password)
    {
        var requestBody = new RegisterRequest
        {
            Name = name,
            Email = email,
            Password = password
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(RegisterUrl, requestBody);
            response.EnsureSuccessStatusCode();

            var registeredUser = await response.Content.ReadFromJsonAsync<UserModels>();
            Console.WriteLine(registeredUser);

            return registeredUser;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request error while registering user: {ex.Message}");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"Request timed out while registering user: {ex.Message}");
            return null;
        }
        catch (System.Text.Json.JsonException ex)
        {
            Console.WriteLine($"Error parsing registration response JSON: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error while registering user: {ex.Message}");
            return null;
        }
    }

    public async Task<UserModels?> LoginUser(string email, string password)
    {
        try
        {
            var requestBody = new LoginRequest { Email = email, Password = password };
            var response = await _httpClient.PostAsJsonAsync(LoginUrl, requestBody);
            response.EnsureSuccessStatusCode();

            var registeredUser = await response.Content.ReadFromJsonAsync<UserModels>();
            Console.WriteLine(registeredUser);

            return registeredUser;

        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request error while registering user: {ex.Message}");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"Request timed out while registering user: {ex.Message}");
            return null;
        }
        catch (System.Text.Json.JsonException ex)
        {
            Console.WriteLine($"Error parsing registration response JSON: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error while registering user: {ex.Message}");
            return null;
        }

    }

}