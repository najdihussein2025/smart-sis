namespace SmartSis.Api.Shared.Models;

/// <summary>CreatedAt / UpdatedAt, set by AuditInterceptor. Use directly for tables with a natural or composite key.</summary>
public abstract class AuditableEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Default base: int identity Id + audit columns.</summary>
public abstract class BaseEntity : AuditableEntity
{
    public int Id { get; set; }
}
