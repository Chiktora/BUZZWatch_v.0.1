using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BuzzWatch.Api.Authentication
{
    public static class AuthenticationExtensions
    {
        public static AuthenticationBuilder AddApiKeySupport(this AuthenticationBuilder builder)
        {
            return builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationHandler.Scheme, 
                options => { });
        }

        public static JwtBearerOptions ConfigureJwtOptions(this JwtBearerOptions options, IConfiguration config)
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config["Jwt:Issuer"],
                ValidAudience = config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            return options;
        }
    }
} 