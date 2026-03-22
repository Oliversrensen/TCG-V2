using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TCG.Server.Data;
using TCG.Server.Hubs;
using TCG.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Database (Neon PostgreSQL, or in-memory when no connection)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["NEON_DATABASE_URL"];
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<TcgDbContext>(opts =>
        opts.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<TcgDbContext>(opts =>
        opts.UseInMemoryDatabase("TcgDev"));
}

// Redis (optional - for matchmaking, match state, connection store)
var redisConnection = builder.Configuration["REDIS_URL"] ?? builder.Configuration["REDIS_CONNECTION"];
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(opts => opts.Configuration = redisConnection);
    builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
        StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnection!));
    builder.Services.AddSingleton<TCG.Core.Services.IMatchStateStore, RedisMatchStateStore>();
    builder.Services.AddSingleton<IMatchConnectionStore, RedisMatchConnectionStore>();
    builder.Services.AddSingleton<IMatchmakingQueue, RedisMatchmakingQueue>();
}
else
{
    builder.Services.AddSingleton<TCG.Core.Services.IMatchStateStore, MatchStateStore>();
    builder.Services.AddSingleton<IMatchConnectionStore, MatchConnectionStore>();
    builder.Services.AddSingleton<IMatchmakingQueue, InMemoryMatchmakingQueue>();
}

// Auth (Neon Auth JWT, or dev-only test header when NEON_AUTH_URL is empty)
var neonAuthUrl = builder.Configuration["NEON_AUTH_URL"]?.TrimEnd('/');
if (!string.IsNullOrEmpty(neonAuthUrl))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.Authority = neonAuthUrl;
            opts.MetadataAddress = $"{neonAuthUrl}/.well-known/openid-configuration";
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudience = neonAuthUrl,
                ValidateLifetime = true,
            };
            opts.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var path = ctx.Request.Path;
                    if (path.StartsWithSegments("/hubs") &&
                        ctx.Request.Query.TryGetValue("access_token", out var token))
                        ctx.Token = token;
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = ctx =>
                {
                    if (ctx.Exception is SecurityTokenInvalidSignatureException)
                        ctx.Response.Headers.Append("X-Auth-Error", "Invalid token signature");
                    return Task.CompletedTask;
                },
            };
        });
    builder.Services.AddAuthorization();
}
else
{
    // Dev fallback when NEON_AUTH_URL not set: accept X-User-Id or user_id query
    builder.Services.AddAuthentication(TCG.Server.Auth.DevTestAuthHandler.SchemeName)
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TCG.Server.Auth.DevTestAuthHandler>(TCG.Server.Auth.DevTestAuthHandler.SchemeName, null);
    builder.Services.AddAuthorization();
}

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddScoped<IDeckService, DeckService>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddScoped<IMatchmakingService, MatchmakingService>();
builder.Services.AddSingleton<TCG.GameLogic.IGameEngine, TCG.GameLogic.GameEngine>();

builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Ensure DB schema and seed
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TcgDbContext>();
    await db.Database.EnsureCreatedAsync();
    await CardDefinitionSeeder.SeedAsync(db);
}
catch (InvalidOperationException) { /* DbContext not registered */ }
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "Database init failed");
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GameHub>("/hubs/game");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
