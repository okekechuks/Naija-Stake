using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NaijaStake.Infrastructure.Services;
using Xunit;

namespace NaijaStake.Infrastructure.Tests;

public class TokenServiceTests
{
    private IConfiguration CreateConfig(string secret, string issuer = "test-issuer", string audience = "test-audience", int expiryMinutes = 60)
    {
        var dict = new Dictionary<string, string>
        {
            ["JwtSettings:SecretKey"] = secret,
            ["JwtSettings:Issuer"] = issuer,
            ["JwtSettings:Audience"] = audience,
            ["JwtSettings:ExpiryMinutes"] = expiryMinutes.ToString()
        };

        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public void GenerateToken_Produces_Valid_Jwt_With_Claims()
    {
        var secret = "super-secret-key-that-is-at-least-32-chars-long!!!";
        var config = CreateConfig(secret);
        var svc = new TokenService(config);

        var userId = Guid.NewGuid();
        var email = "me@example.com";
        var first = "Chuka";
        var last = "Okoye";

        var token = svc.GenerateToken(userId, email, first, last);

        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();

        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
        var validations = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidIssuer = "test-issuer",
            ValidateAudience = true,
            ValidAudience = "test-audience",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(5)
        };

        var principal = handler.ValidateToken(token, validations, out var validatedToken);
        validatedToken.Should().BeOfType<JwtSecurityToken>();

        var jwt = (JwtSecurityToken)validatedToken;
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
        jwt.Claims.Should().Contain(c => c.Type == "name" && c.Value == first + " " + last);
        jwt.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }
}
