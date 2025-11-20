namespace LunaQi.Api.Models;

// DTO
public sealed record UserDto(Guid Id, string Username, string Region, List<UserPhaseDto> UserPhases);
public sealed record UserPhaseDto(Guid PhaseDefinitionId, bool IsEnabled, string PhaseName, DateTimeOffset StartDate, DateTimeOffset EndDate);
public sealed record PhaseDefinitionDto(Guid Id, string Name, DateTimeOffset StartDate, DateTimeOffset EndDate);
// DTOs
public sealed record RegisterDto(string Username, string Password, string Region = "europe");
public sealed record LoginDto(string Username, string Password);
public sealed record TokenResponse(string AccessToken, string RefreshToken);
public sealed record RefreshDto(string RefreshToken);

// Models/RefreshToken.cs
public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Token { get; set; } = "";          // zufälliger, kryptographischer String
    public DateTimeOffset ExpiresAt { get; set; }    // z. B. +30 Tage
    public DateTimeOffset? RevokedAt { get; set; }   // Null = aktiv
    public string? CreatedByIp { get; set; }
}


public sealed class User
{
    public required Guid Id { get; set; }
    public required string Username { get; set; }
    public string Region { get; set; } = string.Empty;
    
    public string PasswordHash { get; set; } = "";
    
    public ICollection<UserPhase> UserPhases { get; set; } = new List<UserPhase>();
}

public sealed class PhaseDefinition
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required DateTimeOffset StartDate { get; set; }
    public required DateTimeOffset EndDate { get; set; }
}

public sealed class UserPhase
{
    public required Guid UserId { get; set; }
    public required Guid PhaseDefinitionId { get; set; }
    public required bool IsEnabled { get; set; }
    
    public User? User { get; set; }
    public PhaseDefinition? PhaseDefinition { get; set; }
}

