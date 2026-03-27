using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for kiosk self-registration operations.
/// Handles first-time family registration at the kiosk, creating all related records
/// atomically and returning a search result ready for the check-in flow.
/// </summary>
public interface ICheckinRegistrationService
{
    /// <summary>
    /// Registers a new family at the kiosk in a single atomic transaction.
    /// Creates the family, parent person with phone number, and all child persons.
    /// Returns a <see cref="CheckinFamilySearchResultDto"/> so the kiosk can immediately
    /// proceed to member selection without a separate search round-trip.
    /// </summary>
    /// <param name="request">Kiosk registration request with parent and children details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created family, formatted as a check-in search result.</returns>
    Task<CheckinFamilySearchResultDto> RegisterFamilyAsync(
        KioskFamilyRegistrationRequest request,
        CancellationToken ct = default);
}
