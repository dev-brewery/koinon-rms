namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request DTO for updating the current user's profile.
/// Only allows updating "safe" fields - does not allow changing status or role fields.
/// </summary>
public record UpdateMyProfileRequest
{
    /// <summary>
    /// Email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Email communication preference.
    /// </summary>
    public string? EmailPreference { get; init; }

    /// <summary>
    /// Preferred name or nickname.
    /// </summary>
    public string? NickName { get; init; }

    /// <summary>
    /// Phone numbers to update (replaces existing).
    /// </summary>
    public IReadOnlyList<UpdatePhoneNumberRequest>? PhoneNumbers { get; init; }
}

/// <summary>
/// Request DTO for updating a phone number.
/// </summary>
public record UpdatePhoneNumberRequest
{
    /// <summary>
    /// IdKey of existing phone number (null if creating new).
    /// </summary>
    public string? IdKey { get; init; }

    /// <summary>
    /// Phone number (digits only).
    /// </summary>
    public required string Number { get; init; }

    /// <summary>
    /// Phone extension.
    /// </summary>
    public string? Extension { get; init; }

    /// <summary>
    /// Phone type IdKey (Mobile, Home, Work).
    /// </summary>
    public string? PhoneTypeIdKey { get; init; }

    /// <summary>
    /// Whether SMS messaging is enabled for this number.
    /// </summary>
    public bool IsMessagingEnabled { get; init; }

    /// <summary>
    /// Whether this phone number should be hidden from directories.
    /// </summary>
    public bool IsUnlisted { get; init; }
}
