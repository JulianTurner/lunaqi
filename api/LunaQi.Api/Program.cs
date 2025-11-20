using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using LunaQi.Api.Data;
using LunaQi.Api.Helper;
using LunaQi.Api.Models;
using LunaQi.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;

Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<LunaQiDbContext>();
builder.Services.AddSingleton<AuthService>();

var jwtSecret = builder.Configuration["Jwt:Secret"]!;
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer   = builder.Configuration["Jwt:Issuer"]   ?? "LunaQi",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "LunaQiClient",
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });


builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LunaQiDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}


app.UseDefaultFiles();
app.UseStaticFiles();


app.MapGet("/debug/jwt", (HttpRequest req, IConfiguration cfg) =>
{
    var raw = req.Headers.Authorization.ToString();
    if (!raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("No Bearer header");

    var token = raw[7..].Trim();
    var tvp = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer   = cfg["Jwt:Issuer"]   ?? "LunaQi",
        ValidAudience = cfg["Jwt:Audience"] ?? "LunaQiClient",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Secret"]!)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    try
    {
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, tvp, out var validated);
        var jwt = (JwtSecurityToken)validated;

        return Results.Ok(new {
            iss = jwt.Issuer,
            aud = jwt.Audiences,
            alg = jwt.Header.Alg,
            iat = jwt.IssuedAt,
            nbf = jwt.ValidFrom,
            exp = jwt.ValidTo,
            name = principal.Identity?.Name,
            sub  = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.GetType().Name + ": " + ex.Message);
    }
});


app.UseAuthentication();
app.UseAuthorization();

// Debug endpoint that runs the same authentication pipeline the middleware uses
app.MapGet("/debug/auth", async (HttpContext ctx) =>
{
    var result = await ctx.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

    if (!result.Succeeded)
    {
        return Results.BadRequest(new {
            succeeded = false,
            failure = result.Failure?.ToString() ?? "<no exception>",
            failureType = result.Failure?.GetType().FullName,
            properties = result.Properties?.Items
        });
    }

    var claims = result.Principal?.Claims.Select(c => new { c.Type, c.Value }).ToList();
    return Results.Ok(new {
        succeeded = true,
        authenticationScheme = result.Ticket?.AuthenticationScheme,
        claims
    });
});

// Auth-Endpunkte
app.MapPost("/auth/register", async (RegisterDto dto, LunaQiDbContext db, AuthService auth) =>
{
    if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest("Username/Password erforderlich.");

    var exists = await db.Users.AnyAsync(u => u.Username == dto.Username);
    if (exists) return Results.Conflict("Username bereits vergeben.");

    var user = new User
    {
        Id = Guid.NewGuid(),
        Username = dto.Username,
        Region = dto.Region,
        PasswordHash = auth.HashPassword(dto.Password),
    };
    db.Users.Add(user);

    // sofort Refresh-Token ausstellen (optional)
    var rt = new RefreshToken
    {
        UserId = user.Id,
        Token = AuthService.GenerateRefreshToken(),
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(int.Parse(app.Configuration["Jwt:RefreshTokenDays"] ?? "30"))
    };
    db.RefreshTokens.Add(rt);
    await db.SaveChangesAsync();

    var access = auth.CreateAccessToken(user);
    return Results.Ok(new TokenResponse(access, rt.Token));
});

app.MapPost("/auth/login", async (LoginDto dto, LunaQiDbContext db, AuthService auth, HttpContext ctx) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
    if (user is null) return Results.Unauthorized();

    if (!auth.VerifyPassword(dto.Password, user.PasswordHash))
        return Results.Unauthorized();

    var access = auth.CreateAccessToken(user);

    var rt = new RefreshToken
    {
        UserId = user.Id,
        Token = AuthService.GenerateRefreshToken(),
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(int.Parse(app.Configuration["Jwt:RefreshTokenDays"] ?? "30")),
        CreatedByIp = ctx.Connection.RemoteIpAddress?.ToString()
    };
    db.RefreshTokens.Add(rt);
    await db.SaveChangesAsync();

    return Results.Ok(new TokenResponse(access, rt.Token));
});

