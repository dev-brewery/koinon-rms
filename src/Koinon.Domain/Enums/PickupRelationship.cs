namespace Koinon.Domain.Enums;

/// <summary>
/// Defines the relationship between a child and an authorized pickup person.
/// </summary>
public enum PickupRelationship
{
    /// <summary>
    /// Parent of the child.
    /// </summary>
    Parent = 0,

    /// <summary>
    /// Grandparent of the child.
    /// </summary>
    Grandparent = 1,

    /// <summary>
    /// Sibling of the child.
    /// </summary>
    Sibling = 2,

    /// <summary>
    /// Legal guardian of the child.
    /// </summary>
    Guardian = 3,

    /// <summary>
    /// Aunt of the child.
    /// </summary>
    Aunt = 4,

    /// <summary>
    /// Uncle of the child.
    /// </summary>
    Uncle = 5,

    /// <summary>
    /// Family friend.
    /// </summary>
    Friend = 6,

    /// <summary>
    /// Other relationship not covered by specific types.
    /// </summary>
    Other = 7
}
