using Koinon.Domain.Data;

namespace Koinon.Domain.Entities;

/// <summary>
/// Abstract base class for all domain entities.
/// Provides core properties including identity, unique identifiers, and audit tracking.
/// </summary>
public abstract class Entity : IEntity, IAuditable
{
    /// <summary>
    /// Primary key (database identity column).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Globally unique identifier for the entity.
    /// Automatically initialized to a new GUID.
    /// </summary>
    public Guid Guid { get; set; } = Guid.NewGuid();

    /// <summary>
    /// URL-safe Base64-encoded representation of the Id.
    /// This is a computed property and should never be stored in the database.
    /// </summary>
    public string IdKey => IdKeyHelper.Encode(Id);

    /// <summary>
    /// Date and time when the entity was created.
    /// </summary>
    public DateTime CreatedDateTime { get; set; }

    /// <summary>
    /// Date and time when the entity was last modified.
    /// </summary>
    public DateTime? ModifiedDateTime { get; set; }

    /// <summary>
    /// PersonAlias ID of the user who created this entity.
    /// </summary>
    public int? CreatedByPersonAliasId { get; set; }

    /// <summary>
    /// PersonAlias ID of the user who last modified this entity.
    /// </summary>
    public int? ModifiedByPersonAliasId { get; set; }
}
