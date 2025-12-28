namespace Koinon.Domain.Entities;

/// <summary>
/// INVALID: Entity that does NOT inherit from Entity base class.
/// This should be detected as a violation during graph generation.
/// </summary>
public class BadEntity
{
    /// <summary>
    /// Custom ID property without proper base class.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Custom Guid without proper base class.
    /// </summary>
    public Guid Guid { get; set; }

    /// <summary>
    /// Name property.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Created date without auditing base class.
    /// </summary>
    public DateTime CreatedDateTime { get; set; }
}
