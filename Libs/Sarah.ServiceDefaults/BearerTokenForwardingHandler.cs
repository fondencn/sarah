using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Sarah.ServiceDefaults;

public sealed class BearerTokenForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BearerTokenForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization == null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var authHeader = httpContext?.Request.Headers.Authorization.ToString();

            if (!string.IsNullOrWhiteSpace(authHeader) &&
                AuthenticationHeaderValue.TryParse(authHeader, out var parsedHeader) &&
                string.Equals(parsedHeader.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Authorization = parsedHeader;
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}

public static class BearerTokenForwardingHttpClientBuilderExtensions
{
    public static IHttpClientBuilder AddBearerTokenForwarding(this IHttpClientBuilder builder)
    {
        return builder.AddHttpMessageHandler<BearerTokenForwardingHandler>();
    }
}