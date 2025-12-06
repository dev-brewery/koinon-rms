namespace Koinon.Domain.Entities;

/// <summary>
/// Base interface for all domain entities.
/// Provides unique identifiers and URL-safe key encoding.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Primary key (database identity column).
    /// </summary>
    int Id { get; set; }

    /// <summary>
    /// Globally unique identifier for the entity.
    /// </summary>
    Guid Guid { get; set; }

    /// <summary>
    /// URL-safe Base64-encoded representation of the Id.
    /// This computed property should never be stored in the database.
    /// </summary>
    string IdKey { get; }
}
