using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NaijaStake.Infrastructure.Configuration;
using NaijaStake.Infrastructure.Data;
using NaijaStake.Infrastructure.Repositories;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add user secrets for development (secrets are stored outside the project)
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Load JWT SecretKey from environment variable (takes precedence over config)
var jwtSecretKey = builder.Configuration["JWT_SECRET_KEY"] 
    ?? builder.Configuration["JwtSettings:SecretKey"];

// Configuration
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() 
    ?? throw new InvalidOperationException("JwtSettings configuration is missing.");

// Override SecretKey from environment variable if provided
if (!string.IsNullOrWhiteSpace(jwtSecretKey))
{
    jwtSettings.SecretKey = jwtSecretKey;
}

// Validate JWT settings
jwtSettings.Validate();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var redisConnection = builder.Configuration.GetConnectionString("Redis:ConnectionString");

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<StakeItDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
    }));

// Redis (only initialize when configured)
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    var redisOptions = ConfigurationOptions.Parse(redisConnection);
    var redis = ConnectionMultiplexer.Connect(redisOptions);
    builder.Services.AddSingleton(redis);
    builder.Services.AddSingleton(redis.GetDatabase());
}

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IBetRepository, BetRepository>();
builder.Services.AddScoped<IStakeRepository, StakeRepository>();

// Application services
builder.Services.AddScoped<NaijaStake.Infrastructure.Services.IUserService, NaijaStake.Infrastructure.Services.UserService>();
builder.Services.AddScoped<NaijaStake.Infrastructure.Services.IWalletService, NaijaStake.Infrastructure.Services.WalletService>();
builder.Services.AddScoped<NaijaStake.Infrastructure.Services.ITransactionService, NaijaStake.Infrastructure.Services.TransactionService>();
builder.Services.AddScoped<NaijaStake.Infrastructure.Services.IBetService, NaijaStake.Infrastructure.Services.BetService>();
builder.Services.AddScoped<NaijaStake.Infrastructure.Services.IStakeService, NaijaStake.Infrastructure.Services.StakeService>();
// Token service for JWT generation
builder.Services.AddSingleton<NaijaStake.Infrastructure.Services.ITokenService, NaijaStake.Infrastructure.Services.TokenService>();
builder.Services.AddScoped<NaijaStake.Infrastructure.Services.IPasswordHasher, NaijaStake.Infrastructure.Services.BcryptPasswordHasher>();
builder.Services.AddScoped<NaijaStake.Infrastructure.Services.IRefreshTokenService, NaijaStake.Infrastructure.Services.RefreshTokenService>();
// Background cleanup for refresh tokens
builder.Services.AddHostedService<NaijaStake.API.Services.RefreshTokenCleanupService>();

// Register JwtSettings as a singleton for dependency injection
builder.Services.AddSingleton(jwtSettings);

// Authentication
var secretKey = Encoding.ASCII.GetBytes(jwtSettings.SecretKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// SignalR for real-time updates
builder.Services.AddSignalR();

// Configure host to not stop on background service exceptions (for development resilience)
builder.Services.Configure<Microsoft.Extensions.Hosting.HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = Microsoft.Extensions.Hosting.BackgroundServiceExceptionBehavior.Ignore;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Database migrations on startup (development only)
if (app.Environment.IsDevelopment())
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StakeItDbContext>();
            await db.Database.MigrateAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetService<Microsoft.Extensions.Logging.ILogger<Program>>();
        logger?.LogWarning(ex, "Skipping DB migrations (likely running in test environment without DB).");
    }
}

app.Run();
