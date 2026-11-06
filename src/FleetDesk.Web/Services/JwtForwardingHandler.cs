using System.Net.Http.Headers;

namespace FleetDesk.Web.Services;

/// Adjunta el JWT guardado en la cookie de sesion a cada llamada hacia el microservicio backend.
public class JwtForwardingHandler : DelegatingHandler
{
    public const string TokenClaimType = "fleetdesk_jwt";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _httpContextAccessor.HttpContext?.User.FindFirst(TokenClaimType)?.Value;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
