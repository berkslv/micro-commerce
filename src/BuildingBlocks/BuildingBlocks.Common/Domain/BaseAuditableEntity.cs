namespace BuildingBlocks.Common.Domain;

/// <summary>
/// Base entity with audit properties for tracking creation and modification.
/// </summary>
public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; protected set; }

    public string? CreatedBy { get; protected set; }

    public DateTime? ModifiedAt { get; protected set; }

    public string? ModifiedBy { get; protected set; }

    public void SetCreatedAt(DateTime createdAt)
    {
        CreatedAt = createdAt;
    }

    public void SetCreatedBy(string createdBy)
    {
        CreatedBy = createdBy;
    }

    public void SetModifiedAt(DateTime modifiedAt)
    {
        ModifiedAt = modifiedAt;
    }

    public void SetModifiedBy(string modifiedBy)
    {
        ModifiedBy = modifiedBy;
    }
}
