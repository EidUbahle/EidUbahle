using System;

namespace EidUbahle.Domain.Entities
{
    /// <summary>
    /// Base entity for every table in the system.
    /// Enforces consistent audit columns, soft delete, multi-tenant isolation,
    /// and optimistic concurrency via Version (rowversion).
    /// </summary>
    public abstract class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid CompanyId { get; set; }
        public Guid BranchId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public byte[] Version { get; set; }          // rowversion / timestamp
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public Guid? DeletedBy { get; set; }
    }
}
