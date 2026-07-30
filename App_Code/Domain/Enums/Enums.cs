namespace EidUbahle.Domain.Enums
{
    public enum ThemeMode { Auto, Light, Dark }
    public enum LayoutType { Classic, TopNav, Compact }
    public enum TextDirection { LTR, RTL }
    public enum SyncStatus { Pending, InProgress, Success, Failed, Conflict, Partial }
    public enum SyncType { Push, Pull, Full }
    public enum ConflictResolution { Pending, ServerWins, ClientWins, LastModifiedWins, ManualMerge }
    public enum SubscriptionStatus { Trial, Active, Expired, Cancelled, Suspended }
    public enum DocumentStatus { Draft, PendingApproval, Approved, Rejected, Cancelled, Posted }
    public enum AccountingBasis { Accrual, Cash }
    public enum JournalEntryType { Normal, Reversing, Adjustment, Opening, Closing }
    public enum NotificationChannel { InApp, Email, SMS, WhatsApp, Push }
    public enum NotificationStatus { Queued, Sent, Failed, Delivered }
    public enum WorkflowStatus { Active, Inactive, Draft }
    public enum WorkflowStepType { Approval, Notification, AutoAction, Condition }
    public enum PermissionAction { View, Create, Edit, Delete, Approve, Export, Import, Print }
    public enum AuditAction { Create, Update, Delete, Restore, Login, Logout, Export, Approve, Reject, Post, Reverse }
    public enum StorageProvider { Local, AzureBlob, AwsS3 }
    public enum AttachmentStatus { Uploaded, Processing, Ready, Failed }
    public enum JobStatus { Queued, Running, Completed, Failed, Cancelled }
}
