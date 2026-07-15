using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SecureGate.Api.Infrastructure.Security;
using SecureGate.Domain.Entities;
using Xunit;

namespace SecureGate.Tests.Security;

public class JwtTokenGeneratorTests
{
    private const string TestSecret = "this-is-a-development-only-secret-key-please-change-me";

    private static JwtTokenGenerator CreateGenerator(int expirationMinutes = 60)
    {
        var settings = new JwtSettings
        {
            Secret = TestSecret,
            Issuer = "SecureGate",
            Audience = "SecureGate",
            ExpirationMinutes = expirationMinutes,
        };
        return new JwtTokenGenerator(Options.Create(settings));
    }

    [Fact]
    public void Generate_ReturnsATokenWithTheExpectedClaims()
    {
        var generator = CreateGenerator();
        var user = new User("Ana Souza", "ana@example.com", "hashed-password");

        var result = generator.Generate(user);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.Equal(user.Id.ToString(), token.Claims.Single(c => c.Type == "sub").Value);
        Assert.Equal(user.Email, token.Claims.Single(c => c.Type == "email").Value);
        Assert.Equal(user.Name, token.Claims.Single(c => c.Type == "name").Value);
        Assert.Equal("SecureGate", token.Issuer);
        Assert.Contains("SecureGate", token.Audiences);
    }

    [Fact]
    public void Generate_SetsExpiresAtAccordingToConfiguredExpirationMinutes()
    {
        var generator = CreateGenerator(expirationMinutes: 30);
        var user = new User("Ana Souza", "ana@example.com", "hashed-password");
        var expectedExpiration = DateTime.UtcNow.AddMinutes(30);

        var result = generator.Generate(user);

        Assert.InRange(result.ExpiresAt, expectedExpiration.AddSeconds(-5), expectedExpiration.AddSeconds(5));
    }

    [Fact]
    public void Generate_ProducesATokenSignedWithTheConfiguredSecret()
    {
        var generator = CreateGenerator();
        var user = new User("Ana Souza", "ana@example.com", "hashed-password");

        var result = generator.Generate(user);

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = "SecureGate",
            ValidAudience = "SecureGate",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret)),
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(result.Token, validationParameters, out _);

        Assert.NotNull(principal);
    }
}
