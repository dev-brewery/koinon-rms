namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to register a new family at a kiosk during first-time check-in.
/// Collects only the minimal information needed at a touch-screen kiosk under time pressure.
/// </summary>
public record KioskFamilyRegistrationRequest
{
    public string ParentFirstName { get; init; } = default!;
    public string ParentLastName { get; init; } = default!;

    /// <summary>
    /// 10-digit US phone number. Formatting characters (dashes, parens, spaces) are stripped
    /// before storage. The normalized form is used for check-in search.
    /// </summary>
    public string PhoneNumber { get; init; } = default!;

    public List<KioskChildRegistrationRequest> Children { get; init; } = new();
}

/// <summary>
/// Minimal child information collected at the kiosk.
/// </summary>
public record KioskChildRegistrationRequest
{
    public string FirstName { get; init; } = default!;

    /// <summary>
    /// Optional. Defaults to the parent's last name if not provided.
    /// </summary>
    public string? LastName { get; init; }

    public DateOnly? BirthDate { get; init; }
}
