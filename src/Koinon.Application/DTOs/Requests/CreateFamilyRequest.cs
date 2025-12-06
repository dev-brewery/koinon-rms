namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to create a new family group.
/// </summary>
public record CreateFamilyRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? CampusId { get; init; }
    public CreateFamilyAddressRequest? Address { get; init; }
}

/// <summary>
/// Request to add a member to a family.
/// </summary>
public record AddFamilyMemberRequest
{
    public required string PersonId { get; init; }
    public required string RoleId { get; init; }
}

/// <summary>
/// Request to update a family's address.
/// </summary>
public record UpdateFamilyAddressRequest
{
    public string? Street1 { get; init; }
    public string? Street2 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
}

/// <summary>
/// Request to create an address for a family.
/// </summary>
public record CreateFamilyAddressRequest
{
    public required string Street1 { get; init; }
    public string? Street2 { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string PostalCode { get; init; }
    public string? Country { get; init; }
}
