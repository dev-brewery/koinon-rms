using Koinon.Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Koinon.Api.Filters;

/// <summary>
/// Action filter that validates IdKey parameters in route or query string.
/// Automatically validates any parameter whose name ends with "IdKey" or matches known IdKey parameter names.
/// Returns 400 Bad Request with ProblemDetails if validation fails.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class ValidateIdKeyAttribute : ActionFilterAttribute
{
    /// <summary>
    /// Validates IdKey parameters before action execution.
    /// Checks route values, query parameters, and action arguments for IdKey patterns.
    /// </summary>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Check route parameters ending with "IdKey" or "Id"
        foreach (var routeValue in context.RouteData.Values)
        {
            if (ShouldValidateParameter(routeValue.Key) && routeValue.Value != null)
            {
                var value = routeValue.Value.ToString();
                if (!string.IsNullOrWhiteSpace(value) && !IdKeyValidator.IsValid(value))
                {
                    context.Result = CreateBadRequestResult(context, routeValue.Key, value);
                    return;
                }
            }
        }

        // Check query string parameters
        var queryParams = context.HttpContext.Request.Query;
        foreach (var param in queryParams)
        {
            if (ShouldValidateParameter(param.Key) && !string.IsNullOrWhiteSpace(param.Value))
            {
                var value = param.Value.ToString();
                if (!IdKeyValidator.IsValid(value))
                {
                    context.Result = CreateBadRequestResult(context, param.Key, value);
                    return;
                }
            }
        }

        // Check action arguments (only validate string arguments, skip complex DTOs)
        foreach (var arg in context.ActionArguments)
        {
            if (ShouldValidateParameter(arg.Key) && arg.Value is string stringValue)
            {
                if (!string.IsNullOrWhiteSpace(stringValue) && !IdKeyValidator.IsValid(stringValue))
                {
                    context.Result = CreateBadRequestResult(context, arg.Key, stringValue);
                    return;
                }
            }
        }

        base.OnActionExecuting(context);
    }

    /// <summary>
    /// Determines if a parameter name should be validated as an IdKey.
    /// </summary>
    private static bool ShouldValidateParameter(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        // Match parameters ending with "IdKey" (case-insensitive)
        if (parameterName.EndsWith("IdKey", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Match common IdKey parameter names
        var idKeyParameterNames = new[]
        {
            "campusId", "recordStatusId", "connectionStatusId", "groupTypeId",
            "parentGroupId", "personIdKey", "scheduleIdKey", "kioskId",
            "locationIdKey", "attendanceIdKey"
        };

        return idKeyParameterNames.Contains(parameterName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a standardized Bad Request response with ProblemDetails.
    /// </summary>
    private static BadRequestObjectResult CreateBadRequestResult(
        ActionExecutingContext context,
        string parameterName,
        string? value)
    {
        return new BadRequestObjectResult(new ProblemDetails
        {
            Title = "Invalid IdKey format",
            Detail = $"The parameter '{parameterName}' must be a valid IdKey (URL-safe Base64 encoded identifier). Provided value: '{value}'",
            Status = StatusCodes.Status400BadRequest,
            Instance = context.HttpContext.Request.Path
        });
    }
}
