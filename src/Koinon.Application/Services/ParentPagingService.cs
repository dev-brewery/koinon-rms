using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service implementation for parent paging operations.
/// Manages pager assignments and SMS notifications to parents during check-in.
/// </summary>
public class ParentPagingService(
    IApplicationDbContext context,
    ISmsService smsService,
    ILogger<ParentPagingService> logger) : IParentPagingService
{
    private const int PagerNumberStart = 100;
    private const int MaxPagesPerHour = 3;

    public async Task<PagerAssignmentDto> AssignPagerAsync(int attendanceId, int? campusId, CancellationToken ct = default)
    {
        logger.LogInformation("Assigning pager for attendance {AttendanceId}", attendanceId);

        // Get the attendance record with related data
        var attendance = await context.Attendances
            .Include(a => a.Occurrence)
                .ThenInclude(o => o!.Group)
            .Include(a => a.Occurrence)
                .ThenInclude(o => o!.Location)
            .Include(a => a.PersonAlias)
                .ThenInclude(pa => pa!.Person)
            .FirstOrDefaultAsync(a => a.Id == attendanceId, ct);

        if (attendance == null)
        {
            throw new InvalidOperationException($"Attendance record {attendanceId} not found");
        }

        // Get the next pager number
        var pagerNumber = await GetNextPagerNumberAsync(campusId, attendance.StartDateTime.Date, ct);

        // Get location ID from attendance occurrence
        var locationId = attendance.Occurrence?.LocationId;

        // Create pager assignment
        // Note: CreatedDateTime set explicitly because this service bypasses Repository<T>
        // which normally handles IAuditable timestamp initialization
        var pagerAssignment = new PagerAssignment
        {
            AttendanceId = attendanceId,
            PagerNumber = pagerNumber,
            CampusId = campusId,
            LocationId = locationId,
            CreatedDateTime = DateTime.UtcNow
        };

        context.PagerAssignments.Add(pagerAssignment);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Assigned pager {PagerNumber} to attendance {AttendanceId}", pagerNumber, attendanceId);

        // Build DTO
        var childName = attendance.PersonAlias?.Person?.FullName ?? "Unknown";
        var groupName = attendance.Occurrence?.Group?.Name ?? "Unknown";
        var locationName = attendance.Occurrence?.Location?.Name ?? "Unknown";

        // Get parent phone number from family
        var parentPhoneNumber = await GetParentPhoneNumberAsync(attendance.PersonAlias?.PersonId, ct);

        return new PagerAssignmentDto(
            IdKey: IdKeyHelper.Encode(pagerAssignment.Id),
            PagerNumber: pagerNumber,
            AttendanceIdKey: IdKeyHelper.Encode(attendanceId),
            ChildName: childName,
            GroupName: groupName,
            LocationName: locationName,
            ParentPhoneNumber: parentPhoneNumber,
            CheckedInAt: attendance.StartDateTime,
            MessagesSentCount: 0
        );
    }

    public async Task<Result<PagerMessageDto>> SendPageAsync(SendPageRequest request, int sentByPersonId, CancellationToken ct = default)
    {
        logger.LogInformation("Sending page to pager {PagerNumber}", request.PagerNumber);

        // Parse pager number (handle "P-127" or "127" format)
        var pagerNumberStr = request.PagerNumber.Replace("P-", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (!int.TryParse(pagerNumberStr, out int pagerNumber))
        {
            return Result<PagerMessageDto>.Failure(Error.UnprocessableEntity($"Invalid pager number format: {request.PagerNumber}"));
        }

        // Find the pager assignment for today
        var today = DateTime.UtcNow.Date;
        var pagerAssignment = await context.PagerAssignments
            .Include(pa => pa.Attendance)
                .ThenInclude(a => a!.PersonAlias)
                    .ThenInclude(pa => pa!.Person)
            .Include(pa => pa.Attendance)
                .ThenInclude(a => a!.Occurrence)
                    .ThenInclude(o => o!.Location)
            .Include(pa => pa.Messages)
            .FirstOrDefaultAsync(pa =>
                pa.PagerNumber == pagerNumber &&
                pa.CreatedDateTime.Date == today,
                ct);

        if (pagerAssignment == null)
        {
            return Result<PagerMessageDto>.Failure(Error.NotFound("Pager", request.PagerNumber));
        }

        // Rate limiting: Check if 3+ messages sent in last hour
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var recentMessagesCount = pagerAssignment.Messages
            .Count(m => m.SentDateTime.HasValue && m.SentDateTime.Value >= oneHourAgo);

        if (recentMessagesCount >= MaxPagesPerHour)
        {
            return Result<PagerMessageDto>.Failure(
                Error.UnprocessableEntity($"Rate limit exceeded: Maximum {MaxPagesPerHour} pages per hour allowed"));
        }

        // Validate custom message
        if (request.MessageType == PagerMessageType.Custom && string.IsNullOrWhiteSpace(request.CustomMessage))
        {
            return Result<PagerMessageDto>.Failure(
                Error.UnprocessableEntity("Custom message text is required when MessageType is Custom"));
        }

        // Get parent phone number
        var parentPhoneNumber = await GetParentPhoneNumberAsync(pagerAssignment.Attendance?.PersonAlias?.PersonId, ct);
        if (string.IsNullOrWhiteSpace(parentPhoneNumber))
        {
            return Result<PagerMessageDto>.Failure(
                Error.UnprocessableEntity("No parent phone number found for this child"));
        }

        // Build message from template or use custom
        var childName = pagerAssignment.Attendance?.PersonAlias?.Person?.FullName ?? "your child";
        var locationName = pagerAssignment.Attendance?.Occurrence?.Location?.Name ?? "the classroom";
        var messageText = BuildMessageText(request.MessageType, request.CustomMessage, childName, locationName);

        // Send SMS
        SmsResult smsResult;
        PagerMessageStatus status;
        string? twilioSid = null;
        string? failureReason = null;

        if (!smsService.IsConfigured)
        {
            logger.LogWarning("SMS service not configured - creating pager message record with Failed status");
            status = PagerMessageStatus.Failed;
            failureReason = "SMS service is not configured";
            smsResult = new SmsResult(false, null, failureReason);
        }
        else
        {
            try
            {
                smsResult = await smsService.SendSmsAsync(parentPhoneNumber, messageText, ct);

                if (smsResult.Success)
                {
                    status = PagerMessageStatus.Sent;
                    twilioSid = smsResult.MessageId;
                }
                else
                {
                    status = PagerMessageStatus.Failed;
                    failureReason = smsResult.ErrorMessage;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending SMS to {PhoneNumber}", parentPhoneNumber);
                status = PagerMessageStatus.Failed;
                failureReason = ex.Message;
                smsResult = new SmsResult(false, null, ex.Message);
            }
        }

        // Get sent by person name
        var sentByPerson = await context.People.FindAsync(new object[] { sentByPersonId }, ct);
        var sentByPersonName = sentByPerson?.FullName ?? "Unknown";

        // Create PagerMessage record
        // Note: CreatedDateTime set explicitly because this service bypasses Repository<T>
        // which normally handles IAuditable timestamp initialization
        var pagerMessage = new PagerMessage
        {
            PagerAssignmentId = pagerAssignment.Id,
            SentByPersonId = sentByPersonId,
            MessageType = request.MessageType,
            MessageText = messageText,
            PhoneNumber = parentPhoneNumber,
            TwilioMessageSid = twilioSid,
            Status = status,
            SentDateTime = DateTime.UtcNow,
            FailureReason = failureReason,
            CreatedDateTime = DateTime.UtcNow
        };

        context.PagerMessages.Add(pagerMessage);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Page sent to {PhoneNumber} for pager {PagerNumber}, Status: {Status}",
            parentPhoneNumber, pagerNumber, status);

        var dto = new PagerMessageDto(
            IdKey: IdKeyHelper.Encode(pagerMessage.Id),
            MessageType: request.MessageType,
            MessageText: messageText,
            Status: status,
            SentDateTime: pagerMessage.SentDateTime ?? DateTime.UtcNow,
            DeliveredDateTime: pagerMessage.DeliveredDateTime,
            SentByPersonName: sentByPersonName
        );

        return Result<PagerMessageDto>.Success(dto);
    }

    public async Task<List<PagerAssignmentDto>> SearchPagerAsync(PageSearchRequest request, CancellationToken ct = default)
    {
        var date = request.Date?.Date ?? DateTime.UtcNow.Date;

        var query = context.PagerAssignments
            .Include(pa => pa.Attendance)
                .ThenInclude(a => a!.PersonAlias)
                    .ThenInclude(pa => pa!.Person)
                        .ThenInclude(p => p!.PrimaryFamily)
                            .ThenInclude(f => f!.Members)
                                .ThenInclude(m => m.Person)
                                    .ThenInclude(p => p!.PhoneNumbers)
            .Include(pa => pa.Attendance)
                .ThenInclude(a => a!.PersonAlias)
                    .ThenInclude(pa => pa!.Person)
                        .ThenInclude(p => p!.PrimaryFamily)
                            .ThenInclude(f => f!.Members)
                                .ThenInclude(m => m.GroupRole)
            .Include(pa => pa.Attendance)
                .ThenInclude(a => a!.Occurrence)
                    .ThenInclude(o => o!.Group)
            .Include(pa => pa.Attendance)
                .ThenInclude(a => a!.Occurrence)
                    .ThenInclude(o => o!.Location)
            .Include(pa => pa.Messages)
            .Where(pa => pa.CreatedDateTime.Date == date);

        // Filter by campus if provided
        if (request.CampusId.HasValue)
        {
            query = query.Where(pa => pa.CampusId == request.CampusId.Value);
        }

        // Filter by search term (pager number or child name)
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();

            // Try to parse as pager number
            if (int.TryParse(searchTerm.Replace("P-", "", StringComparison.OrdinalIgnoreCase), out int pagerNumber))
            {
                query = query.Where(pa => pa.PagerNumber == pagerNumber);
            }
            else
            {
                // Search by child name (case-insensitive)
                // Use ToLower() on both pattern and columns - EF Core translates to SQL LOWER()
                var searchPattern = $"%{searchTerm.ToLower()}%";
                query = query.Where(pa =>
                    EF.Functions.Like(pa.Attendance!.PersonAlias!.Person!.FirstName.ToLower(), searchPattern) ||
                    EF.Functions.Like(pa.Attendance!.PersonAlias!.Person!.LastName.ToLower(), searchPattern) ||
                    (pa.Attendance!.PersonAlias!.Person!.NickName != null &&
                     EF.Functions.Like(pa.Attendance!.PersonAlias!.Person!.NickName.ToLower(), searchPattern))
                );
            }
        }

        var assignments = await query
            .OrderByDescending(pa => pa.CreatedDateTime)
            .ToListAsync(ct);

        // Extract phone numbers inline to avoid N+1 queries
        var results = assignments.Select(assignment =>
        {
            var childName = assignment.Attendance?.PersonAlias?.Person?.FullName ?? "Unknown";
            var groupName = assignment.Attendance?.Occurrence?.Group?.Name ?? "Unknown";
            var locationName = assignment.Attendance?.Occurrence?.Location?.Name ?? "Unknown";

            // Get parent phone number from already-loaded family data
            var parentPhoneNumber = GetParentPhoneNumberFromLoadedData(assignment.Attendance?.PersonAlias?.Person);

            return new PagerAssignmentDto(
                IdKey: IdKeyHelper.Encode(assignment.Id),
                PagerNumber: assignment.PagerNumber,
                AttendanceIdKey: IdKeyHelper.Encode(assignment.AttendanceId),
                ChildName: childName,
                GroupName: groupName,
                LocationName: locationName,
                ParentPhoneNumber: parentPhoneNumber,
                CheckedInAt: assignment.Attendance?.StartDateTime ?? assignment.CreatedDateTime,
                MessagesSentCount: assignment.Messages.Count
            );
        }).ToList();

        return results;
    }

    public async Task<PageHistoryDto?> GetPageHistoryAsync(int pagerNumber, DateTime? date, CancellationToken ct = default)
    {
        var searchDate = date?.Date ?? DateTime.UtcNow.Date;

        var pagerAssignment = await context.PagerAssignments
            .Include(pa => pa.Attendance)
                .ThenInclude(a => a!.PersonAlias)
                    .ThenInclude(pa => pa!.Person)
            .Include(pa => pa.Messages)
                .ThenInclude(m => m.SentByPerson)
            .FirstOrDefaultAsync(pa =>
                pa.PagerNumber == pagerNumber &&
                pa.CreatedDateTime.Date == searchDate,
                ct);

        if (pagerAssignment == null)
        {
            return null;
        }

        var childName = pagerAssignment.Attendance?.PersonAlias?.Person?.FullName ?? "Unknown";
        var parentPhoneNumber = await GetParentPhoneNumberAsync(pagerAssignment.Attendance?.PersonAlias?.PersonId, ct);

        var messages = pagerAssignment.Messages
            .OrderBy(m => m.SentDateTime)
            .Select(m => new PagerMessageDto(
                IdKey: IdKeyHelper.Encode(m.Id),
                MessageType: m.MessageType,
                MessageText: m.MessageText,
                Status: m.Status,
                SentDateTime: m.SentDateTime ?? m.CreatedDateTime,
                DeliveredDateTime: m.DeliveredDateTime,
                SentByPersonName: m.SentByPerson?.FullName ?? "Unknown"
            ))
            .ToList();

        return new PageHistoryDto(
            IdKey: IdKeyHelper.Encode(pagerAssignment.Id),
            PagerNumber: pagerNumber,
            ChildName: childName,
            ParentPhoneNumber: parentPhoneNumber ?? "Unknown",
            Messages: messages
        );
    }

    public async Task<int> GetNextPagerNumberAsync(int? campusId, DateTime date, CancellationToken ct = default)
    {
        var dateOnly = date.Date;

        var maxPagerNumber = await context.PagerAssignments
            .Where(pa => pa.CreatedDateTime.Date == dateOnly && pa.CampusId == campusId)
            .MaxAsync(pa => (int?)pa.PagerNumber, ct);

        return maxPagerNumber.HasValue ? maxPagerNumber.Value + 1 : PagerNumberStart;
    }

    /// <summary>
    /// Gets the parent/guardian phone number from already-loaded Person entity (used to avoid N+1).
    /// Looks for SMS-enabled mobile numbers from adult family members.
    /// </summary>
    private static string? GetParentPhoneNumberFromLoadedData(Person? person)
    {
        if (person?.PrimaryFamily == null)
        {
            return null;
        }

        // Get adult family members (role GUID = FamilyAdult)
        var adultMembers = person.PrimaryFamily.Members
            .Where(m => m.GroupRole?.Guid == SystemGuid.GroupTypeRole.FamilyAdult)
            .Select(m => m.Person)
            .Where(p => p != null)
            .ToList();

        // Find first SMS-enabled mobile phone number from adults
        foreach (var adult in adultMembers)
        {
            var mobilePhone = adult!.PhoneNumbers
                .Where(pn => pn.IsMessagingEnabled)
                .OrderBy(pn => pn.Id) // Get the first one added
                .FirstOrDefault();

            if (mobilePhone != null && !string.IsNullOrWhiteSpace(mobilePhone.Number))
            {
                return FormatPhoneNumber(mobilePhone);
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the parent/guardian phone number for a person.
    /// Looks for SMS-enabled mobile numbers from adult family members.
    /// </summary>
    private async Task<string?> GetParentPhoneNumberAsync(int? personId, CancellationToken ct = default)
    {
        if (!personId.HasValue)
        {
            return null;
        }

        // Get the person's primary family
        var person = await context.People
            .Include(p => p.PrimaryFamily)
                .ThenInclude(f => f!.Members)
                    .ThenInclude(m => m.Person)
                        .ThenInclude(p => p!.PhoneNumbers)
            .Include(p => p.PrimaryFamily)
                .ThenInclude(f => f!.Members)
                    .ThenInclude(m => m.GroupRole)
            .FirstOrDefaultAsync(p => p.Id == personId.Value, ct);

        if (person?.PrimaryFamily == null)
        {
            return null;
        }

        // Get adult family members (role GUID = FamilyAdult)
        var adultMembers = person.PrimaryFamily.Members
            .Where(m => m.GroupRole?.Guid == SystemGuid.GroupTypeRole.FamilyAdult)
            .Select(m => m.Person)
            .Where(p => p != null)
            .ToList();

        // Find first SMS-enabled mobile phone number from adults
        foreach (var adult in adultMembers)
        {
            var mobilePhone = adult!.PhoneNumbers
                .Where(pn => pn.IsMessagingEnabled)
                .OrderBy(pn => pn.Id) // Get the first one added
                .FirstOrDefault();

            if (mobilePhone != null && !string.IsNullOrWhiteSpace(mobilePhone.Number))
            {
                return FormatPhoneNumber(mobilePhone);
            }
        }

        return null;
    }

    /// <summary>
    /// Formats a phone number to E.164 format for SMS delivery.
    /// </summary>
    private static string FormatPhoneNumber(PhoneNumber phoneNumber)
    {
        var number = phoneNumber.Number;
        if (!number.StartsWith('+'))
        {
            // Assume US/Canada if no country code
            var normalized = phoneNumber.NumberNormalized;
            if (normalized.Length == 10)
            {
                number = $"+1{normalized}";
            }
            else if (normalized.Length == 11 && normalized.StartsWith('1'))
            {
                number = $"+{normalized}";
            }
            else
            {
                number = $"+1{normalized}"; // Best effort
            }
        }
        return number;
    }

    /// <summary>
    /// Builds the SMS message text based on message type and template.
    /// </summary>
    private static string BuildMessageText(PagerMessageType messageType, string? customMessage, string childName, string locationName)
    {
        return messageType switch
        {
            PagerMessageType.PickupNeeded => $"Please come to {locationName} to pick up {childName}.",
            PagerMessageType.NeedsAttention => $"Your child {childName} needs attention in {locationName}.",
            PagerMessageType.ServiceEnding => $"Service ending early - please pick up {childName} from {locationName}.",
            PagerMessageType.Custom => customMessage ?? "",
            _ => $"Please check on {childName} in {locationName}."
        };
    }
}
