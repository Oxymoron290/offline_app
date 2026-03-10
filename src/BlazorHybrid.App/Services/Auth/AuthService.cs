using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace BlazorHybrid.App.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IPublicClientApplication _pca;
    private readonly ILogger<AuthService> _logger;
    private readonly string[] _scopes;
    private AuthenticationResult? _authResult;

    public bool IsAuthenticated => _authResult is not null && _authResult.ExpiresOn > DateTimeOffset.UtcNow;
    public string? UserName => _authResult?.Account?.Username;
    public string? UserId => _authResult?.UniqueId;

    public AuthService(IPublicClientApplication pca, ILogger<AuthService> logger, AuthConfiguration config)
    {
        _pca = pca;
        _logger = logger;
        _scopes = config.Scopes;
    }

    public async Task<string?> LoginAsync()
    {
        try
        {
            // Try silent auth first
            var accounts = await _pca.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

            if (firstAccount is not null)
            {
                try
                {
                    _authResult = await _pca.AcquireTokenSilent(_scopes, firstAccount).ExecuteAsync();
                    _logger.LogInformation("Silent auth succeeded for {User}", _authResult.Account.Username);
                    return _authResult.AccessToken;
                }
                catch (MsalUiRequiredException)
                {
                    _logger.LogInformation("Silent auth failed, falling back to interactive");
                }
            }

            // Interactive auth
            _authResult = await _pca.AcquireTokenInteractive(_scopes).ExecuteAsync();
            _logger.LogInformation("Interactive auth succeeded for {User}", _authResult.Account.Username);
            return _authResult.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed");
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            var accounts = await _pca.GetAccountsAsync();
            foreach (var account in accounts)
            {
                await _pca.RemoveAsync(account);
            }
            _authResult = null;
            _logger.LogInformation("Logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed");
        }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        if (_authResult is null) return null;

        if (_authResult.ExpiresOn <= DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return await LoginAsync();
        }

        return _authResult.AccessToken;
    }
}

public class AuthConfiguration
{
    public string ClientId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = [];
    public string RedirectUri { get; set; } = string.Empty;
}
