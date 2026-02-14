using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using NaijaStake.API.Dtos;

namespace NaijaStake.Integration.Tests.Auth;

public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_Refresh_Revoke_Flow_Works()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing registrations for StakeItDbContext
                var toRemove = services.Where(d => d.ServiceType == typeof(Microsoft.EntityFrameworkCore.DbContextOptions<NaijaStake.Infrastructure.Data.StakeItDbContext>) || d.ServiceType == typeof(NaijaStake.Infrastructure.Data.StakeItDbContext)).ToList();
                foreach (var d in toRemove) services.Remove(d);

                // Use SQLite in-memory for tests
                var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
                connection.Open();
                services.AddDbContext<NaijaStake.Infrastructure.Data.StakeItDbContext>(options =>
                    options.UseSqlite(connection));

                // Build a provider to create the schema
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<NaijaStake.Infrastructure.Data.StakeItDbContext>();
                    db.Database.EnsureCreated();
                }
            });
        });
        var client = factory.CreateClient();

        // Arrange: create a user via UsersController
        var email = "test+int@local";
        var password = "P@ssw0rd!";
        var reg = new { Email = email, PasswordHash = password, FirstName = "Test", LastName = "User", PhoneNumber = "+1234567890" };
        var regResp = await client.PostAsJsonAsync("/api/users/register", reg);
        regResp.EnsureSuccessStatusCode();

        // Login
        var loginReq = new LoginRequestDto(email, password);
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", loginReq);
        loginResp.EnsureSuccessStatusCode();
        var loginObj = await loginResp.Content.ReadFromJsonAsync<NaijaStake.API.Dtos.LoginResponseDto>();
        if (loginObj == null) throw new System.Exception("Login response could not be deserialized");
        var refreshToken = loginObj.Refresh?.Token;
        refreshToken.Should().NotBeNullOrEmpty();

        // Refresh
        var refreshReq = new RefreshRequestDto(refreshToken!);
        var refreshResp = await client.PostAsJsonAsync("/api/auth/refresh", refreshReq);
        refreshResp.EnsureSuccessStatusCode();
        var refreshObj = await refreshResp.Content.ReadFromJsonAsync<RefreshResponseDto>();
        refreshObj.Should().NotBeNull();
        refreshObj!.AccessToken.Should().NotBeNullOrEmpty();

        // Revoke (requires auth) - use access token
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", refreshObj.AccessToken);
        var revokeReq = new RevokeRequestDto(refreshObj.RefreshToken);
        var revokeResp = await client.PostAsJsonAsync("/api/auth/revoke", revokeReq);
        revokeResp.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }
}
