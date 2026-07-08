using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Koinon.ArchitectureTests;

/// <summary>
/// Enforces the IdKey invariant (ADR 0002): integer IDs never appear in the
/// public API surface. Controller actions must take string idKey parameters,
/// and route templates must not constrain ids to int/long.
/// </summary>
public class RouteParameterTests
{
    /// <summary>
    /// Deliberate exceptions, one per line with a justifying comment.
    /// Adding to this list is a reviewed decision, not a convenience.
    /// </summary>
    private static readonly string[] _allowlist = [];

    private static readonly Assembly _apiAssembly = typeof(Koinon.Api.Controllers.AuthController).Assembly;

    private static IEnumerable<Type> Controllers() =>
        _apiAssembly.GetTypes().Where(t =>
            t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

    private static IEnumerable<MethodInfo> Actions(Type controller) =>
        controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName);

    [Fact]
    public void NoControllerAction_TakesIntegerIdParameter()
    {
        var violations = new List<string>();

        foreach (var controller in Controllers())
        {
            foreach (var action in Actions(controller))
            {
                foreach (var param in action.GetParameters())
                {
                    var isIdName = param.Name is not null &&
                                   (param.Name.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                                    param.Name.EndsWith("Id", StringComparison.Ordinal));
                    var isIntegral = param.ParameterType == typeof(int) || param.ParameterType == typeof(long) ||
                                     param.ParameterType == typeof(int?) || param.ParameterType == typeof(long?);
                    // [FromBody]/[FromQuery] DTO members are out of scope here; this
                    // guards route/query scalars, which is where enumeration happens.
                    if (isIdName && isIntegral)
                    {
                        var key = $"{controller.Name}.{action.Name}({param.Name})";
                        if (!_allowlist.Contains(key))
                        {
                            violations.Add(key);
                        }
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "integer IDs must never leave the API — use string idKey (ADR 0002, " +
            "docs/adr/0002-idkey-public-identifiers.md)");
    }

    [Fact]
    public void NoRouteTemplate_ConstrainsIdToIntOrLong()
    {
        var violations = new List<string>();

        foreach (var controller in Controllers())
        {
            var templates = new List<string?>();
            templates.AddRange(controller.GetCustomAttributes<RouteAttribute>().Select(a => a.Template));

            foreach (var action in Actions(controller))
            {
                templates.AddRange(action.GetCustomAttributes<RouteAttribute>().Select(a => a.Template));
                templates.AddRange(action.GetCustomAttributes().OfType<HttpMethodAttribute>().Select(a => a.Template));

                foreach (var template in templates.Where(t => t is not null))
                {
                    if (template!.Contains(":int", StringComparison.OrdinalIgnoreCase) ||
                        template.Contains(":long", StringComparison.OrdinalIgnoreCase))
                    {
                        violations.Add($"{controller.Name}.{action.Name} → \"{template}\"");
                    }
                }

                templates.Clear();
            }
        }

        violations.Should().BeEmpty(
            "route templates must use string idKey, never {id:int}/{id:long} (ADR 0002)");
    }
}
