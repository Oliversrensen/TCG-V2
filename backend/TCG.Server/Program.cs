using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TCG.Server.Auth;
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

// Auth: Neon Auth JWT (uses JWKS; no OIDC discovery)
var neonAuthUrl = builder.Configuration["NEON_AUTH_URL"]?.TrimEnd('/')
    ?? throw new InvalidOperationException("NEON_AUTH_URL is required. Set it in appsettings.json or environment.");
var jwksUri = $"{neonAuthUrl}/.well-known/jwks.json";

// Fetch JWKS at startup; use ScottBrady to load EdDSA keys (Microsoft.IdentityModel doesn't support EdDSA)
using var http = new HttpClient();
var jwksJson = await http.GetStringAsync(jwksUri);
var signingKeys = NeonEdDsaKeyLoader.LoadFromJwks(jwksJson);
if (signingKeys.Count == 0)
    throw new InvalidOperationException("No EdDSA signing keys from JWKS. Check NEON_AUTH_URL.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.Authority = null;
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKeys = signingKeys,
            ValidateIssuerSigningKey = true,
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
                var logger = ctx.HttpContext.RequestServices.GetService<ILogger<Program>>();
                logger?.LogWarning("JWT auth failed: {Ex}", ctx.Exception?.Message);
                if (ctx.Exception is SecurityTokenInvalidSignatureException)
                    ctx.Response.Headers.Append("X-Auth-Error", "Invalid token signature");
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddSignalR();
builder.Services.AddScoped<IDeckService, DeckService>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddScoped<IGameSetupService, GameSetupService>();
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

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GameHub>("/hubs/game");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
