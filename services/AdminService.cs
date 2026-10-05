namespace lost_and_found.Services;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using lost_and_found.Models;

/// <summary>
/// API gateway for admin dashboard data and decisions.
/// Keep admin endpoints here so they are easy to scan and maintain together.
/// </summary>
public sealed class AdminService
{
    private const string ApiBaseUrl = "https://lost-and-found-b3dyanfccqbdaqak.southafricanorth-01.azurewebsites.net/api/";
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    public AdminService(HttpClient httpClient, AuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    public Task<ApiResult<List<ClaimItem>>> GetClaimsAsync() =>
        GetAdminDataAsync<List<ClaimItem>>("claims");

    public Task<ApiResult<List<MatchItem>>> GetMatchesAsync() =>
        GetAdminDataAsync<List<MatchItem>>("matches");

    public Task<ApiResult<ClaimItem>> DecideClaimAsync(int claimId, bool approve, string? note = null) =>
        PostDecisionAsync<ClaimItem>($"claims/{claimId}/decide", approve, note, "claim");

    public Task<ApiResult<MatchItem>> DecideMatchAsync(int matchId, bool approve, string? note = null) =>
        PostDecisionAsync<MatchItem>($"matches/{matchId}/decide", approve, note, "match");

    private async Task<ApiResult<T>> GetAdminDataAsync<T>(string path)
    {
        if (!TryCreateAuthenticatedRequest(HttpMethod.Get, path, out var request, out var authError))
            return new(default, authError);

        try
        {
            using (request)
            {
                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new(default, await ReadApiErrorAsync(response));

                var data = await response.Content.ReadFromJsonAsync<T>();
                return data is null
                    ? new(default, "The server returned an empty response.")
                    : new(data, null);
            }
        }
        catch (HttpRequestException)
        {
            return new(default, "Couldn't reach the server. Check your connection and try again.");
        }
        catch (Exception)
        {
            return new(default, "Something went wrong while loading admin data.");
        }
    }

    private async Task<ApiResult<T>> PostDecisionAsync<T>(string path, bool approve, string? note, string resourceName)
    {
        if (!TryCreateAuthenticatedRequest(HttpMethod.Post, path, out var request, out var authError))
            return new(default, authError);

        request.Content = JsonContent.Create(new { approve, note });

        try
        {
            using (request)
            {
                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new(default, await ReadApiErrorAsync(response));

                var data = response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0
                    ? default
                    : await response.Content.ReadFromJsonAsync<T>();
                return new(data, null);
            }
        }
        catch (HttpRequestException)
        {
            return new(default, "Couldn't reach the server. Check your connection and try again.");
        }
        catch (Exception)
        {
            return new(default, $"Something went wrong while deciding the {resourceName}.");
        }
    }

    private bool TryCreateAuthenticatedRequest(HttpMethod method, string path, out HttpRequestMessage request, out string error)
    {
        request = new HttpRequestMessage(method, $"{ApiBaseUrl}{path}");
        var token = _authService.CurrentUser?.token;
        if (string.IsNullOrWhiteSpace(token))
        {
            request.Dispose();
            error = "Please log in first.";
            request = null!;
            return false;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        error = string.Empty;
        return true;
    }

    private static async Task<string> ReadApiErrorAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return "Your session has expired. Please log in again.";
        if (response.StatusCode == HttpStatusCode.Forbidden)
            return "This account does not have permission to view admin data.";

        var detail = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(detail)
            ? $"Server error {(int)response.StatusCode}."
            : $"Server error {(int)response.StatusCode}: {detail}";
    }

    public record ApiResult<T>(T? Data, string? Error)
    {
        public bool Ok => Error is null;
    }
}
