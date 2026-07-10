using System.Security.Claims;
using System.Text.Encodings.Web;
using HealthCasePlatform.Api.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace HealthCasePlatform.Api.Tests.Integration;

internal sealed class FakeAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string RolesHeader = "X-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        StringValues headerValue = Request.Headers[RolesHeader];
        var header = headerValue.ToString();

        var roles = string.IsNullOrWhiteSpace(header)
            ? AppRoles.All
            : header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var claims = roles
            .Select(role => new Claim(ClaimTypes.Role, role))
            .Append(new Claim(ClaimTypes.Name, "test-user"));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
