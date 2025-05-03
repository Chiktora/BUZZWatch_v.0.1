using BuzzWatch.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace BuzzWatch.Api.Authentication
{
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public new TimeProvider? TimeProvider { get; set; }
    }

    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private readonly ApplicationDbContext _db;
        public new const string Scheme = "ApiKey";

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options, 
            ILoggerFactory logger,
            UrlEncoder encoder,
            ApplicationDbContext db)
            : base(options, logger, encoder) => _db = db;

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKeyValue))
                return AuthenticateResult.NoResult();

            var key = apiKeyValue.ToString();
            var apiKey = await _db.ApiKeys
                .FirstOrDefaultAsync(k => k.Key == key && k.ExpiresAt > DateTimeOffset.UtcNow);

            if (apiKey is null)
                return AuthenticateResult.Fail("Invalid API key");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, apiKey.DeviceId.ToString()),
                new Claim("scope", "device")
            };

            var identity = new ClaimsIdentity(claims, Scheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme);

            return AuthenticateResult.Success(ticket);
        }
    }
} 