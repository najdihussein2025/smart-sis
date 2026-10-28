namespace SmartSis.Api.Shared.Models;

/// <summary>Rows are never physically deleted. A global query filter hides rows with DeletedAt set.</summary>
public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }

    /// <summary>Called by SoftDeleteInterceptor when the entity is removed.</summary>
    void MarkDeleted(DateTime utcNow);
}
