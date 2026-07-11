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
    public const string UserHeader = "X-User";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        StringValues rolesHeaderValue = Request.Headers[RolesHeader];
        var rolesHeader = rolesHeaderValue.ToString();

        var roles = string.IsNullOrWhiteSpace(rolesHeader)
            ? AppRoles.All
            : rolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var userId = Request.Headers.TryGetValue(UserHeader, out var userValue)
            && !StringValues.IsNullOrEmpty(userValue)
                ? userValue.ToString()
                : "test-user";

        var claims = roles
            .Select(role => new Claim(ClaimTypes.Role, role))
            .Append(new Claim(ClaimTypes.NameIdentifier, userId))
            .Append(new Claim(ClaimTypes.Name, userId));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
