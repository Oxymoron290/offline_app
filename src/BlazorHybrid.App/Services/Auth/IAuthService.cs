namespace BlazorHybrid.App.Services.Auth;

public interface IAuthService
{
    Task<string?> LoginAsync();
    Task LogoutAsync();
    Task<string?> GetAccessTokenAsync();
    bool IsAuthenticated { get; }
    string? UserName { get; }
    string? UserId { get; }
}
