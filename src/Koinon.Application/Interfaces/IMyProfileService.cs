using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for self-service profile management.
/// Allows authenticated users to view and update their own profile and family information.
/// </summary>
public interface IMyProfileService
{
    /// <summary>
    /// Gets the current user's profile details.
    /// </summary>
    Task<Result<MyProfileDto>> GetMyProfileAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates the current user's profile with restricted fields.
    /// Users can only update safe fields: Email, PhoneNumbers, NickName, EmailPreference.
    /// </summary>
    Task<Result<MyProfileDto>> UpdateMyProfileAsync(
        UpdateMyProfileRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current user's family members.
    /// </summary>
    Task<Result<IReadOnlyList<MyFamilyMemberDto>>> GetMyFamilyAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates a family member's information (limited fields).
    /// Only allowed if current user is an adult in the family AND target person is a child.
    /// </summary>
    Task<Result<MyFamilyMemberDto>> UpdateFamilyMemberAsync(
        string personIdKey,
        UpdateFamilyMemberRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current user's involvement (groups and attendance summary).
    /// </summary>
    Task<Result<MyInvolvementDto>> GetMyInvolvementAsync(CancellationToken ct = default);
}