app.MapPost("/auth/refresh", async (RefreshDto dto, LunaQiDbContext db, AuthService auth) =>
{
    var now = DateTimeOffset.UtcNow;
    var stored = await db.RefreshTokens
        .Include(r => r.User)
        .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken);

    if (stored is null || stored.RevokedAt != null || stored.ExpiresAt <= now || stored.User is null)
        return Results.Unauthorized();

    var access = auth.CreateAccessToken(stored.User);

    // optional: Rotating Refresh Token
    stored.RevokedAt = now;
    var next = new RefreshToken
    {
        UserId = stored.UserId,
        Token = AuthService.GenerateRefreshToken(),
        ExpiresAt = now.AddDays(int.Parse(app.Configuration["Jwt:RefreshTokenDays"] ?? "30"))
    };
    db.RefreshTokens.Add(next);
    await db.SaveChangesAsync();

    return Results.Ok(new TokenResponse(access, next.Token));
});

app.MapPost("/auth/logout", async (RefreshDto dto, LunaQiDbContext db) =>
{
    var stored = await db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == dto.RefreshToken);
    if (stored is null) return Results.Ok(); // idempotent

    stored.RevokedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok();
});


app.MapGet("/me", async (LunaQiDbContext db, ClaimsPrincipal user) =>
{
    var username = user.Identity?.Name;
    if (string.IsNullOrWhiteSpace(username))
        return Results.Unauthorized();

    var me = await db.Users
        .AsNoTracking()
        .Where(u => u.Username == username)
        .Select(u => new UserDto(
            u.Id,
            u.Username,
            u.Region,
            u.UserPhases.Select(up => new UserPhaseDto(
                up.PhaseDefinitionId,
                up.IsEnabled,
                up.PhaseDefinition != null ? up.PhaseDefinition.Name : string.Empty,
                up.PhaseDefinition != null ? up.PhaseDefinition.StartDate : DateTimeOffset.MinValue,
                up.PhaseDefinition != null ? up.PhaseDefinition.EndDate : DateTimeOffset.MinValue
            )).ToList()
        ))
        .FirstOrDefaultAsync();

    return me is not null ? Results.Ok(me) : Results.Unauthorized();
}).RequireAuthorization();


app.MapGet("/api/users", async (LunaQiDbContext db) =>
    await db.Users
        .AsNoTracking()
        .Select(user => new UserDto(
            user.Id,
            user.Username,
            user.Region,
            user.UserPhases.Select(up => new UserPhaseDto(
                up.PhaseDefinitionId,
                up.IsEnabled,
                up.PhaseDefinition != null ? up.PhaseDefinition.Name : string.Empty,
                up.PhaseDefinition != null ? up.PhaseDefinition.StartDate : DateTimeOffset.MinValue,
                up.PhaseDefinition != null ? up.PhaseDefinition.EndDate : DateTimeOffset.MinValue
            )).ToList()
        ))
        .ToListAsync()
).RequireAuthorization();

app.MapGet("/api/phasedefinitions", async (LunaQiDbContext db) =>
    await db.PhaseDefinitions
        .AsNoTracking()
        .Select(pd => new PhaseDefinitionDto(
            pd.Id,
            pd.Name,
            pd.StartDate,
            pd.EndDate
        ))
        .ToListAsync());

// csharp
app.MapGet("/api/users/{id}/phases", async (Guid id, LunaQiDbContext db) =>
{
    var phases = await db.UserPhases
        .AsNoTracking()
        .Where(up => up.UserId == id)
        .Select(up => new UserPhaseDto(
            up.PhaseDefinitionId,
            up.IsEnabled,
            up.PhaseDefinition != null ? up.PhaseDefinition.Name : string.Empty,
            up.PhaseDefinition != null ? up.PhaseDefinition.StartDate : DateTimeOffset.MinValue,
            up.PhaseDefinition != null ? up.PhaseDefinition.EndDate : DateTimeOffset.MinValue
        ))
        .ToListAsync();

    return phases.Count > 0 ? Results.Ok(phases) : Results.NotFound();
}).RequireAuthorization();



// SPA-Fallback (für Routing in Angular)
app.MapFallbackToFile("index.html");

app.Run();