using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NaijaStake.API.Dtos;

namespace NaijaStake.Integration.Tests.Auth;

public class RefreshTokenEdgeCasesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RefreshTokenEdgeCasesTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> CreateFactoryWithSqlite()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var toRemove = services.Where(d => d.ServiceType == typeof(Microsoft.EntityFrameworkCore.DbContextOptions<NaijaStake.Infrastructure.Data.StakeItDbContext>) || d.ServiceType == typeof(NaijaStake.Infrastructure.Data.StakeItDbContext)).ToList();
                foreach (var d in toRemove) services.Remove(d);

                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();
                services.AddDbContext<NaijaStake.Infrastructure.Data.StakeItDbContext>(options => options.UseSqlite(connection));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NaijaStake.Infrastructure.Data.StakeItDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    public async Task Expired_RefreshToken_Returns_Unauthorized()
    {
        using var factory = CreateFactoryWithSqlite();
        var client = factory.CreateClient();

        // Register
        var email = "expired+int@local";
        var password = "P@ssw0rd!";
        var reg = new { Email = email, PasswordHash = password, FirstName = "E", LastName = "X", PhoneNumber = "+100" };
        var regResp = await client.PostAsJsonAsync("/api/users/register", reg);
        regResp.EnsureSuccessStatusCode();

        // Insert expired refresh token directly
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NaijaStake.Infrastructure.Data.StakeItDbContext>();
            var user = db.Users.First(u => u.Email == email);
            var token = NaijaStake.Domain.Entities.RefreshToken.Create(user.Id, Guid.NewGuid().ToString(), DateTime.UtcNow.AddMinutes(-10));
            db.RefreshTokens.Add(token);
            await db.SaveChangesAsync();

            // Attempt refresh
            var refreshReq = new RefreshRequestDto(token.Token);
            var refreshResp = await client.PostAsJsonAsync("/api/auth/refresh", refreshReq);
            refreshResp.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Revoked_RefreshToken_Returns_Unauthorized()
    {
        using var factory = CreateFactoryWithSqlite();
        var client = factory.CreateClient();

        // Register
        var email = "revoked+int@local";
        var password = "P@ssw0rd!";
        var reg = new { Email = email, PasswordHash = password, FirstName = "R", LastName = "V", PhoneNumber = "+101" };
        var regResp = await client.PostAsJsonAsync("/api/users/register", reg);
        regResp.EnsureSuccessStatusCode();

        // Create a refresh token then mark revoked in DB
        string tokenValue;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NaijaStake.Infrastructure.Data.StakeItDbContext>();
            var user = db.Users.First(u => u.Email == email);
            var token = NaijaStake.Domain.Entities.RefreshToken.Create(user.Id, Guid.NewGuid().ToString(), DateTime.UtcNow.AddMinutes(60));
            db.RefreshTokens.Add(token);
            await db.SaveChangesAsync();
            tokenValue = token.Token;

            // Revoke
            token.Revoke();
            await db.SaveChangesAsync();
        }

        var refreshReq = new RefreshRequestDto(tokenValue);
        var refreshResp = await client.PostAsJsonAsync("/api/auth/refresh", refreshReq);
        refreshResp.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reuse_Rotated_RefreshToken_Is_Not_Allowed()
    {
        using var factory = CreateFactoryWithSqlite();
        var client = factory.CreateClient();

        var email = "rotate+int@local";
        var password = "P@ssw0rd!";
        var reg = new { Email = email, PasswordHash = password, FirstName = "T", LastName = "R", PhoneNumber = "+102" };
        var regResp = await client.PostAsJsonAsync("/api/users/register", reg);
        regResp.EnsureSuccessStatusCode();

        // Login to obtain initial refresh token
        var loginReq = new LoginRequestDto(email, password);
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", loginReq);
        loginResp.EnsureSuccessStatusCode();
        var loginObj = await loginResp.Content.ReadFromJsonAsync<LoginResponseDto>();
        loginObj.Should().NotBeNull();
        var originalToken = loginObj!.Refresh.Token;

        // First refresh -> rotates token
        var refreshReq1 = new RefreshRequestDto(originalToken);
        var refreshResp1 = await client.PostAsJsonAsync("/api/auth/refresh", refreshReq1);
        refreshResp1.EnsureSuccessStatusCode();
        var refreshObj1 = await refreshResp1.Content.ReadFromJsonAsync<RefreshResponseDto>();
        refreshObj1.Should().NotBeNull();

        // Try using original token again -> should be unauthorized
        var refreshReq2 = new RefreshRequestDto(originalToken);
        var refreshResp2 = await client.PostAsJsonAsync("/api/auth/refresh", refreshReq2);
        refreshResp2.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoke_By_Wrong_User_Returns_Forbid()
    {
        using var factory = CreateFactoryWithSqlite();
        var client = factory.CreateClient();

        // Register user A
        var emailA = "userA+int@local";
        var pwA = "P@ssw0rd!";
        var regA = new { Email = emailA, PasswordHash = pwA, FirstName = "A", LastName = "One", PhoneNumber = "+110" };
        (await client.PostAsJsonAsync("/api/users/register", regA)).EnsureSuccessStatusCode();

        // Register user B
        var emailB = "userB+int@local";
        var pwB = "P@ssw0rd!";
        var regB = new { Email = emailB, PasswordHash = pwB, FirstName = "B", LastName = "Two", PhoneNumber = "+111" };
        (await client.PostAsJsonAsync("/api/users/register", regB)).EnsureSuccessStatusCode();

        // Login A and B
        var loginA = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(emailA, pwA));
        loginA.EnsureSuccessStatusCode();
        var loginObjA = await loginA.Content.ReadFromJsonAsync<LoginResponseDto>();

        var loginB = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(emailB, pwB));
        loginB.EnsureSuccessStatusCode();
        var loginObjB = await loginB.Content.ReadFromJsonAsync<LoginResponseDto>();

        loginObjA.Should().NotBeNull();
        loginObjB.Should().NotBeNull();

        // Attempt to revoke A's refresh token using B's access token
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginObjB!.Access.AccessToken);
        var revokeResp = await client.PostAsJsonAsync("/api/auth/revoke", new RevokeRequestDto(loginObjA!.Refresh.Token));
        revokeResp.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }
}
