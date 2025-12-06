namespace Koinon.Domain.Entities;

/// <summary>
/// Interface for entities that track creation and modification audit information.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Date and time when the entity was created.
    /// </summary>
    DateTime CreatedDateTime { get; set; }

    /// <summary>
    /// Date and time when the entity was last modified.
    /// </summary>
    DateTime? ModifiedDateTime { get; set; }

    /// <summary>
    /// PersonAlias ID of the user who created this entity.
    /// </summary>
    int? CreatedByPersonAliasId { get; set; }

    /// <summary>
    /// PersonAlias ID of the user who last modified this entity.
    /// </summary>
    int? ModifiedByPersonAliasId { get; set; }
}
