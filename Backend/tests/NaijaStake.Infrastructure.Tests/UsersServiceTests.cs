using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NaijaStake.Domain.Entities;
using NaijaStake.Infrastructure.Data;
using NaijaStake.Infrastructure.Repositories;
using NaijaStake.Infrastructure.Services;
using Xunit;

namespace NaijaStake.Infrastructure.Tests;

public class UsersServiceTests
{
    private StakeItDbContext CreateContext(SqliteConnection conn)
    {
        var options = new DbContextOptionsBuilder<StakeItDbContext>()
            .UseSqlite(conn)
            .Options;
        return new StakeItDbContext(options);
    }

    private class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => "fake-hash:" + password;
        public bool Verify(string hash, string password) => hash == Hash(password);
    }

    [Fact]
    public async Task CreateAsync_Hashes_And_Persists_User()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var userRepo = new UserRepository(db);
        var hasher = new FakeHasher();
        var svc = new UserService(userRepo, hasher);

        var email = "TEST@Example.com";
        var phone = "08000000000";
        var password = "P@ssw0rd";
        var first = "Jane";
        var last = "Doe";

        var user = await svc.CreateAsync(email, phone, password, first, last);

        user.Should().NotBeNull();
        user.Email.Should().Be(email.ToLowerInvariant());
        user.PasswordHash.Should().Be(hasher.Hash(password));

        var fromDb = await db.Users.FindAsync(user.Id);
        fromDb.Should().NotBeNull();
        fromDb!.Email.Should().Be(email.ToLowerInvariant());
    }

    [Fact]
    public async Task GetByEmailAsync_Returns_User()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var userRepo = new UserRepository(db);
        var hasher = new FakeHasher();
        var svc = new UserService(userRepo, hasher);

        var email = "someone@domain.com";
        var user = await svc.CreateAsync(email, "08011111111", "pwd", "A", "B");

        var found = await svc.GetByEmailAsync(email);
        found.Should().NotBeNull();
        found!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_User()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = CreateContext(conn);
        db.Database.EnsureCreated();

        var userRepo = new UserRepository(db);
        var hasher = new FakeHasher();
        var svc = new UserService(userRepo, hasher);

        var user = await svc.CreateAsync("idtest@x.com", "08022222222", "pwd2", "X", "Y");

        var found = await svc.GetByIdAsync(user.Id);
        found.Should().NotBeNull();
        found!.Email.Should().Be("idtest@x.com");
    }
}
