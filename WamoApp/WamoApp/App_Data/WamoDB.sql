IF DB_ID('WamoDB') IS NULL CREATE DATABASE WamoDB;
GO
USE WamoDB;
GO
CREATE TABLE Roles (RoleID INT IDENTITY(1,1) PRIMARY KEY, RoleName NVARCHAR(100) NOT NULL UNIQUE, Description NVARCHAR(255) NULL, IsSystemRole BIT NOT NULL DEFAULT 0, IsActive BIT NOT NULL DEFAULT 1, CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
GO
CREATE TABLE Users (UserID INT IDENTITY(1,1) PRIMARY KEY, Username NVARCHAR(100) NOT NULL UNIQUE, FullName NVARCHAR(150) NOT NULL, Email NVARCHAR(150) NOT NULL UNIQUE, PhoneNumber NVARCHAR(50) NULL, PasswordHash NVARCHAR(512) NOT NULL, IsActive BIT NOT NULL DEFAULT 1, IsLocked BIT NOT NULL DEFAULT 0, MustChangePassword BIT NOT NULL DEFAULT 0, FailedLoginCount INT NOT NULL DEFAULT 0, LockoutEnd DATETIME2 NULL, IsDeleted BIT NOT NULL DEFAULT 0, CreatedBy INT NULL, CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), ModifiedBy INT NULL, ModifiedDate DATETIME2 NULL);
GO
CREATE TABLE UserRoles (UserRoleID INT IDENTITY(1,1) PRIMARY KEY, UserID INT NOT NULL, RoleID INT NOT NULL, IsActive BIT NOT NULL DEFAULT 1, CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserID) REFERENCES Users(UserID), CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleID) REFERENCES Roles(RoleID), CONSTRAINT UQ_UserRoles UNIQUE (UserID, RoleID));
GO
CREATE TABLE Pages (PageID INT IDENTITY(1,1) PRIMARY KEY, PageName NVARCHAR(150) NOT NULL, PagePath NVARCHAR(200) NOT NULL UNIQUE, MenuGroup NVARCHAR(100) NOT NULL, MenuOrder INT NOT NULL DEFAULT 0, IsActive BIT NOT NULL DEFAULT 1);
GO
CREATE TABLE Permissions (PermissionID INT IDENTITY(1,1) PRIMARY KEY, ModuleKey NVARCHAR(100) NOT NULL, PermissionName NVARCHAR(50) NOT NULL, Description NVARCHAR(255) NULL, IsActive BIT NOT NULL DEFAULT 1, CONSTRAINT UQ_Permissions UNIQUE (ModuleKey, PermissionName));
GO
CREATE TABLE RolePages (RolePageID INT IDENTITY(1,1) PRIMARY KEY, RoleID INT NOT NULL, PageID INT NOT NULL, IsAllowed BIT NOT NULL DEFAULT 1, CONSTRAINT FK_RolePages_Roles FOREIGN KEY (RoleID) REFERENCES Roles(RoleID), CONSTRAINT FK_RolePages_Pages FOREIGN KEY (PageID) REFERENCES Pages(PageID), CONSTRAINT UQ_RolePages UNIQUE (RoleID, PageID));
GO
CREATE TABLE RolePermissions (RolePermissionID INT IDENTITY(1,1) PRIMARY KEY, RoleID INT NOT NULL, PermissionID INT NOT NULL, IsAllowed BIT NOT NULL DEFAULT 1, CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleID) REFERENCES Roles(RoleID), CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionID) REFERENCES Permissions(PermissionID), CONSTRAINT UQ_RolePermissions UNIQUE (RoleID, PermissionID));
GO
CREATE TABLE UserSessions (SessionID UNIQUEIDENTIFIER PRIMARY KEY, UserID INT NOT NULL, SessionTokenHash NVARCHAR(512) NOT NULL, LoginTime DATETIME2 NOT NULL, LastActivity DATETIME2 NOT NULL, LogoutTime DATETIME2 NULL, ExpirationTime DATETIME2 NULL, IPAddress NVARCHAR(64) NULL, UserAgent NVARCHAR(500) NULL, DeviceName NVARCHAR(200) NULL, Browser NVARCHAR(100) NULL, OperatingSystem NVARCHAR(100) NULL, IsActive BIT NOT NULL DEFAULT 1, IsRevoked BIT NOT NULL DEFAULT 0, RevokedAt DATETIME2 NULL, RevokedBy INT NULL, CONSTRAINT FK_UserSessions_Users FOREIGN KEY (UserID) REFERENCES Users(UserID));
GO
CREATE INDEX IX_UserSessions_User_Active ON UserSessions(UserID, IsActive, IsRevoked, LastActivity DESC);
GO
CREATE TABLE LoginAttempts (LoginAttemptID INT IDENTITY(1,1) PRIMARY KEY, Username NVARCHAR(150) NOT NULL, IPAddress NVARCHAR(64) NULL, AttemptTime DATETIME2 NOT NULL, IsSuccessful BIT NOT NULL, FailureReason NVARCHAR(255) NULL);
GO
CREATE TABLE PasswordResetTokens (ResetTokenID INT IDENTITY(1,1) PRIMARY KEY, UserID INT NOT NULL, TokenHash NVARCHAR(512) NOT NULL, ExpiresAt DATETIME2 NOT NULL, IsUsed BIT NOT NULL DEFAULT 0, UsedDate DATETIME2 NULL, CreatedDate DATETIME2 NOT NULL, CONSTRAINT FK_ResetTokens_Users FOREIGN KEY (UserID) REFERENCES Users(UserID));
GO
CREATE TABLE AuditLogs (AuditID INT IDENTITY(1,1) PRIMARY KEY, UserID INT NULL, Action NVARCHAR(50) NOT NULL, Module NVARCHAR(100) NULL, Page NVARCHAR(200) NULL, RecordID NVARCHAR(100) NULL, OldValue NVARCHAR(MAX) NULL, NewValue NVARCHAR(MAX) NULL, IPAddress NVARCHAR(64) NULL, UserAgent NVARCHAR(500) NULL, CreatedDate DATETIME2 NOT NULL, CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserID) REFERENCES Users(UserID));
GO
CREATE TABLE Languages (LanguageID INT IDENTITY(1,1) PRIMARY KEY, LanguageCode NVARCHAR(10) NOT NULL UNIQUE, Name NVARCHAR(100) NOT NULL, NativeName NVARCHAR(100) NOT NULL, IsDefault BIT NOT NULL DEFAULT 0, IsRtl BIT NOT NULL DEFAULT 0, IsActive BIT NOT NULL DEFAULT 1, SortOrder INT NOT NULL DEFAULT 0);
GO
CREATE TABLE Translations (TranslationID INT IDENTITY(1,1) PRIMARY KEY, LanguageCode NVARCHAR(10) NOT NULL, TranslationKey NVARCHAR(150) NOT NULL, TranslationValue NVARCHAR(500) NOT NULL, CONSTRAINT UQ_Translations UNIQUE (LanguageCode, TranslationKey), CONSTRAINT FK_Translations_Languages FOREIGN KEY (LanguageCode) REFERENCES Languages(LanguageCode));
GO
CREATE TABLE Sections (SectionID INT IDENTITY(1,1) PRIMARY KEY, SectionName NVARCHAR(100) NOT NULL UNIQUE, Code NVARCHAR(20) NULL, IsActive BIT NOT NULL DEFAULT 1, CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
GO
CREATE TABLE Blocks (BlockID INT IDENTITY(1,1) PRIMARY KEY, SectionID INT NOT NULL, BlockNumber NVARCHAR(20) NOT NULL, IsActive BIT NOT NULL DEFAULT 1, CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), CONSTRAINT FK_Blocks_Sections FOREIGN KEY (SectionID) REFERENCES Sections(SectionID), CONSTRAINT UQ_Blocks UNIQUE (SectionID, BlockNumber));
GO
CREATE TABLE Houses (HouseID INT IDENTITY(1,1) PRIMARY KEY, BlockID INT NOT NULL, HouseNumber NVARCHAR(20) NOT NULL, Latitude NVARCHAR(30) NULL, Longitude NVARCHAR(30) NULL, IsActive BIT NOT NULL DEFAULT 1, CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), CONSTRAINT FK_Houses_Blocks FOREIGN KEY (BlockID) REFERENCES Blocks(BlockID), CONSTRAINT UQ_Houses UNIQUE (BlockID, HouseNumber));
GO
CREATE TABLE Customers (CustomerID INT IDENTITY(1,1) PRIMARY KEY, LinkedUserID INT NULL, CustomerNumber NVARCHAR(30) NOT NULL UNIQUE, FullName NVARCHAR(150) NOT NULL, PhoneNumber NVARCHAR(50) NOT NULL, WhatsAppNumber NVARCHAR(50) NULL, Email NVARCHAR(150) NULL, SectionID INT NULL, BlockID INT NULL, HouseID INT NULL, CustomerType NVARCHAR(50) NOT NULL, ServiceName NVARCHAR(100) NULL, PreferredSchedule NVARCHAR(100) NULL, Latitude NVARCHAR(30) NULL, Longitude NVARCHAR(30) NULL, AddressLine NVARCHAR(255) NOT NULL, Notes NVARCHAR(1000) NULL, Status NVARCHAR(50) NOT NULL DEFAULT 'Pending', CreatedBy INT NULL, CreatedDate DATETIME2 NOT NULL, ModifiedBy INT NULL, ModifiedDate DATETIME2 NULL, CONSTRAINT FK_Customers_User FOREIGN KEY (LinkedUserID) REFERENCES Users(UserID), CONSTRAINT FK_Customers_Sections FOREIGN KEY (SectionID) REFERENCES Sections(SectionID), CONSTRAINT FK_Customers_Blocks FOREIGN KEY (BlockID) REFERENCES Blocks(BlockID), CONSTRAINT FK_Customers_Houses FOREIGN KEY (HouseID) REFERENCES Houses(HouseID));
GO
CREATE TABLE CustomerServices (CustomerServiceID INT IDENTITY(1,1) PRIMARY KEY, CustomerID INT NOT NULL, ServiceName NVARCHAR(100) NOT NULL, Status NVARCHAR(50) NOT NULL DEFAULT 'Active', StartDate DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE), EndDate DATE NULL, CONSTRAINT FK_CustomerServices_Customers FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID));
GO
CREATE TABLE CollectionSchedules (ScheduleID INT IDENTITY(1,1) PRIMARY KEY, CustomerID INT NOT NULL, ScheduleName NVARCHAR(100) NOT NULL, NextCollectionDate DATE NULL, LastCollectionDate DATE NULL, Status NVARCHAR(50) NOT NULL DEFAULT 'Active', CONSTRAINT FK_CollectionSchedules_Customers FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID));
GO
CREATE TABLE CollectionRequests (RequestID INT IDENTITY(1,1) PRIMARY KEY, RequestNumber NVARCHAR(30) NOT NULL UNIQUE, CustomerID INT NULL, ServiceName NVARCHAR(100) NOT NULL, AddressLine NVARCHAR(255) NOT NULL, PreferredDate DATE NOT NULL, PreferredTime NVARCHAR(20) NULL, Description NVARCHAR(1000) NULL, PhotoUrl NVARCHAR(500) NULL, Latitude NVARCHAR(30) NULL, Longitude NVARCHAR(30) NULL, Status NVARCHAR(50) NOT NULL DEFAULT 'Pending', CreatedDate DATETIME2 NOT NULL, CONSTRAINT FK_CollectionRequests_Users FOREIGN KEY (CustomerID) REFERENCES Users(UserID));
GO
CREATE TABLE CollectionAssignments (AssignmentID INT IDENTITY(1,1) PRIMARY KEY, RequestID INT NOT NULL, AssignedUserID INT NOT NULL, AssignedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), Status NVARCHAR(50) NOT NULL DEFAULT 'Assigned', CONSTRAINT FK_Assignment_Request FOREIGN KEY (RequestID) REFERENCES CollectionRequests(RequestID), CONSTRAINT FK_Assignment_User FOREIGN KEY (AssignedUserID) REFERENCES Users(UserID));
GO
CREATE TABLE Collections (CollectionID INT IDENTITY(1,1) PRIMARY KEY, CustomerID INT NULL, ServiceName NVARCHAR(100) NOT NULL, CollectionDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), Status NVARCHAR(50) NOT NULL DEFAULT 'Pending', Notes NVARCHAR(1000) NULL, CONSTRAINT FK_Collections_Customers FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID));
GO
CREATE TABLE PaymentMethods (PaymentMethodID INT IDENTITY(1,1) PRIMARY KEY, MethodName NVARCHAR(100) NOT NULL UNIQUE, IsActive BIT NOT NULL DEFAULT 1);
GO
CREATE TABLE Invoices (InvoiceID INT IDENTITY(1,1) PRIMARY KEY, CustomerID INT NOT NULL, InvoiceNumber NVARCHAR(30) NOT NULL UNIQUE, BillingPeriod NVARCHAR(50) NOT NULL, AmountDue DECIMAL(18,2) NOT NULL, AmountPaid DECIMAL(18,2) NOT NULL DEFAULT 0, BalanceAmount AS (AmountDue - AmountPaid), DueDate DATE NOT NULL, Status NVARCHAR(50) NOT NULL DEFAULT 'Pending', CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), CONSTRAINT FK_Invoices_Customers FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID));
GO
CREATE TABLE Payments (PaymentID INT IDENTITY(1,1) PRIMARY KEY, InvoiceID INT NOT NULL, PaymentMethodID INT NULL, AmountPaid DECIMAL(18,2) NOT NULL, PaymentDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), Status NVARCHAR(50) NOT NULL DEFAULT 'Paid', TransactionReference NVARCHAR(100) NULL, CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), CONSTRAINT FK_Payments_Invoices FOREIGN KEY (InvoiceID) REFERENCES Invoices(InvoiceID), CONSTRAINT FK_Payments_Methods FOREIGN KEY (PaymentMethodID) REFERENCES PaymentMethods(PaymentMethodID));
GO
CREATE VIEW PaymentsView AS SELECT i.CustomerID AS UserID, i.InvoiceNumber, i.AmountDue, p.AmountPaid, (i.AmountDue - p.AmountPaid) AS BalanceAmount, p.PaymentDate, p.Status FROM Payments p INNER JOIN Invoices i ON i.InvoiceID = p.InvoiceID;
GO
CREATE TABLE Complaints (ComplaintID INT IDENTITY(1,1) PRIMARY KEY, CustomerID INT NULL, ComplaintNumber NVARCHAR(30) NOT NULL UNIQUE, Subject NVARCHAR(150) NOT NULL, Description NVARCHAR(2000) NOT NULL, Priority NVARCHAR(50) NOT NULL DEFAULT 'Medium', Status NVARCHAR(50) NOT NULL DEFAULT 'Open', AssignedTo INT NULL, Response NVARCHAR(2000) NULL, CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), ResolvedDate DATETIME2 NULL, CONSTRAINT FK_Complaints_Users FOREIGN KEY (CustomerID) REFERENCES Users(UserID), CONSTRAINT FK_Complaints_Assigned FOREIGN KEY (AssignedTo) REFERENCES Users(UserID));
GO
CREATE TABLE ComplaintAttachments (AttachmentID INT IDENTITY(1,1) PRIMARY KEY, ComplaintID INT NOT NULL, FileName NVARCHAR(255) NOT NULL, FilePath NVARCHAR(500) NOT NULL, CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), CONSTRAINT FK_ComplaintAttachments_Complaints FOREIGN KEY (ComplaintID) REFERENCES Complaints(ComplaintID));
GO
CREATE TABLE ProductCategories (CategoryID INT IDENTITY(1,1) PRIMARY KEY, CategoryName NVARCHAR(100) NOT NULL UNIQUE, IsActive BIT NOT NULL DEFAULT 1);
GO
CREATE TABLE Products (ProductID INT IDENTITY(1,1) PRIMARY KEY, CategoryID INT NOT NULL, ProductName NVARCHAR(150) NOT NULL, Description NVARCHAR(1000) NULL, Price DECIMAL(18,2) NOT NULL, StockQuantity INT NOT NULL DEFAULT 0, ImageUrl NVARCHAR(500) NULL, IsActive BIT NOT NULL DEFAULT 1, CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), CONSTRAINT FK_Products_ProductCategories FOREIGN KEY (CategoryID) REFERENCES ProductCategories(CategoryID));
GO
CREATE TABLE ShopOrders (OrderID INT IDENTITY(1,1) PRIMARY KEY, CustomerID INT NULL, OrderNumber NVARCHAR(30) NOT NULL UNIQUE, WhatsAppNumber NVARCHAR(50) NULL, TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0, Status NVARCHAR(50) NOT NULL DEFAULT 'Pending', OrderDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), CONSTRAINT FK_ShopOrders_Users FOREIGN KEY (CustomerID) REFERENCES Users(UserID));
GO
CREATE TABLE ShopOrderItems (OrderItemID INT IDENTITY(1,1) PRIMARY KEY, OrderID INT NOT NULL, ProductID INT NOT NULL, Quantity INT NOT NULL CHECK (Quantity > 0), UnitPrice DECIMAL(18,2) NOT NULL, CONSTRAINT FK_OrderItems_Order FOREIGN KEY (OrderID) REFERENCES ShopOrders(OrderID), CONSTRAINT FK_OrderItems_Product FOREIGN KEY (ProductID) REFERENCES Products(ProductID));
GO
CREATE TABLE BlogCategories (CategoryID INT IDENTITY(1,1) PRIMARY KEY, CategoryName NVARCHAR(100) NOT NULL UNIQUE, IsActive BIT NOT NULL DEFAULT 1);
GO
CREATE TABLE BlogPosts (PostID INT IDENTITY(1,1) PRIMARY KEY, CategoryID INT NOT NULL, Title NVARCHAR(200) NOT NULL, Slug NVARCHAR(200) NOT NULL UNIQUE, Summary NVARCHAR(500) NULL, Content NVARCHAR(MAX) NOT NULL, LanguageCode NVARCHAR(10) NOT NULL, IsFeatured BIT NOT NULL DEFAULT 0, IsPublished BIT NOT NULL DEFAULT 1, PublishedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), CONSTRAINT FK_BlogPosts_Category FOREIGN KEY (CategoryID) REFERENCES BlogCategories(CategoryID), CONSTRAINT FK_BlogPosts_Language FOREIGN KEY (LanguageCode) REFERENCES Languages(LanguageCode));
GO
CREATE TABLE Announcements (AnnouncementID INT IDENTITY(1,1) PRIMARY KEY, Title NVARCHAR(200) NOT NULL, Content NVARCHAR(1000) NOT NULL, LanguageCode NVARCHAR(10) NOT NULL DEFAULT 'en', StartDate DATETIME2 NULL, EndDate DATETIME2 NULL, IsActive BIT NOT NULL DEFAULT 1, CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), CONSTRAINT FK_Announcements_Language FOREIGN KEY (LanguageCode) REFERENCES Languages(LanguageCode));
GO
CREATE TABLE Notifications (NotificationID INT IDENTITY(1,1) PRIMARY KEY, UserID INT NOT NULL, NotificationType NVARCHAR(100) NOT NULL, Title NVARCHAR(200) NOT NULL, Message NVARCHAR(1000) NOT NULL, IsRead BIT NOT NULL DEFAULT 0, CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserID) REFERENCES Users(UserID));
GO
CREATE TABLE SystemSettings (SettingID INT IDENTITY(1,1) PRIMARY KEY, SettingKey NVARCHAR(100) NOT NULL UNIQUE, SettingValue NVARCHAR(1000) NULL, Category NVARCHAR(100) NULL, Description NVARCHAR(255) NULL, IsEncrypted BIT NOT NULL DEFAULT 0, ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
GO
INSERT INTO Roles (RoleName, Description, IsSystemRole) VALUES ('Super Admin','Full platform access',1),('Administrator','General administration',1),('Manager','Management user',0),('Collection Manager','Collection planning',0),('Collection Staff','Collection operations',0),('Finance','Billing and payments',0),('Customer Support','Complaints and customer care',0),('Shop Manager','Product and orders',0),('Content Manager','Blog and announcements',0),('Customer','Customer portal access',1);
GO
INSERT INTO Languages (LanguageCode, Name, NativeName, IsDefault, IsRtl, SortOrder) VALUES ('en','English','English',1,0,1),('so','Somali','Soomaali',0,0,2),('ar','Arabic','العربية',0,1,3);
GO
INSERT INTO Translations (LanguageCode, TranslationKey, TranslationValue) VALUES ('en','login','Login'),('so','login','Galitaanka'),('ar','login','تسجيل الدخول'),('en','register','Register'),('so','register','Diiwaangelin'),('ar','register','تسجيل');
GO
INSERT INTO Sections (SectionName, Code) VALUES ('Farjano','FAR'),('Hodan','HOD'),('Waberi','WAB');
GO
INSERT INTO Blocks (SectionID, BlockNumber) VALUES (1,'1'),(1,'12'),(2,'2'),(3,'13');
GO
INSERT INTO Houses (BlockID, HouseNumber, Latitude, Longitude) VALUES (2,'0345','-0.358','42.545'),(2,'0346','-0.359','42.546'),(4,'0347','-0.360','42.547');
GO
INSERT INTO ProductCategories (CategoryName) VALUES ('Garbage Bins'),('Waste Bags'),('Cleaning Materials'),('Gloves'),('Brooms'),('Dustpans'),('Recycling Products'),('Environmental Products');
GO
INSERT INTO Products (CategoryID, ProductName, Description, Price, StockQuantity, IsActive) VALUES (1,'120L Garbage Bin','Durable outdoor waste bin',35.00,25,1),(2,'Heavy Duty Waste Bags','Industrial black waste bags pack',12.00,100,1),(3,'Cleaning Kit','Brush, detergent, and gloves set',18.50,40,1),(7,'Recycling Box','Home recycling storage box',22.00,15,1);
GO
INSERT INTO BlogCategories (CategoryName) VALUES ('Cleaning'),('Environment'),('Waste & Health'),('Tree Planting'),('Flood Prevention'),('Recycling'),('WAMO Announcements'),('Community Cleaning Campaigns');
GO
INSERT INTO BlogPosts (CategoryID, Title, Slug, Summary, Content, LanguageCode, IsFeatured, IsPublished) VALUES (2,'Cleaner Cities Start at Home','cleaner-cities-start-at-home','Daily habits improve public hygiene.','Practical waste separation and disposal guidance for households.','en',1,1),(6,'Recycling for Community Growth','recycling-for-community-growth','Recycling creates healthier neighborhoods.','Community-led recycling reduces landfill pressure and supports cleaner environments.','en',0,1);
GO
INSERT INTO Announcements (Title, Content, LanguageCode, StartDate, EndDate, IsActive) VALUES ('Weekly Collection Notice','Weekly household collection runs every Monday and Thursday.','en',SYSUTCDATETIME(),DATEADD(DAY,30,SYSUTCDATETIME()),1),('Community Cleanup','Join the weekend neighborhood cleanup campaign.','en',SYSUTCDATETIME(),DATEADD(DAY,15,SYSUTCDATETIME()),1);
GO
INSERT INTO Pages (PageName, PagePath, MenuGroup, MenuOrder) VALUES ('Dashboard','Admin/Dashboard.aspx','Overview',1),('Customers','Admin/Customers.aspx','Customers',10),('Sections','Admin/Sections.aspx','Customers',11),('Blocks','Admin/Blocks.aspx','Customers',12),('Houses','Admin/Houses.aspx','Customers',13),('Payments','Admin/Payments.aspx','Finance',20),('Collections','Admin/Collections.aspx','Collection',30),('Collection Requests','Admin/CollectionRequests.aspx','Collection',31),('Complaints','Admin/Complaints.aspx','Support',40),('Shop Orders','Admin/ShopOrders.aspx','Shop',50),('Products','Admin/Products.aspx','Shop',51),('Blog','Admin/Blog.aspx','Content',60),('Announcements','Admin/Announcements.aspx','Content',61),('Users','Admin/Users.aspx','Security',70),('Roles','Admin/Roles.aspx','Security',71),('Permissions','Admin/Permissions.aspx','Security',72),('Sessions','Admin/Sessions.aspx','Security',73),('Audit Logs','Admin/AuditLogs.aspx','Security',74),('Languages','Admin/Languages.aspx','Settings',80),('Settings','Admin/Settings.aspx','Settings',81);
GO
DECLARE @Modules TABLE (ModuleKey NVARCHAR(100)); INSERT INTO @Modules VALUES ('Dashboard'),('Customers'),('Sections'),('Blocks'),('Houses'),('Payments'),('Collections'),('CollectionRequests'),('Complaints'),('ShopOrders'),('Products'),('Blog'),('Announcements'),('Users'),('Roles'),('Permissions'),('Sessions'),('AuditLogs'),('Languages'),('Settings');
INSERT INTO Permissions (ModuleKey, PermissionName, Description) SELECT ModuleKey, PermissionName, ModuleKey + ' ' + PermissionName FROM @Modules CROSS JOIN (VALUES ('View'),('Create'),('Edit'),('Delete'),('Approve'),('Export'),('Print')) p(PermissionName);
GO
DECLARE @SuperAdminRoleId INT = (SELECT RoleID FROM Roles WHERE RoleName = 'Super Admin'); INSERT INTO RolePages (RoleID, PageID, IsAllowed) SELECT @SuperAdminRoleId, PageID, 1 FROM Pages; INSERT INTO RolePermissions (RoleID, PermissionID, IsAllowed) SELECT @SuperAdminRoleId, PermissionID, 1 FROM Permissions;
GO
INSERT INTO Users (Username, FullName, Email, PhoneNumber, PasswordHash, IsActive, MustChangePassword, CreatedDate) VALUES ('superadmin','WAMO Super Admin','admin@wamo.local','+252611234567','PBKDF2$sha1$120000$rNdvI6ass9WNeEoUmWBb6w==$/eG/RYbBmCPA/eLoabR0MJ6cVvP+U2r/xbM80Wk+J78=',1,1,SYSUTCDATETIME()),('ahmed.hassan','Ahmed Hassan','ahmed@example.com','+252610000001','PBKDF2$sha1$120000$rNdvI6ass9WNeEoUmWBb6w==$/eG/RYbBmCPA/eLoabR0MJ6cVvP+U2r/xbM80Wk+J78=',1,0,SYSUTCDATETIME());
GO
INSERT INTO UserRoles (UserID, RoleID, IsActive) SELECT u.UserID, r.RoleID, 1 FROM Users u CROSS JOIN Roles r WHERE (u.Username = 'superadmin' AND r.RoleName = 'Super Admin') OR (u.Username = 'ahmed.hassan' AND r.RoleName = 'Customer');
GO
INSERT INTO Customers (LinkedUserID, CustomerNumber, FullName, PhoneNumber, WhatsAppNumber, Email, SectionID, BlockID, HouseID, CustomerType, ServiceName, PreferredSchedule, Latitude, Longitude, AddressLine, Notes, Status, CreatedDate) SELECT u.UserID,'WAMO-2026-000001','Ahmed Hassan','+252610000001','+252610000001','ahmed@example.com',1,2,1,'Household','Weekly','Weekly','-0.358','42.545','Farjano Block 12 House 0345','Seed customer','Active',SYSUTCDATETIME() FROM Users u WHERE u.Username = 'ahmed.hassan';
GO
INSERT INTO CustomerServices (CustomerID, ServiceName, Status) SELECT CustomerID, 'Weekly', 'Active' FROM Customers WHERE CustomerNumber = 'WAMO-2026-000001';
INSERT INTO CollectionSchedules (CustomerID, ScheduleName, NextCollectionDate, LastCollectionDate, Status) SELECT CustomerID, 'Weekly', DATEADD(DAY,7,CAST(GETDATE() AS date)), CAST(GETDATE() AS date), 'Active' FROM Customers WHERE CustomerNumber = 'WAMO-2026-000001';
INSERT INTO Invoices (CustomerID, InvoiceNumber, BillingPeriod, AmountDue, AmountPaid, DueDate, Status) SELECT CustomerID, 'INV-2026-000001', 'Monthly', 15.00, 15.00, DATEADD(DAY,30,CAST(GETDATE() AS date)), 'Paid' FROM Customers WHERE CustomerNumber = 'WAMO-2026-000001';
INSERT INTO PaymentMethods (MethodName) VALUES ('Cash'),('Mobile Money'),('Bank Transfer');
INSERT INTO Payments (InvoiceID, PaymentMethodID, AmountPaid, PaymentDate, Status) SELECT InvoiceID, 2, 15.00, SYSUTCDATETIME(), 'Paid' FROM Invoices WHERE InvoiceNumber = 'INV-2026-000001';
INSERT INTO Notifications (UserID, NotificationType, Title, Message) SELECT LinkedUserID, 'Announcement', 'Welcome to WAMO', 'Your account is active and ready.' FROM Customers WHERE CustomerNumber = 'WAMO-2026-000001';
GO
INSERT INTO SystemSettings (SettingKey, SettingValue, Category, Description, IsEncrypted) VALUES ('WhatsAppNumber','252611234567','Communication','Primary WhatsApp contact number',0),('WhatsAppMessageTemplate','Hello WAMO, I need assistance regarding {{topic}}.','Communication','Default WhatsApp template',0),('SupportPhone','+252611234567','Communication','Primary support phone number',0),('SupportEmail','info@wamo.local','Communication','Primary support email',0),('FacebookUrl','https://facebook.com/wamo','Communication','Official Facebook page',0),('TikTokUrl','https://tiktok.com/@wamo','Communication','Official TikTok page',0),('OfficeLocation','Kismayo, Somalia','Communication','Primary office location',0),('SessionInactivityMinutes','1440','Security','Configurable inactivity timeout in minutes',0),('SmtpHost','smtp.example.com','Email','SMTP host placeholder',0),('RequireHttps','false','Security','Set true in production behind HTTPS',0);
GO
