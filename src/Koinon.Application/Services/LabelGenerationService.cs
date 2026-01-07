using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Application.Services.Common;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for generating check-in labels in multiple formats (ZPL, HTML).
/// Performance-critical - optimized for <100ms label generation.
/// </summary>
public class LabelGenerationService(
    IApplicationDbContext context,
    IUserContext userContext,
    IMapper mapper,
    ILogger<LabelGenerationService> logger)
    : AuthorizedCheckinService(context, userContext, logger), ILabelGenerationService
{
    // Label dimensions (in mm) for common Zebra printers
    private const int ChildNameLabelWidth = 101; // 4 inches
    private const int ChildNameLabelHeight = 51; // 2 inches
    private const int ParentClaimLabelWidth = 76; // 3 inches
    private const int ParentClaimLabelHeight = 51; // 2 inches

    public async Task<LabelSetDto> GenerateLabelsAsync(
        LabelRequestDto request,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Step 1: Validate and decode the IdKey (no auth info leaked)
        if (!IdKeyHelper.TryDecode(request.AttendanceIdKey, out var attendanceId))
        {
            Logger.LogWarning("Invalid attendance ID key for label generation: {IdKey}", request.AttendanceIdKey);
            throw new ArgumentException($"Invalid attendance IdKey: {request.AttendanceIdKey}");
        }

        // Step 2: Load attendance with all necessary related data
        var attendance = await Context.Attendances
            .AsNoTracking()
            .Include(a => a.Occurrence)
                .ThenInclude(o => o!.Group)
                .ThenInclude(g => g!.Schedule)
            .Include(a => a.Occurrence)
                .ThenInclude(o => o!.Location)
            .Include(a => a.AttendanceCode)
            .FirstOrDefaultAsync(a => a.Id == attendanceId, ct);

        if (attendance == null)
        {
            Logger.LogWarning("Attendance {AttendanceId} not found for label generation", attendanceId);
            throw new InvalidOperationException($"Attendance not found: {request.AttendanceIdKey}");
        }

        // Get person information via PersonAlias
        if (attendance.PersonAliasId == null)
        {
            Logger.LogWarning("Attendance {AttendanceId} has no associated person", attendanceId);
            throw new InvalidOperationException("Attendance has no associated person");
        }

        var personAlias = await Context.PersonAliases
            .AsNoTracking()
            .Include(pa => pa.Person)
            .FirstOrDefaultAsync(pa => pa.Id == attendance.PersonAliasId.Value, ct);

        if (personAlias?.Person == null)
        {
            Logger.LogWarning("Person not found for attendance {AttendanceId}", attendanceId);
            throw new InvalidOperationException($"Person not found for attendance {request.AttendanceIdKey}");
        }

        var person = personAlias.Person;

        // Step 3: SECURITY - Verify user can access this person's data
        AuthorizePersonAccess(person.Id, nameof(GenerateLabelsAsync));

        // Determine age and whether this is a child
        var age = CalculateAge(person.BirthDate);
        var isChild = age.HasValue && age.Value < 18;

        // Determine which labels to generate
        var labelTypes = request.LabelTypes ?? GetDefaultLabelTypes(isChild, person);

        // Generate labels
        var labels = new List<LabelDto>();
        var fields = BuildLabelFields(person, attendance, request.CustomFields);

        foreach (var labelType in labelTypes)
        {
            var label = GenerateLabel(labelType, fields);
            labels.Add(label);
        }

        stopwatch.Stop();
        if (stopwatch.ElapsedMilliseconds > 100)
        {
            Logger.LogWarning(
                "Label generation exceeded 100ms target: {Elapsed}ms for attendance {AttendanceIdKey}, {LabelCount} labels",
                stopwatch.ElapsedMilliseconds, request.AttendanceIdKey, labels.Count);
        }
        else
        {
            Logger.LogInformation(
                "Label generation completed in {Elapsed}ms for attendance {AttendanceIdKey}, {LabelCount} labels",
                stopwatch.ElapsedMilliseconds, request.AttendanceIdKey, labels.Count);
        }

        return new LabelSetDto(
            request.AttendanceIdKey,
            person.IdKey,
            labels.AsReadOnly()
        );
    }

    public async Task<IReadOnlyList<LabelSetDto>> GenerateBatchLabelsAsync(
        BatchLabelRequestDto request,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<LabelSetDto>();
        var errors = new List<string>();

        // Generate labels for each attendance
        // SECURITY: Fail entire batch on any authorization failure to prevent info disclosure
        foreach (var attendanceIdKey in request.AttendanceIdKeys)
        {
            var labelRequest = new LabelRequestDto
            {
                AttendanceIdKey = attendanceIdKey,
                LabelTypes = request.LabelTypes,
                CustomFields = request.CustomFields
            };

            try
            {
                var labelSet = await GenerateLabelsAsync(labelRequest, ct);
                results.Add(labelSet);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Authorization failure - fail entire batch to prevent timing attacks
                Logger.LogWarning(ex, "Authorization failed in batch label generation for {IdKey}", attendanceIdKey);
                throw new UnauthorizedAccessException(
                    "Batch label generation failed: one or more attendance records are not accessible");
            }
            catch (Exception ex) when (ex is not UnauthorizedAccessException)
            {
                // Only catch non-auth exceptions - auth failures must propagate immediately
                Logger.LogError(ex, "Error generating labels for {IdKey}", attendanceIdKey);
                errors.Add($"Failed to generate labels for {attendanceIdKey}: {ex.Message}");
            }
        }

        // If any non-auth errors occurred, fail the entire batch
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Batch label generation failed with {errors.Count} errors: {string.Join("; ", errors)}");
        }

        stopwatch.Stop();
        var targetMs = request.AttendanceIdKeys.Count * 50; // 50ms per label set
        if (stopwatch.ElapsedMilliseconds > targetMs)
        {
            Logger.LogWarning(
                "Batch label generation exceeded {Target}ms target: {Elapsed}ms for {Count} check-ins",
                targetMs, stopwatch.ElapsedMilliseconds, request.AttendanceIdKeys.Count);
        }
        else
        {
            Logger.LogInformation(
                "Batch label generation completed in {Elapsed}ms for {Count} check-ins",
                stopwatch.ElapsedMilliseconds, request.AttendanceIdKeys.Count);
        }

        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<LabelTemplateDto>> GetTemplatesAsync(CancellationToken ct = default)
    {
        var templates = await Context.LabelTemplates
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Type)
            .ThenBy(t => t.Name)
            .ProjectTo<LabelTemplateDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return templates.AsReadOnly();
    }

    public Task<LabelPreviewDto> PreviewLabelAsync(
        LabelPreviewRequestDto request,
        CancellationToken ct = default)
    {
        // Authorization check - preview is authentication-only, no specific resource access needed
        AuthorizeAuthentication(nameof(PreviewLabelAsync));

        // Generate HTML preview based on label type
        var html = GenerateLabelPreviewHtml(request.Type, request.Fields);

        return Task.FromResult(new LabelPreviewDto(
            Type: request.Type,
            PreviewHtml: html,
            Format: "HTML"
        ));
    }

    // Private helper methods

    private static int? CalculateAge(DateOnly? birthDate)
    {
        if (!birthDate.HasValue)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Value.Year;

        if (birthDate.Value > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    private static IReadOnlyList<LabelType> GetDefaultLabelTypes(bool isChild, Person person)
    {
        var labels = new List<LabelType>();

        if (isChild)
        {
            // Children get name tag and parent claim ticket
            labels.Add(LabelType.ChildName);
            labels.Add(LabelType.ParentClaim);

            // Add allergy label if person has allergies
            if (!string.IsNullOrWhiteSpace(person.Allergies))
            {
                labels.Add(LabelType.Allergy);
            }
        }
        else
        {
            // Adults/visitors get visitor badge
            labels.Add(LabelType.VisitorName);
        }

        return labels.AsReadOnly();
    }

    private static Dictionary<string, string> BuildLabelFields(
        Person person,
        Attendance attendance,
        IDictionary<string, string>? customFields)
    {
        // For MVP, display times in UTC. Proper timezone support requires Organization/Location timezone settings.
        // BLOCKER-3: Labels should display location timezone, not server timezone.
        // Future: Pass TimeZoneInfo parameter and convert attendance.StartDateTime accordingly.
        var fields = new Dictionary<string, string>
        {
            ["FirstName"] = person.FirstName,
            ["LastName"] = person.LastName,
            ["NickName"] = person.NickName ?? person.FirstName,
            ["FullName"] = person.FullName,
            ["SecurityCode"] = attendance.AttendanceCode?.Code ?? "---",
            ["CheckInTime"] = $"{attendance.StartDateTime:h:mm tt} UTC",
            ["Date"] = attendance.StartDateTime.ToString("M/d/yyyy"),
            ["GroupName"] = attendance.Occurrence?.Group?.Name ?? "Check-In",
            ["LocationName"] = attendance.Occurrence?.Location?.Name ?? "",
            ["ServiceTime"] = attendance.Occurrence?.Group?.Schedule?.Name ?? ""
        };

        // Add age if available
        var age = CalculateAge(person.BirthDate);
        if (age.HasValue)
        {
            fields["Age"] = age.Value.ToString();
        }

        // Add allergy information with sanitization for ZPL safety
        var allergiesValue = person.Allergies ?? "";
        // Remove all ZPL control characters: ^ ~ \ (field delimiter, tilde commands, backslash)
        allergiesValue = System.Text.RegularExpressions.Regex.Replace(allergiesValue, @"[\^~\\]", "");
        if (allergiesValue.Length > 50)
        {
            allergiesValue = allergiesValue.Substring(0, 47) + "...";
        }

        fields["Allergies"] = allergiesValue;
        fields["HasCriticalAllergies"] = person.HasCriticalAllergies ? "CRITICAL" : "";

        // Merge custom fields
        if (customFields != null)
        {
            foreach (var (key, value) in customFields)
            {
                fields[key] = value;
            }
        }

        return fields;
    }

    private static LabelDto GenerateLabel(LabelType labelType, Dictionary<string, string> fields)
    {
        return labelType switch
        {
            LabelType.ChildName => GenerateChildNameLabel(fields),
            LabelType.ParentClaim => GenerateParentClaimLabel(fields),
            LabelType.Allergy => GenerateAllergyLabel(fields),
            LabelType.ChildSecurity => GenerateChildSecurityLabel(fields),
            LabelType.VisitorName => GenerateVisitorNameLabel(fields),
            _ => throw new ArgumentException($"Unknown label type: {labelType}")
        };
    }

    private static LabelDto GenerateChildNameLabel(Dictionary<string, string> fields)
    {
        var zpl = GetChildNameZplTemplate();
        var content = ReplacePlaceholders(zpl, fields);

        return new LabelDto(
            LabelType.ChildName,
            content,
            "ZPL",
            fields
        );
    }

    private static LabelDto GenerateParentClaimLabel(Dictionary<string, string> fields)
    {
        var zpl = GetParentClaimZplTemplate();
        var content = ReplacePlaceholders(zpl, fields);

        return new LabelDto(
            LabelType.ParentClaim,
            content,
            "ZPL",
            fields
        );
    }

    private static LabelDto GenerateAllergyLabel(Dictionary<string, string> fields)
    {
        var zpl = GetAllergyAlertZplTemplate();
        var content = ReplacePlaceholders(zpl, fields);

        return new LabelDto(
            LabelType.Allergy,
            content,
            "ZPL",
            fields
        );
    }

    private static LabelDto GenerateChildSecurityLabel(Dictionary<string, string> fields)
    {
        // Simple security code label with large text
        var zpl = new StringBuilder()
            .AppendLine("^XA")
            .AppendLine("^FO50,50^A0N,150,150^FD{SecurityCode}^FS")
            .AppendLine("^XZ")
            .ToString();

        var content = ReplacePlaceholders(zpl, fields);

        return new LabelDto(
            LabelType.ChildSecurity,
            content,
            "ZPL",
            fields
        );
    }

    private static LabelDto GenerateVisitorNameLabel(Dictionary<string, string> fields)
    {
        var zpl = new StringBuilder()
            .AppendLine("^XA")
            .AppendLine("^FO50,30^A0N,60,60^FD{FullName}^FS")
            .AppendLine("^FO50,100^A0N,30,30^FD{GroupName}^FS")
            .AppendLine("^FO50,140^A0N,25,25^FD{ServiceTime}^FS")
            .AppendLine("^XZ")
            .ToString();

        var content = ReplacePlaceholders(zpl, fields);

        return new LabelDto(
            LabelType.VisitorName,
            content,
            "ZPL",
            fields
        );
    }

    private static string GetChildNameZplTemplate()
    {
        // ZPL for 4"x2" label on Zebra thermal printer
        // ^XA = Start of label, ^XZ = End of label
        // ^FO = Field Origin (position), ^A = Font, ^FD = Field Data
        return new StringBuilder()
            .AppendLine("^XA")
            .AppendLine("^FO50,30^A0N,50,50^FD{NickName} {LastName}^FS")
            .AppendLine("^FO50,90^A0N,30,30^FD{GroupName}^FS")
            .AppendLine("^FO50,130^A0N,25,25^FD{ServiceTime}^FS")
            .AppendLine("^FO300,30^A0N,80,80^FD{SecurityCode}^FS")
            .AppendLine("^FO300,120^A0N,20,20^FDCode: {SecurityCode}^FS")
            .AppendLine("^XZ")
            .ToString();
    }

    private static string GetParentClaimZplTemplate()
    {
        // Parent claim ticket with large security code
        return new StringBuilder()
            .AppendLine("^XA")
            .AppendLine("^FO50,20^A0N,100,100^FD{SecurityCode}^FS")
            .AppendLine("^FO50,130^A0N,25,25^FD{FullName}^FS")
            .AppendLine("^FO50,160^A0N,20,20^FD{ServiceTime} - {CheckInTime}^FS")
            .AppendLine("^XZ")
            .ToString();
    }

    private static string GetAllergyAlertZplTemplate()
    {
        return new StringBuilder()
            .AppendLine("^XA")
            .AppendLine("^FO50,20^A0N,40,40^FDALLERGY ALERT^FS")
            .AppendLine("^FO50,70^A0N,30,30^FD{FullName}^FS")
            .AppendLine("^FO50,110^A0N,25,25^FD{Allergies}^FS")
            .AppendLine("^XZ")
            .ToString();
    }

    private static string GetChildSecurityZplTemplate()
    {
        // Simple security code label with large text
        return new StringBuilder()
            .AppendLine("^XA")
            .AppendLine("^FO50,50^A0N,150,150^FD{SecurityCode}^FS")
            .AppendLine("^XZ")
            .ToString();
    }

    private static string GetVisitorNameZplTemplate()
    {
        return new StringBuilder()
            .AppendLine("^XA")
            .AppendLine("^FO50,30^A0N,60,60^FD{FullName}^FS")
            .AppendLine("^FO50,100^A0N,30,30^FD{GroupName}^FS")
            .AppendLine("^FO50,140^A0N,25,25^FD{ServiceTime}^FS")
            .AppendLine("^XZ")
            .ToString();
    }

    private static string ReplacePlaceholders(string template, Dictionary<string, string> fields)
    {
        var result = template;

        foreach (var (key, value) in fields)
        {
            // Escape braces to prevent injection of placeholder patterns
            var safeValue = (value ?? "").Replace("{", "{{").Replace("}", "}}");
            result = result.Replace($"{{{key}}}", safeValue);
        }

        return result;
    }

    private static string GenerateLabelPreviewHtml(LabelType labelType, IDictionary<string, string> fields)
    {
        return labelType switch
        {
            LabelType.ChildName => GenerateChildNamePreviewHtml(fields),
            LabelType.ParentClaim => GenerateParentClaimPreviewHtml(fields),
            LabelType.Allergy => GenerateAllergyPreviewHtml(fields),
            LabelType.ChildSecurity => GenerateChildSecurityPreviewHtml(fields),
            LabelType.VisitorName => GenerateVisitorNamePreviewHtml(fields),
            _ => "<div>Unknown label type</div>"
        };
    }

    private static string GenerateChildNamePreviewHtml(IDictionary<string, string> fields)
    {
        fields.TryGetValue("NickName", out var nickName);
        fields.TryGetValue("LastName", out var lastName);
        fields.TryGetValue("GroupName", out var group);
        fields.TryGetValue("ServiceTime", out var service);
        fields.TryGetValue("SecurityCode", out var code);

        var name = (nickName ?? "") + " " + (lastName ?? "");

        // HTML-encode all user data to prevent XSS
        var encoder = HtmlEncoder.Default;
        var encodedName = encoder.Encode(name);
        var encodedGroup = encoder.Encode(group ?? "");
        var encodedService = encoder.Encode(service ?? "");
        var encodedCode = encoder.Encode(code ?? "");

        return $@"
<div style='width: 4in; height: 2in; border: 1px solid #ccc; padding: 10px; font-family: Arial, sans-serif; position: relative;'>
    <div style='font-size: 24px; font-weight: bold;'>{encodedName}</div>
    <div style='font-size: 16px; margin-top: 5px;'>{encodedGroup}</div>
    <div style='font-size: 14px; color: #666;'>{encodedService}</div>
    <div style='position: absolute; top: 10px; right: 10px; font-size: 36px; font-weight: bold; border: 2px solid #000; padding: 5px;'>{encodedCode}</div>
</div>";
    }

    private static string GenerateParentClaimPreviewHtml(IDictionary<string, string> fields)
    {
        fields.TryGetValue("SecurityCode", out var code);
        fields.TryGetValue("FullName", out var name);
        fields.TryGetValue("ServiceTime", out var service);
        fields.TryGetValue("CheckInTime", out var time);

        // HTML-encode all user data to prevent XSS
        var encoder = HtmlEncoder.Default;
        var encodedCode = encoder.Encode(code ?? "");
        var encodedName = encoder.Encode(name ?? "");
        var encodedService = encoder.Encode(service ?? "");
        var encodedTime = encoder.Encode(time ?? "");

        return $@"
<div style='width: 3in; height: 2in; border: 1px solid #ccc; padding: 10px; font-family: Arial, sans-serif; text-align: center;'>
    <div style='font-size: 48px; font-weight: bold; margin-top: 10px;'>{encodedCode}</div>
    <div style='font-size: 14px; margin-top: 10px;'>{encodedName}</div>
    <div style='font-size: 12px; color: #666;'>{encodedService} - {encodedTime}</div>
</div>";
    }

    private static string GenerateAllergyPreviewHtml(IDictionary<string, string> fields)
    {
        fields.TryGetValue("FullName", out var name);
        fields.TryGetValue("Allergies", out var allergies);

        // HTML-encode all user data to prevent XSS
        var encoder = HtmlEncoder.Default;
        var encodedName = encoder.Encode(name ?? "");
        var encodedAllergies = encoder.Encode(allergies ?? "");

        return $@"
<div style='width: 4in; height: 2in; border: 3px solid #ff0000; padding: 10px; font-family: Arial, sans-serif; background-color: #fff8dc;'>
    <div style='font-size: 20px; font-weight: bold; color: #ff0000;'>ALLERGY ALERT</div>
    <div style='font-size: 16px; font-weight: bold; margin-top: 10px;'>{encodedName}</div>
    <div style='font-size: 14px; margin-top: 5px; color: #ff0000;'>{encodedAllergies}</div>
</div>";
    }

    private static string GenerateChildSecurityPreviewHtml(IDictionary<string, string> fields)
    {
        fields.TryGetValue("SecurityCode", out var code);

        // HTML-encode all user data to prevent XSS
        var encoder = HtmlEncoder.Default;
        var encodedCode = encoder.Encode(code ?? "");

        return $@"
<div style='width: 2in; height: 1in; border: 1px solid #ccc; padding: 10px; font-family: Arial, sans-serif; text-align: center;'>
    <div style='font-size: 48px; font-weight: bold; margin-top: 5px;'>{encodedCode}</div>
</div>";
    }

    private static string GenerateVisitorNamePreviewHtml(IDictionary<string, string> fields)
    {
        fields.TryGetValue("FullName", out var name);
        fields.TryGetValue("GroupName", out var group);
        fields.TryGetValue("ServiceTime", out var service);

        // HTML-encode all user data to prevent XSS
        var encoder = HtmlEncoder.Default;
        var encodedName = encoder.Encode(name ?? "");
        var encodedGroup = encoder.Encode(group ?? "");
        var encodedService = encoder.Encode(service ?? "");

        return $@"
<div style='width: 4in; height: 2in; border: 1px solid #ccc; padding: 10px; font-family: Arial, sans-serif;'>
    <div style='font-size: 28px; font-weight: bold;'>{encodedName}</div>
    <div style='font-size: 16px; margin-top: 10px;'>{encodedGroup}</div>
    <div style='font-size: 14px; color: #666; margin-top: 5px;'>{encodedService}</div>
</div>";
    }
}
