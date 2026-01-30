using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OneOf.Types;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace BlazorPortalCamera.Auth;

public class YarpAuthHandler :
  AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _config;

    public YarpAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration config) : base(options, logger, encoder)
    {
        _config = config;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));

          try
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (!authHeader.StartsWith("Basic "))
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));


            var credentials = Encoding.UTF8
                .GetString(Convert.FromBase64String(authHeader.Substring(6)))
                .Split(':', 2);

            var username = credentials[0];
            var password = credentials[1];

            var validUser = _config["Authentication:Username"];
            var validPass = _config["Authentication:Password"];

            if (username != validUser || password != validPass)
                return Task.FromResult(AuthenticateResult.Fail("Invalid credentials"));


            var claims = new[] { new Claim(ClaimTypes.Name, username) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
          }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers["WWW-Authenticate"] = "Basic realm=\"CameraAccess\"";
        Response.StatusCode = 401;
        return Task.CompletedTask;
    }
}
