namespace Koinon.Domain.Attributes;

/// <summary>
/// Marks an entity class as excluded from automatic audit logging.
/// Use this attribute to prevent circular dependencies or exclude internal tracking entities.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class NoAuditAttribute : Attribute
{
}
