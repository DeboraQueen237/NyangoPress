using NyangoPress.Shared.Common;

namespace NyangoPress.Web.Services;

public sealed class AuthSessionService
{
    public string? AccessToken { get; private set; }
    public UserInfoDto? CurrentUser { get; private set; }
    public bool IsDarkMode { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken) && CurrentUser is not null;
    public string Role => CurrentUser?.Role ?? string.Empty;

    public event Action? OnChange;

    public void SetSession(AuthResponse auth)
    {
        AccessToken = auth.AccessToken;
        CurrentUser = auth.User;
        OnChange?.Invoke();
    }

    public void Logout()
    {
        AccessToken = null;
        CurrentUser = null;
        OnChange?.Invoke();
    }

    public void ToggleDarkMode()
    {
        IsDarkMode = !IsDarkMode;
        OnChange?.Invoke();
    }

    public bool HasAnyRole(params string[] roles)
        => IsAuthenticated && roles.Contains(Role, StringComparer.OrdinalIgnoreCase);
}
