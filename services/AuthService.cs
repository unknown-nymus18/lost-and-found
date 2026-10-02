namespace lost_and_found.Services;

using System.Text.Json;
using lost_and_found.Models;
using Microsoft.JSInterop;

public class AuthService
{
    private readonly IJSRuntime _jsRuntime;
    private const string StorageKey = "authUser";

    public UserModels? CurrentUser { get; private set; }

    public bool isLoggedIn => CurrentUser is not null;

    public event Action? OnChange;

    public AuthService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Call once on app startup (e.g. in MainLayout.OnInitializedAsync) to
    /// restore the logged-in user from localStorage after a page reload.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            Console.WriteLine(json);
            if (!string.IsNullOrWhiteSpace(json))
            {
                CurrentUser = JsonSerializer.Deserialize<UserModels>(json);
                NotifyStateChanged();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to restore auth state: {ex.Message}");
        }
    }

    public async Task SetUserAsync(UserModels user)
    {
        CurrentUser = user;
        var json = JsonSerializer.Serialize(user);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        NotifyStateChanged();
    }

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
