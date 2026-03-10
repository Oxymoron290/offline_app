using BlazorHybrid.App.Services.Auth;
using System.Net.Http.Headers;

namespace BlazorHybrid.App.Services.Api;

/// <summary>
/// DelegatingHandler that injects the bearer token from AuthService into HTTP requests.
/// </summary>
public class AuthenticatedHttpMessageHandler : DelegatingHandler
{
    private readonly IAuthService _authService;

    public AuthenticatedHttpMessageHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _authService.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
