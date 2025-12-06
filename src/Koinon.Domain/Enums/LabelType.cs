namespace Koinon.Domain.Enums;

/// <summary>
/// Defines the types of labels that can be generated during check-in.
/// </summary>
public enum LabelType
{
    /// <summary>
    /// Child's name tag worn by child.
    /// Typically includes child's name, security code, group, and service time.
    /// </summary>
    ChildName = 0,

    /// <summary>
    /// Security code label for child's back.
    /// Contains large security code for easy identification.
    /// </summary>
    ChildSecurity = 1,

    /// <summary>
    /// Parent's claim ticket with matching code.
    /// Used to verify authorized pickup of child.
    /// </summary>
    ParentClaim = 2,

    /// <summary>
    /// Visitor name badge.
    /// For guests and first-time visitors.
    /// </summary>
    VisitorName = 3,

    /// <summary>
    /// Allergy alert label.
    /// Prominently displays allergies and special needs.
    /// </summary>
    Allergy = 4
}
