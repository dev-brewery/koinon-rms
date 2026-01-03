using Koinon.Api.Filters;
using Koinon.Application.Common;
using Koinon.Application.DTOs.PersonMerge;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for person merge and duplicate detection operations.
/// Provides endpoints for finding duplicates, comparing records, executing merges, and managing ignore rules.
/// </summary>
[ApiController]
[Route("api/v1/people")]
[Authorize]
public class PersonMergeController(
    IDuplicateDetectionService duplicateDetectionService,
    IPersonMergeService personMergeService,
    IDuplicateIgnoreService duplicateIgnoreService,
    ILogger<PersonMergeController> logger) : ControllerBase
{
    /// <summary>
    /// Lists all potential duplicate person pairs with pagination.
    /// </summary>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of potential duplicates</returns>
    /// <response code="200">Returns paginated list of duplicate matches</response>
    [HttpGet("duplicates")]
    [ProducesResponseType(typeof(PagedResult<DuplicateMatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDuplicates(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await duplicateDetectionService.FindDuplicatesAsync(page, pageSize, ct);

        logger.LogInformation(
            "Duplicate detection completed: Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
            result.Page, result.PageSize, result.TotalCount);

        return Ok(new
        {
            data = result.Items,
            meta = new
            {
                page = result.Page,
                pageSize = result.PageSize,
                totalCount = result.TotalCount,
                totalPages = (int)Math.Ceiling(result.TotalCount / (double)result.PageSize)
            }
        });
    }

    /// <summary>
    /// Finds potential duplicates for a specific person.
    /// </summary>
    /// <param name="idKey">The person's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of potential duplicate matches</returns>
    /// <response code="200">Returns list of duplicate matches for the person</response>
    [HttpGet("{idKey}/duplicates")]
    [ValidateIdKey]
    [ProducesResponseType(typeof(List<DuplicateMatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDuplicatesForPerson(
        string idKey,
        CancellationToken ct = default)
    {
        var duplicates = await duplicateDetectionService.FindDuplicatesForPersonAsync(idKey, ct);

        logger.LogInformation(
            "Duplicates found for person: IdKey={IdKey}, Count={Count}",
            idKey, duplicates.Count);

        return Ok(new { data = duplicates });
    }

    /// <summary>
    /// Gets a side-by-side comparison of two persons to aid in merge decision.
    /// </summary>
    /// <param name="person1IdKey">IdKey of the first person</param>
    /// <param name="person2IdKey">IdKey of the second person</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Comparison details including record counts</returns>
    /// <response code="200">Returns comparison details</response>
    /// <response code="404">One or both persons not found</response>
    [HttpGet("compare")]
    [ValidateIdKey]
    [ProducesResponseType(typeof(PersonComparisonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ComparePeople(
        [FromQuery] string person1IdKey,
        [FromQuery] string person2IdKey,
        CancellationToken ct = default)
    {
        var comparison = await personMergeService.ComparePersonsAsync(person1IdKey, person2IdKey, ct);

        if (comparison == null)
        {
            logger.LogDebug(
                "Person comparison failed - one or both not found: Person1IdKey={Person1IdKey}, Person2IdKey={Person2IdKey}",
                person1IdKey, person2IdKey);

            return NotFound(new ProblemDetails
            {
                Title = "Person not found",
                Detail = "One or both persons not found for comparison",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug(
            "Person comparison retrieved: Person1IdKey={Person1IdKey}, Person2IdKey={Person2IdKey}",
            person1IdKey, person2IdKey);

        return Ok(new { data = comparison });
    }

    /// <summary>
    /// Executes a merge operation, combining two person records.
    /// </summary>
    /// <param name="request">Merge request with survivor/merged selection and field preferences</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result containing counts of updated records</returns>
    /// <response code="200">Merge completed successfully</response>
    /// <response code="400">Invalid merge request</response>
    /// <response code="401">Not authenticated</response>
    [HttpPost("merge")]
    [ProducesResponseType(typeof(PersonMergeResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MergePeople(
        [FromBody] PersonMergeRequestDto request,
        CancellationToken ct = default)
    {
        // Get the current user's PersonId from claims
        var personIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(personIdClaim) || !int.TryParse(personIdClaim, out var currentUserId))
        {
            logger.LogWarning("Failed to get PersonId from claims for person merge");

            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "Unable to identify current user",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await personMergeService.MergeAsync(request, currentUserId, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Person merge failed: SurvivorIdKey={SurvivorIdKey}, MergedIdKey={MergedIdKey}, Error={Error}",
                request.SurvivorIdKey, request.MergedIdKey, result.Error!.Message);

            return BadRequest(new ProblemDetails
            {
                Title = "Merge failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogInformation(
            "Person merge completed: SurvivorIdKey={SurvivorIdKey}, MergedIdKey={MergedIdKey}, UpdatedRecords={UpdatedRecords}",
            request.SurvivorIdKey, request.MergedIdKey, result.Value!.TotalRecordsUpdated);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Gets the audit log of person merge operations.
    /// </summary>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of merge history records</returns>
    /// <response code="200">Returns paginated merge history</response>
    [HttpGet("merge-history")]
    [ProducesResponseType(typeof(PagedResult<PersonMergeHistoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMergeHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await personMergeService.GetMergeHistoryAsync(page, pageSize, ct);

        logger.LogInformation(
            "Merge history retrieved: Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
            result.Page, result.PageSize, result.TotalCount);

        return Ok(new
        {
            data = result.Items,
            meta = new
            {
                page = result.Page,
                pageSize = result.PageSize,
                totalCount = result.TotalCount,
                totalPages = (int)Math.Ceiling(result.TotalCount / (double)result.PageSize)
            }
        });
    }

    /// <summary>
    /// Marks a pair of persons as "not duplicates" to exclude them from duplicate detection.
    /// </summary>
    /// <param name="request">Request containing the two person IdKeys and optional reason</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success result</returns>
    /// <response code="200">Duplicate pair ignored successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Not authenticated</response>
    [HttpPost("duplicates/ignore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IgnoreDuplicate(
        [FromBody] IgnoreDuplicateRequestDto request,
        CancellationToken ct = default)
    {
        // Get the current user's PersonId from claims
        var personIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(personIdClaim) || !int.TryParse(personIdClaim, out var currentUserId))
        {
            logger.LogWarning("Failed to get PersonId from claims for ignore duplicate");

            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "Unable to identify current user",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await duplicateIgnoreService.IgnoreDuplicateAsync(request, currentUserId, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Ignore duplicate failed: Person1IdKey={Person1IdKey}, Person2IdKey={Person2IdKey}, Error={Error}",
                request.Person1IdKey, request.Person2IdKey, result.Error!.Message);

            return BadRequest(new ProblemDetails
            {
                Title = "Ignore failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogInformation(
            "Duplicate pair ignored: Person1IdKey={Person1IdKey}, Person2IdKey={Person2IdKey}",
            request.Person1IdKey, request.Person2IdKey);

        return Ok(new { message = "Duplicate pair ignored successfully" });
    }

    /// <summary>
    /// Removes the "ignore" flag from a duplicate pair, allowing them to appear in detection again.
    /// </summary>
    /// <param name="person1IdKey">IdKey of the first person</param>
    /// <param name="person2IdKey">IdKey of the second person</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success result</returns>
    /// <response code="200">Ignore flag removed successfully</response>
    /// <response code="400">Invalid request</response>
    [HttpDelete("duplicates/ignore/{person1IdKey}/{person2IdKey}")]
    [ValidateIdKey]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnignoreDuplicate(
        string person1IdKey,
        string person2IdKey,
        CancellationToken ct = default)
    {
        var result = await duplicateIgnoreService.UnignoreDuplicateAsync(person1IdKey, person2IdKey, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Unignore duplicate failed: Person1IdKey={Person1IdKey}, Person2IdKey={Person2IdKey}, Error={Error}",
                person1IdKey, person2IdKey, result.Error!.Message);

            return BadRequest(new ProblemDetails
            {
                Title = "Unignore failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogInformation(
            "Duplicate pair unignored: Person1IdKey={Person1IdKey}, Person2IdKey={Person2IdKey}",
            person1IdKey, person2IdKey);

        return Ok(new { message = "Ignore flag removed successfully" });
    }
}
