namespace CentralIdentity.Domain.Common;

public abstract class AuditableEntity : Entity
{
    public string? CreatedBy { get; protected set; }
    public string? UpdatedBy { get; protected set; }

    protected void SetCreatedBy(string createdBy) => CreatedBy = createdBy;
    protected void SetUpdatedBy(string updatedBy) => UpdatedBy = updatedBy;
}
