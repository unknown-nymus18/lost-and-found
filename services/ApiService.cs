using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using lost_and_found.Models;


namespace lost_and_found.Services;

using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Forms;
using lost_and_found.Pages.Report;
using lost_and_found.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    private readonly AuthService _authService;
    private const string baseUrl = "https://lost-and-found-b3dyanfccqbdaqak.southafricanorth-01.azurewebsites.net/api/";


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

    public class ReportRequest
    {
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required int Category { get; set; }
        public required string Location { get; set; }
        public required DateTimeOffset Date { get; set; }

    }

    public class ForgotPasswordRequest
    {
        public required string Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        public required string Token { get; set; }
        public required string NewPassword { get; set; }
    }

    public class SubmitClaimRequest
    {
        public required int FoundReportId { get; set; }
        public required string ProofDescription { get; set; }
    }

    public class UpdateProfileRequest
    {
        public required string Name { get; set; }
        public required string Phone { get; set; }
        public required string Hall { get; set; }
        public required string PreferredPickupLocation { get; set; }
    }

    public class NotificationPreferencesRequest
    {
        public required bool MatchAlerts { get; set; }
        public required bool ClaimUpdates { get; set; }
        public required bool FinderMessages { get; set; }
        public required bool SmsAlerts { get; set; }
    }


    public ApiService(HttpClient httpClient, AuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    public async Task<string> GetRawApiDataAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{baseUrl}reports/");
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
            var response = await _httpClient.GetAsync($"{baseUrl}reports/");
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


    public async Task<ReportItem?> GetLostItemById(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{baseUrl}reports/lost/{id}");
            response.EnsureSuccessStatusCode();

            var report = await response.Content.ReadFromJsonAsync<ReportItem>();
            return report;
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    public async Task<ReportItem?> GetFoundItemById(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{baseUrl}reports/found/{id}");
            response.EnsureSuccessStatusCode();

            var report = await response.Content.ReadFromJsonAsync<ReportItem>();
            return report;
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
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
            var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/auth/register/", requestBody);
            response.EnsureSuccessStatusCode();

            var registeredUser = await response.Content.ReadFromJsonAsync<UserModels>();
            Console.WriteLine(registeredUser);

            if (registeredUser is not null)
            {
                await _authService.SetUserAsync(registeredUser);
            }

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
            var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/auth/login/", requestBody);
            response.EnsureSuccessStatusCode();

            var registeredUser = await response.Content.ReadFromJsonAsync<UserModels>();
            Console.WriteLine(registeredUser);

            if (registeredUser is not null)
            {
                await _authService.SetUserAsync(registeredUser);
            }

            return registeredUser;

        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request error while Signing in user: {ex.Message}");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"Request timed out while Signing in user: {ex.Message}");
            return null;
        }
        catch (System.Text.Json.JsonException ex)
        {
            Console.WriteLine($"Error parsing registration response JSON: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error while Signing in user: {ex.Message}");
            return null;
        }

    }

    public async Task<List<ClaimItem>?> GetUserClaims()
    {
        if (_authService.CurrentUser is null)
        {
            Console.WriteLine("Log in first");
            return null;
        }
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/claims/mine");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.CurrentUser.token);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var userClaims = await response.Content.ReadFromJsonAsync<List<ClaimItem>>();
            return userClaims;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    public async Task<List<MatchItem>?> GetMyMatches()
    {
        var user = _authService.CurrentUser;
        if (user is null || string.IsNullOrEmpty(user.token))
        {
            Console.WriteLine("Log in first");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}matches/mine");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.token);

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<MatchItem>>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching matches: {ex.Message}");
            return null;
        }
    }

    public async Task<ClaimItem?> MakeClaim(string foundReportId, string proofDescription)
    {
        var user = _authService.CurrentUser;
        if (user is null || string.IsNullOrEmpty(user.token))
        {
            Console.WriteLine("Log in first");
            return null;
        }
        try
        {
            var body = new ClaimItemRequest { foundReportId = foundReportId, proofDescription = proofDescription };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}claims")
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.token);

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ClaimItem>();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    public async Task<List<ReportItem>?> GetUserReports()
    {
        if (_authService.CurrentUser is null)
        {
            Console.WriteLine("Log in first");
            return null;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/reports/mine");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.CurrentUser.token);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var userReports = await response.Content.ReadFromJsonAsync<List<ReportItem>>();
            return userReports;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    public async Task<ApiResult<bool>> DeleteReport(ReportItem report)
    {
        var user = _authService.CurrentUser;
        if (user is null || string.IsNullOrEmpty(user.token))
            return new(false, "Please log in first.");

        var kind = report.Kind.Trim().ToLowerInvariant();
        if (kind is not ("lost" or "found"))
            return new(false, "This report has an unsupported kind.");

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Delete, $"{baseUrl}reports/{kind}/{report.Id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.token);

            using var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return new(false, "Your session has expired. Please log in again.");

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync();
                return new(false, $"Couldn't delete this report. Server error {(int)response.StatusCode}: {detail}");
            }

            return new(true, null);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(ex);
            return new(false, "Couldn't reach the server. Check your connection and try again.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new(false, "Something went wrong. Please try again.");
        }
    }

    public record ApiResult<T>(T? Data, string? Error)
    {
        public bool Ok => Error is null;
    }

    public async Task<ApiResult<ReportItem>> ReportLostItem(
        int category, string title, string description, DateTimeOffset date, string location)
    {
        var user = _authService.CurrentUser;
        if (user is null || string.IsNullOrEmpty(user.token))
            return new(null, "Please log in first.");

        var body = new ReportRequest
        {
            Category = category,
            Title = title,
            Description = description,
            Location = location,
            Date = date.ToUniversalTime()
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}reports/lost")
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.token);
            Console.WriteLine(await request.Content!.ReadAsStringAsync());

            using var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return new(null, "Your session has expired. Please log in again.");

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync();
                return new(null, $"Server error {(int)response.StatusCode}: {detail}");
            }

            var item = await response.Content.ReadFromJsonAsync<ReportItem>();
            return item is null
                ? new(null, "The server returned an empty response.")
                : new(item, null);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(ex);
            return new(null, "Couldn't reach the server. Check your connection and try again.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new(null, "Something went wrong. Please try again.");
        }
    }

    public async Task<ApiResult<ReportItem>> ReportLostItemWithPhoto(
        int category, string title, string description, DateTimeOffset date, string location, IBrowserFile? photo)
    {
        var user = _authService.CurrentUser;
        if (user is null || string.IsNullOrEmpty(user.token))
            return new(null, "Please log in first.");

        const long maxPhotoSize = 10 * 1024 * 1024;
        if (photo is not null && photo.Size > maxPhotoSize)
            return new(null, "The photo must be 10 MB or smaller.");

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(title), "Title");
        content.Add(new StringContent(description), "Description");
        content.Add(new StringContent(category.ToString()), "Category");
        content.Add(new StringContent(location), "Location");
        content.Add(new StringContent(date.ToUniversalTime().ToString("O")), "Date");

        try
        {
            if (photo is not null)
            {
                var streamContent = new StreamContent(photo.OpenReadStream(maxPhotoSize));
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(photo.ContentType) ? "application/octet-stream" : photo.ContentType);
                content.Add(streamContent, "Photo", photo.Name);
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}reports/lost/with-photo")
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.token);

            using var response = await _httpClient.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return new(null, "Your session has expired. Please log in again.");

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync();
                return new(null, $"Server error {(int)response.StatusCode}: {detail}");
            }

            var item = await response.Content.ReadFromJsonAsync<ReportItem>();
            return item is null
                ? new(null, "The server returned an empty response.")
                : new(item, null);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(ex);
            return new(null, "Couldn't reach the server. Check your connection and try again.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new(null, "Something went wrong. Please try again.");
        }
    }


    public async Task<ApiResult<ReportItem>> ReportFoundItem(
        int category, string title, string description, DateTimeOffset date, string location)
    {
        var user = _authService.CurrentUser;
        if (user is null || string.IsNullOrEmpty(user.token))
            return new(null, "Please log in first.");

        var body = new ReportRequest
        {
            Category = category,
            Title = title,
            Description = description,
            Location = location,
            Date = date.ToUniversalTime()
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}reports/found")
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.token);
            Console.WriteLine(await request.Content!.ReadAsStringAsync());

            using var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return new(null, "Your session has expired. Please log in again.");

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync();
                return new(null, $"Server error {(int)response.StatusCode}: {detail}");
            }

            var item = await response.Content.ReadFromJsonAsync<ReportItem>();
            return item is null
                ? new(null, "The server returned an empty response.")
                : new(item, null);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(ex);
            return new(null, "Couldn't reach the server. Check your connection and try again.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new(null, "Something went wrong. Please try again.");
        }
    }

    public async Task<ApiResult<ReportItem>> ReportFoundItemWithPhoto(
        int category, string title, string description, DateTimeOffset date, string location, IBrowserFile? photo)
    {
        var user = _authService.CurrentUser;
        if (user is null || string.IsNullOrEmpty(user.token))
            return new(null, "Please log in first.");

        const long maxPhotoSize = 10 * 1024 * 1024;
        if (photo is not null && photo.Size > maxPhotoSize)
            return new(null, "The photo must be 10 MB or smaller.");

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(title), "Title");
        content.Add(new StringContent(description), "Description");
        content.Add(new StringContent(category.ToString()), "Category");
        content.Add(new StringContent(location), "Location");
        content.Add(new StringContent(date.ToUniversalTime().ToString("O")), "Date");

        try
        {
            if (photo is not null)
            {
                var streamContent = new StreamContent(photo.OpenReadStream(maxPhotoSize));
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(photo.ContentType) ? "application/octet-stream" : photo.ContentType);
                content.Add(streamContent, "Photo", photo.Name);
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}reports/found/with-photo")
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.token);

            using var response = await _httpClient.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return new(null, "Your session has expired. Please log in again.");

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync();
                return new(null, $"Server error {(int)response.StatusCode}: {detail}");
            }

            var item = await response.Content.ReadFromJsonAsync<ReportItem>();
            return item is null
                ? new(null, "The server returned an empty response.")
                : new(item, null);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(ex);
            return new(null, "Couldn't reach the server. Check your connection and try again.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new(null, "Something went wrong. Please try again.");
        }
    }


    public async Task<List<NotificationItem>?> GetNotifications()
    {
        if (_authService.CurrentUser is null)
        {
            Console.WriteLine("Log in first");
            return null;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}notifications/");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.CurrentUser.token);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<NotificationItem>>();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    public async Task<ApiResult<bool>> RequestPasswordReset(string email)
    {
        try
        {
            var body = new ForgotPasswordRequest { Email = email };
            using var response = await _httpClient.PostAsJsonAsync($"{baseUrl}auth/forgot-password", body);

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync();
                return new(false, $"Server error {(int)response.StatusCode}: {detail}");
            }

            return new(true, null);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(ex);
            return new(false, "Couldn't reach the server. Check your connection and try again.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new(false, "Something went wrong. Please try again.");
        }
    }

    public async Task<ApiResult<bool>> ResetPassword(string token, string newPassword)
    {
        try
        {
            var body = new ResetPasswordRequest { Token = token, NewPassword = newPassword };
            using var response = await _httpClient.PostAsJsonAsync($"{baseUrl}auth/reset-password", body);

            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Gone)
                return new(false, "This reset link has expired or already been used. Request a new one.");

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync();
                return new(false, $"Server error {(int)response.StatusCode}: {detail}");
            }

            return new(true, null);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(ex);
            return new(false, "Couldn't reach the server. Check your connection and try again.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new(false, "Something went wrong. Please try again.");
        }
    }

    public async Task<ApiResult<ClaimItem>> SubmitClaim(int foundReportId, string proofDescription)
    {
        var user = _authService.CurrentUser;
        if (user is null || string.IsNullOrEmpty(user.token))
            return new(null, "Please log in first.");

        try
        {
            var body = new SubmitClaimRequest
            {
                FoundReportId = foundReportId,
                ProofDescription = proofDescription
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}claims")
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.token);

            using var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return new(null, "Your session has expired. Please log in again.");

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync();
                return new(null, $"Server error {(int)response.StatusCode}: {detail}");
            }

            var claim = await response.Content.ReadFromJsonAsync<ClaimItem>();
            return claim is null
                ? new(null, "The server returned an empty response.")
                : new(claim, null);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(ex);
            return new(null, "Couldn't reach the server. Check your connection and try again.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new(null, "Something went wrong. Please try again.");
        }
    }

    public async Task<ApiResult<bool>> MarkAllNotificationsRead()
    {
        var user = _authService.CurrentUser;
        if (user is null || string.IsNullOrEmpty(user.token))
            return new(false, "Please log in first.");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}notifications/mark-all-read");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.token);

            using var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync();
                return new(false, $"Server error {(int)response.StatusCode}: {detail}");
            }

            return new(true, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new(false, "Couldn't update your notifications. Please try again.");
        }
    }

    public async Task<ApiResult<UserModels>> UpdateProfile(
        string name, string phone, string hall, string preferredPickupLocation)
    {
        var user = _authService.CurrentUser;
        if (user is null || string.IsNullOrEmpty(user.token))
            return new(null, "Please log in first.");

        try
        {
            var body = new UpdateProfileRequest
            {
                Name = name,
                Phone = phone,
                Hall = hall,
                PreferredPickupLocation = preferredPickupLocation
            };

            using var request = new HttpRequestMessage(HttpMethod.Patch, $"{baseUrl}auth/me")
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.token);

            using var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return new(null, "Your session has expired. Please log in again.");

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync();
                return new(null, $"Server error {(int)response.StatusCode}: {detail}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var node = JsonNode.Parse(json)!.AsObject();
            node["token"] = user.token;

            var updated = JsonSerializer.Deserialize<UserModels>(node);
            if (updated is null)
                return new(null, "The server returned an empty response.");

            await _authService.SetUserAsync(updated);
            return new(updated, null);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(ex);
            return new(null, "Couldn't reach the server. Check your connection and try again.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new(null, "Something went wrong. Please try again.");
        }
    }

    public async Task<ApiResult<bool>> UpdateNotificationPreferences(
        bool matchAlerts, bool claimUpdates, bool finderMessages, bool smsAlerts)
    {
        var user = _authService.CurrentUser;
        if (user is null || string.IsNullOrEmpty(user.token))
            return new(false, "Please log in first.");

        try
        {
            var body = new NotificationPreferencesRequest
            {
                MatchAlerts = matchAlerts,
                ClaimUpdates = claimUpdates,
                FinderMessages = finderMessages,
                SmsAlerts = smsAlerts
            };

            using var request = new HttpRequestMessage(HttpMethod.Put, $"{baseUrl}auth/me/notification-preferences")
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.token);

            using var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync();
                return new(false, $"Server error {(int)response.StatusCode}: {detail}");
            }

            return new(true, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new(false, "Couldn't save your preferences. Please try again.");
        }
    }

    public async Task<UserModels?> GetUser()
    {
        if (_authService.CurrentUser is null)
        {
            Console.WriteLine("Log in first");
            return null;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.CurrentUser.token);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var node = JsonNode.Parse(json)!.AsObject();

            node["token"] = _authService.CurrentUser.token;

            var user = JsonSerializer.Deserialize<UserModels>(node);

            return user;

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }

}