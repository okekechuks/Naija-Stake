using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NaijaStake.Infrastructure.Data;
using Xunit;

namespace NaijaStake.Integration.Tests.Fixtures;

/// <summary>
/// Base fixture for integration tests using SQLite in-memory database.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private readonly DbContextOptions<StakeItDbContext> _options;
    private SqliteConnection? _connection;
    public StakeItDbContext Context { get; private set; } = null!;

    public DatabaseFixture()
    {
        // We'll create the connection at runtime and keep it open for the lifetime
        // of the fixture so SQLite in-memory retains schema between connections.
        _options = new DbContextOptionsBuilder<StakeItDbContext>()
            .Options;
    }

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var optionsBuilder = new DbContextOptionsBuilder<StakeItDbContext>()
            .UseSqlite(_connection);

        Context = new StakeItDbContext(optionsBuilder.Options);
        await Context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (Context != null)
        {
            await Context.DisposeAsync();
            Context = null!;
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
            _connection = null;
        }
    }

    public async Task ResetAsync()
    {
        await DisposeAsync();
        await InitializeAsync();
    }
}
