namespace Koinon.Application.DTOs;

/// <summary>
/// Enhanced group member DTO with contact information.
/// Contact details (Email, Phone) are only populated for group leaders.
/// </summary>
public record GroupMemberDetailDto
{
    public required string IdKey { get; init; }
    public required string PersonIdKey { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? PhotoUrl { get; init; }
    public int? Age { get; init; }
    public required string Gender { get; init; }
    public required GroupTypeRoleDto Role { get; init; }
    public required string Status { get; init; }
    public DateTime? DateTimeAdded { get; init; }
    public DateTime? InactiveDateTime { get; init; }
    public string? Note { get; init; }
}
