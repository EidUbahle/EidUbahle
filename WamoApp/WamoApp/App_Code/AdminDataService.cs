using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace WamoApp
{
    public static class AdminDataService
    {
        private static readonly Dictionary<string, AdminModuleDefinition> Modules = new Dictionary<string, AdminModuleDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            { "Customers", new AdminModuleDefinition("Customers", "CustomerID", new[]{"CustomerNumber","FullName","PhoneNumber","Email","CustomerType","Status","CreatedDate"}) },
            { "Sections", new AdminModuleDefinition("Sections", "SectionID", new[]{"SectionName","Code","IsActive"}) },
            { "Blocks", new AdminModuleDefinition("Blocks", "BlockID", new[]{"BlockID","SectionID","BlockNumber","IsActive"}) },
            { "Houses", new AdminModuleDefinition("Houses", "HouseID", new[]{"HouseID","BlockID","HouseNumber","Latitude","Longitude","IsActive"}) },
            { "Payments", new AdminModuleDefinition("Payments", "PaymentID", new[]{"PaymentID","InvoiceID","AmountPaid","PaymentDate","Status"}) },
            { "Collections", new AdminModuleDefinition("Collections", "CollectionID", new[]{"CollectionID","CustomerID","ServiceName","CollectionDate","Status"}) },
            { "CollectionRequests", new AdminModuleDefinition("CollectionRequests", "RequestID", new[]{"RequestID","RequestNumber","CustomerID","ServiceName","PreferredDate","PreferredTime","Status"}) },
            { "Complaints", new AdminModuleDefinition("Complaints", "ComplaintID", new[]{"ComplaintNumber","Subject","Priority","Status","CreatedDate"}) },
            { "ShopOrders", new AdminModuleDefinition("ShopOrders", "OrderID", new[]{"OrderNumber","CustomerID","TotalAmount","Status","OrderDate"}) },
            { "Products", new AdminModuleDefinition("Products", "ProductID", new[]{"ProductName","Price","StockQuantity","IsActive"}) },
            { "Blog", new AdminModuleDefinition("BlogPosts", "PostID", new[]{"Title","LanguageCode","IsFeatured","PublishedDate"}) },
            { "Announcements", new AdminModuleDefinition("Announcements", "AnnouncementID", new[]{"Title","LanguageCode","StartDate","EndDate","IsActive"}) },
            { "Users", new AdminModuleDefinition("Users", "UserID", new[]{"UserID","Username","FullName","Email","IsActive","MustChangePassword"}) },
            { "Roles", new AdminModuleDefinition("Roles", "RoleID", new[]{"RoleName","Description","IsSystemRole","IsActive"}) },
            { "Permissions", new AdminModuleDefinition("Permissions", "PermissionID", new[]{"ModuleKey","PermissionName","Description","IsActive"}) },
            { "Sessions", new AdminModuleDefinition("UserSessions", "SessionID", new[]{"SessionID","UserID","LoginTime","LastActivity","IPAddress","Browser","OperatingSystem","IsActive","IsRevoked"}) },
            { "AuditLogs", new AdminModuleDefinition("AuditLogs", "AuditID", new[]{"AuditID","UserID","Action","Module","Page","RecordID","CreatedDate"}) },
            { "Languages", new AdminModuleDefinition("Languages", "LanguageID", new[]{"LanguageCode","Name","NativeName","IsDefault","IsRtl","IsActive"}) },
            { "Settings", new AdminModuleDefinition("SystemSettings", "SettingID", new[]{"SettingKey","SettingValue","Category","IsEncrypted","ModifiedDate"}) }
        };

        public static object GetList(string moduleKey)
        {
            var def = GetDefinition(moduleKey);
            var sql = string.Format("SELECT TOP 500 {0} FROM {1} ORDER BY 1 DESC", string.Join(",", def.SelectColumns), def.TableName);
            return new { success = true, data = DbHelper.ToDictionaryList(DbHelper.ExecuteDataTable(sql, CommandType.Text)), columns = def.SelectColumns };
        }

        public static object Delete(string moduleKey, string id)
        {
            var def = GetDefinition(moduleKey);
            DbHelper.ExecuteNonQuery(string.Format("DELETE FROM {0} WHERE {1} = @Id", def.TableName, def.KeyColumn), CommandType.Text, new SqlParameter("@Id", id));
            AuditLogger.Log(SessionManager.GetCurrentUserIdOrNull(), "DELETE", moduleKey, "Admin/" + moduleKey + ".aspx", id, null, null);
            return new { success = true, message = "Record deleted successfully." };
        }

        public static object GetDefinitionMetadata(string moduleKey)
        {
            var def = GetDefinition(moduleKey);
            return new { success = true, keyColumn = def.KeyColumn, columns = def.SelectColumns };
        }

        private static AdminModuleDefinition GetDefinition(string moduleKey)
        {
            AdminModuleDefinition def;
            if (!Modules.TryGetValue(moduleKey, out def)) throw new InvalidOperationException("Unsupported module.");
            return def;
        }

        private sealed class AdminModuleDefinition
        {
            public AdminModuleDefinition(string tableName, string keyColumn, string[] columns) { TableName = DbHelper.SafeIdentifier(tableName); KeyColumn = DbHelper.SafeIdentifier(keyColumn); SelectColumns = columns; }
            public string TableName { get; private set; }
            public string KeyColumn { get; private set; }
            public string[] SelectColumns { get; private set; }
        }
    }
}
