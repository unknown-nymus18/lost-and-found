namespace lost_and_found.Services;

using lost_and_found.Models;


public class AuthService
{
    public UserModels? CurrentUser { get; set; }

    public bool isLoggedIn => CurrentUser is not null;

    public event Action? OnChange;

    public void SetUser(UserModels user)
    {
        CurrentUser = user;
        NotifyStateChanged();
    }

    public void Logout()
    {
        CurrentUser = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}