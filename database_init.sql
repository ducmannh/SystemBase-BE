-- =========================================================================================
-- DỰ ÁN: SystemBase (Quản trị hệ thống & Phân quyền)
-- TẬP TIN: database_init.sql
-- MỤC ĐÍCH: Khởi tạo toàn bộ cấu trúc bảng và dữ liệu mẫu (Seed Data) cho Microsoft SQL Server
-- TÀI KHOẢN MẪU:
--   1. Admin   : Tài khoản: admin    | Mật khẩu: Admin@123456 (Toàn quyền hệ thống)
--   2. Manager : Tài khoản: manager  | Mật khẩu: Admin@123456 (Quản lý User & Xem Log)
--   3. User    : Tài khoản: user1    | Mật khẩu: User@123456  (Xem Dashboard)
-- =========================================================================================

SET NOCOUNT ON;
GO

-- =========================================================================================
-- 1. TẠO CÁC BẢNG (TABLES)
-- =========================================================================================

-- 1.1. Bảng User (Người dùng)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[User]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[User] (
        [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_User] PRIMARY KEY,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL,
        [UserCreated] UNIQUEIDENTIFIER NOT NULL,
        [UserModified] UNIQUEIDENTIFIER NOT NULL,
        [UserName] NVARCHAR(500) NOT NULL,
        [PasswordHashed] NVARCHAR(MAX) NOT NULL,
        [Email] NVARCHAR(500) NULL,
        [RefreshToken] NVARCHAR(MAX) NULL,
        [RefreshTokenExpired] DATETIME2 NULL,
        [Name] NVARCHAR(500) NULL,
        [PhoneNumber] NVARCHAR(50) NULL,
        [AvatarPath] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_User_IsActive] DEFAULT 1,
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_User_IsDeleted] DEFAULT 0,
        [LastPasswordChangedAt] DATETIME2 NULL,
        [FailedAccessAttempts] INT NOT NULL CONSTRAINT [DF_User_FailedAttempts] DEFAULT 0,
        [LockoutEnd] DATETIME2 NULL,
        [RequiresPasswordChange] BIT NOT NULL CONSTRAINT [DF_User_ReqPassChange] DEFAULT 0,
        [ResetPasswordCode] NVARCHAR(MAX) NULL,
        [ResetPasswordCodeExpiredAt] DATETIME2 NULL
    );
    PRINT 'Đã tạo bảng [User]';
END
GO

-- 1.2. Bảng SystemRole (Vai trò hệ thống)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SystemRole]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SystemRole] (
        [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_SystemRole] PRIMARY KEY,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL,
        [UserCreated] UNIQUEIDENTIFIER NOT NULL,
        [UserModified] UNIQUEIDENTIFIER NOT NULL,
        [Code] NVARCHAR(255) NOT NULL,
        [Name] NVARCHAR(255) NOT NULL,
        [Description] NVARCHAR(500) NOT NULL,
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_SystemRole_IsDeleted] DEFAULT 0
    );
    PRINT 'Đã tạo bảng [SystemRole]';
END
GO

-- 1.3. Bảng SystemFunction (Chức năng & Menu hệ thống)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SystemFunction]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SystemFunction] (
        [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_SystemFunction] PRIMARY KEY,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL,
        [UserCreated] UNIQUEIDENTIFIER NOT NULL,
        [UserModified] UNIQUEIDENTIFIER NOT NULL,
        [Code] NVARCHAR(255) NOT NULL,
        [Name] NVARCHAR(255) NOT NULL,
        [Url] NVARCHAR(500) NOT NULL,
        [Order] INT NOT NULL CONSTRAINT [DF_SystemFunction_Order] DEFAULT 0,
        [Type] NVARCHAR(50) NOT NULL, -- 'menu' hoặc 'button'
        [Icon] NVARCHAR(100) NOT NULL,
        [IsShow] BIT NOT NULL CONSTRAINT [DF_SystemFunction_IsShow] DEFAULT 1,
        [ParentId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_SystemFunction_ParentId] DEFAULT '00000000-0000-0000-0000-000000000000',
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_SystemFunction_IsDeleted] DEFAULT 0
    );
    PRINT 'Đã tạo bảng [SystemFunction]';
END
GO

-- 1.4. Bảng liên kết Phân quyền: SystemRoleFunctions (Vai trò - Chức năng/Quyền)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SystemRoleFunctions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SystemRoleFunctions] (
        [RoleId] UNIQUEIDENTIFIER NOT NULL,
        [FunctionId] UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT [PK_SystemRoleFunctions] PRIMARY KEY CLUSTERED ([RoleId], [FunctionId])
    );
    PRINT 'Đã tạo bảng [SystemRoleFunctions]';
END
GO

-- 1.5. Bảng liên kết Người dùng: UserRoles (Người dùng - Vai trò)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserRoles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserRoles] (
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [RoleId] UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([UserId], [RoleId])
    );
    PRINT 'Đã tạo bảng [UserRoles]';
END
GO

-- 1.6. Bảng SystemSecuritySettings (Cấu hình chính sách bảo mật hệ thống)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SystemSecuritySettings]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SystemSecuritySettings] (
        [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_SystemSecuritySettings] PRIMARY KEY,
        [MinPasswordLength] INT NOT NULL CONSTRAINT [DF_Sec_MinPass] DEFAULT 8,
        [RequireUppercase] BIT NOT NULL CONSTRAINT [DF_Sec_ReqUpper] DEFAULT 1,
        [RequireLowercase] BIT NOT NULL CONSTRAINT [DF_Sec_ReqLower] DEFAULT 1,
        [RequireNumber] BIT NOT NULL CONSTRAINT [DF_Sec_ReqNumber] DEFAULT 1,
        [RequireSpecialCharacter] BIT NOT NULL CONSTRAINT [DF_Sec_ReqSpecial] DEFAULT 1,
        [PasswordExpiryDays] INT NOT NULL CONSTRAINT [DF_Sec_PassExpiry] DEFAULT 90,
        [MaxFailedAccessAttempts] INT NOT NULL CONSTRAINT [DF_Sec_MaxFailed] DEFAULT 5,
        [LockoutDurationMinutes] INT NOT NULL CONSTRAINT [DF_Sec_LockoutMin] DEFAULT 15,
        [AllowedAdminIPs] NVARCHAR(MAX) NULL
    );
    PRINT 'Đã tạo bảng [SystemSecuritySettings]';
END
GO

-- 1.7. Bảng ActionLogs (Nhật ký thao tác / Audit Log)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ActionLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ActionLogs] (
        [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_ActionLogs] PRIMARY KEY,
        [UserId] UNIQUEIDENTIFIER NULL,
        [UserName] NVARCHAR(100) NOT NULL CONSTRAINT [DF_ActionLogs_UserName] DEFAULT '',
        [Action] NVARCHAR(50) NOT NULL CONSTRAINT [DF_ActionLogs_Action] DEFAULT '',
        [Module] NVARCHAR(100) NOT NULL CONSTRAINT [DF_ActionLogs_Module] DEFAULT '',
        [Description] NVARCHAR(500) NOT NULL CONSTRAINT [DF_ActionLogs_Description] DEFAULT '',
        [IpAddress] NVARCHAR(50) NOT NULL CONSTRAINT [DF_ActionLogs_IpAddress] DEFAULT '',
        [CreatedAt] DATETIME2 NOT NULL
    );
    PRINT 'Đã tạo bảng [ActionLogs]';
END
GO

-- 1.8. Bảng LoginLogs (Nhật ký đăng nhập)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LoginLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[LoginLogs] (
        [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_LoginLogs] PRIMARY KEY,
        [UserId] UNIQUEIDENTIFIER NULL,
        [UserName] NVARCHAR(100) NOT NULL CONSTRAINT [DF_LoginLogs_UserName] DEFAULT '',
        [IpAddress] NVARCHAR(50) NOT NULL CONSTRAINT [DF_LoginLogs_IpAddress] DEFAULT '',
        [UserAgent] NVARCHAR(500) NOT NULL CONSTRAINT [DF_LoginLogs_UserAgent] DEFAULT '',
        [Status] NVARCHAR(20) NOT NULL CONSTRAINT [DF_LoginLogs_Status] DEFAULT '',
        [Message] NVARCHAR(255) NOT NULL CONSTRAINT [DF_LoginLogs_Message] DEFAULT '',
        [CreatedAt] DATETIME2 NOT NULL
    );
    PRINT 'Đã tạo bảng [LoginLogs]';
END
GO

-- 1.9. Bảng ErrorLogs (Nhật ký lỗi hệ thống)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ErrorLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ErrorLogs] (
        [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_ErrorLogs] PRIMARY KEY,
        [UserId] UNIQUEIDENTIFIER NULL,
        [UserName] NVARCHAR(100) NOT NULL CONSTRAINT [DF_ErrorLogs_UserName] DEFAULT '',
        [Message] NVARCHAR(1000) NOT NULL CONSTRAINT [DF_ErrorLogs_Message] DEFAULT '',
        [StackTrace] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_ErrorLogs_StackTrace] DEFAULT '',
        [Path] NVARCHAR(500) NOT NULL CONSTRAINT [DF_ErrorLogs_Path] DEFAULT '',
        [IpAddress] NVARCHAR(50) NOT NULL CONSTRAINT [DF_ErrorLogs_IpAddress] DEFAULT '',
        [CreatedAt] DATETIME2 NOT NULL
    );
    PRINT 'Đã tạo bảng [ErrorLogs]';
END
GO


-- =========================================================================================
-- 2. THÊM DỮ LIỆU MẪU (SEED DATA)
-- =========================================================================================

DECLARE @Now DATETIME2 = DATEADD(HOUR, 7, GETUTCDATE()); -- Giờ Việt Nam (UTC+7)
DECLARE @EmptyGuid UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000000';

-- =========================================================================================
-- 2.1. Cấu hình bảo mật mặc định (SystemSecuritySettings)
-- =========================================================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemSecuritySettings])
BEGIN
    INSERT INTO [dbo].[SystemSecuritySettings] (
        [Id], [MinPasswordLength], [RequireUppercase], [RequireLowercase], [RequireNumber], 
        [RequireSpecialCharacter], [PasswordExpiryDays], [MaxFailedAccessAttempts], 
        [LockoutDurationMinutes], [AllowedAdminIPs]
    )
    VALUES (
        '11111111-1111-1111-1111-111111111111', 8, 1, 1, 1, 1, 90, 5, 15, NULL
    );
    PRINT 'Đã chèn cấu hình bảo mật mặc định.';
END
GO

-- =========================================================================================
-- 2.2. Vai trò mẫu (SystemRole)
-- =========================================================================================
DECLARE @Now DATETIME2 = DATEADD(HOUR, 7, GETUTCDATE());
DECLARE @EmptyGuid UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000000';

-- Role 1: ADMIN (Quản trị viên tối cao)
DECLARE @RoleId_Admin UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000001';
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemRole] WHERE [Id] = @RoleId_Admin OR [Code] = 'ADMIN')
BEGIN
    INSERT INTO [dbo].[SystemRole] ([Id], [CreatedAt], [UpdatedAt], [UserCreated], [UserModified], [Code], [Name], [Description], [IsDeleted])
    VALUES (@RoleId_Admin, @Now, @Now, @EmptyGuid, @EmptyGuid, 'ADMIN', N'Quản trị viên tối cao', N'Có toàn bộ quyền hạn quản trị hệ thống', 0);
END

-- Role 2: MANAGER (Quản lý)
DECLARE @RoleId_Manager UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000002';
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemRole] WHERE [Id] = @RoleId_Manager OR [Code] = 'MANAGER')
BEGIN
    INSERT INTO [dbo].[SystemRole] ([Id], [CreatedAt], [UpdatedAt], [UserCreated], [UserModified], [Code], [Name], [Description], [IsDeleted])
    VALUES (@RoleId_Manager, @Now, @Now, @EmptyGuid, @EmptyGuid, 'MANAGER', N'Quản lý hệ thống', N'Quản lý người dùng, xem log và báo cáo', 0);
END

-- Role 3: USER (Người dùng cơ bản)
DECLARE @RoleId_User UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000003';
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemRole] WHERE [Id] = @RoleId_User OR [Code] = 'USER')
BEGIN
    INSERT INTO [dbo].[SystemRole] ([Id], [CreatedAt], [UpdatedAt], [UserCreated], [UserModified], [Code], [Name], [Description], [IsDeleted])
    VALUES (@RoleId_User, @Now, @Now, @EmptyGuid, @EmptyGuid, 'USER', N'Người dùng tiêu chuẩn', N'Người dùng bình thường truy cập CMS', 0);
END
GO

-- =========================================================================================
-- 2.3. Người dùng mẫu (User)
--   Mật khẩu băm chuẩn BCrypt:
--   - Mật khẩu 'Admin@123456': $2a$11$fLtPxg2tVq.Y3JHfCkv8XuZMi3U/SOejdv3E/MkaO650cLbIBFFjO
--   - Mật khẩu 'User@123456' : $2a$11$f4WcJ4xUJyzhgblqb8X/e.ILgTXcRnF.yZsETfOaR2HNGt3PtQx1.
-- =========================================================================================
DECLARE @Now DATETIME2 = DATEADD(HOUR, 7, GETUTCDATE());
DECLARE @EmptyGuid UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000000';

-- User 1: admin
DECLARE @UserId_Admin UNIQUEIDENTIFIER = 'B0000000-0000-0000-0000-000000000001';
IF NOT EXISTS (SELECT 1 FROM [dbo].[User] WHERE [UserName] = 'admin')
BEGIN
    INSERT INTO [dbo].[User] (
        [Id], [CreatedAt], [UpdatedAt], [UserCreated], [UserModified], [UserName], [PasswordHashed],
        [Email], [Name], [PhoneNumber], [AvatarPath], [IsActive], [IsDeleted], [LastPasswordChangedAt],
        [FailedAccessAttempts], [RequiresPasswordChange]
    )
    VALUES (
        @UserId_Admin, @Now, @Now, @EmptyGuid, @EmptyGuid, 'admin',
        '$2a$11$fLtPxg2tVq.Y3JHfCkv8XuZMi3U/SOejdv3E/MkaO650cLbIBFFjO',
        'admin@systembase.com', N'Quản trị viên Hệ thống', '0988888888', NULL, 1, 0, @Now, 0, 0
    );
END

-- User 2: manager
DECLARE @UserId_Manager UNIQUEIDENTIFIER = 'B0000000-0000-0000-0000-000000000002';
IF NOT EXISTS (SELECT 1 FROM [dbo].[User] WHERE [UserName] = 'manager')
BEGIN
    INSERT INTO [dbo].[User] (
        [Id], [CreatedAt], [UpdatedAt], [UserCreated], [UserModified], [UserName], [PasswordHashed],
        [Email], [Name], [PhoneNumber], [AvatarPath], [IsActive], [IsDeleted], [LastPasswordChangedAt],
        [FailedAccessAttempts], [RequiresPasswordChange]
    )
    VALUES (
        @UserId_Manager, @Now, @Now, @EmptyGuid, @EmptyGuid, 'manager',
        '$2a$11$fLtPxg2tVq.Y3JHfCkv8XuZMi3U/SOejdv3E/MkaO650cLbIBFFjO',
        'manager@systembase.com', N'Trần Văn Quản Lý', '0977777777', NULL, 1, 0, @Now, 0, 0
    );
END

-- User 3: user1
DECLARE @UserId_User1 UNIQUEIDENTIFIER = 'B0000000-0000-0000-0000-000000000003';
IF NOT EXISTS (SELECT 1 FROM [dbo].[User] WHERE [UserName] = 'user1')
BEGIN
    INSERT INTO [dbo].[User] (
        [Id], [CreatedAt], [UpdatedAt], [UserCreated], [UserModified], [UserName], [PasswordHashed],
        [Email], [Name], [PhoneNumber], [AvatarPath], [IsActive], [IsDeleted], [LastPasswordChangedAt],
        [FailedAccessAttempts], [RequiresPasswordChange]
    )
    VALUES (
        @UserId_User1, @Now, @Now, @EmptyGuid, @EmptyGuid, 'user1',
        '$2a$11$f4WcJ4xUJyzhgblqb8X/e.ILgTXcRnF.yZsETfOaR2HNGt3PtQx1.',
        'user1@systembase.com', N'Nguyễn Văn A', '0966666666', NULL, 1, 0, @Now, 0, 0
    );
END
GO

-- =========================================================================================
-- 2.4. Danh mục Menu & Quyền hạn chức năng (SystemFunction)
--   Bao gồm: Dashboard, Nhóm Quản trị hệ thống, Người dùng, Vai trò, Chức năng, Bảo mật, Logs...
-- =========================================================================================
DECLARE @Now DATETIME2 = DATEADD(HOUR, 7, GETUTCDATE());
DECLARE @EmptyGuid UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000000';

-- Định nghĩa các GUID cố định cho Functions
DECLARE @Func_Dashboard             UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000001';
DECLARE @Func_SystemGroup           UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000002';

-- Module Người dùng
DECLARE @Func_SystemUser            UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000010';
DECLARE @Func_SystemUser_Add        UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000011';
DECLARE @Func_SystemUser_Edit       UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000012';
DECLARE @Func_SystemUser_Delete     UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000013';
DECLARE @Func_SystemUser_BulkDelete UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000014';

-- Module Vai trò
DECLARE @Func_SystemRole            UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000020';
DECLARE @Func_SystemRole_Add        UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000021';
DECLARE @Func_SystemRole_Edit       UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000022';
DECLARE @Func_SystemRole_Delete     UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000023';
DECLARE @Func_SystemRole_BulkDelete UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000024';

-- Module Chức năng (SystemFunction)
DECLARE @Func_SystemFunction            UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000030';
DECLARE @Func_SystemFunction_Add        UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000031';
DECLARE @Func_SystemFunction_Edit       UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000032';
DECLARE @Func_SystemFunction_Delete     UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000033';
DECLARE @Func_SystemFunction_BulkDelete UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000034';

-- Module Cấu hình bảo mật (SystemSecuritySetting)
DECLARE @Func_SecuritySetting      UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000040';
DECLARE @Func_SecuritySetting_Save UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000041';

-- Module Lịch sử đăng nhập (LoginLog)
DECLARE @Func_LoginLog            UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000050';
DECLARE @Func_LoginLog_Delete     UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000051';
DECLARE @Func_LoginLog_BulkDelete UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000052';

-- Module Lịch sử thao tác (ActionLog)
DECLARE @Func_ActionLog            UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000060';
DECLARE @Func_ActionLog_Delete     UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000061';
DECLARE @Func_ActionLog_BulkDelete UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000062';

-- Module Lịch sử lỗi (ErrorLog)
DECLARE @Func_ErrorLog            UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000070';
DECLARE @Func_ErrorLog_Delete     UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000071';
DECLARE @Func_ErrorLog_BulkDelete UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000072';

-- Bảng tạm chứa danh sách functions mẫu
DECLARE @TempFunctions TABLE (
    [Id] UNIQUEIDENTIFIER,
    [Code] NVARCHAR(255),
    [Name] NVARCHAR(255),
    [Url] NVARCHAR(500),
    [Order] INT,
    [Type] NVARCHAR(50),
    [Icon] NVARCHAR(100),
    [IsShow] BIT,
    [ParentId] UNIQUEIDENTIFIER
);

INSERT INTO @TempFunctions VALUES
-- Top-level Items
(@Func_Dashboard,             'Dashboard',                 N'Bảng điều khiển',     '/cms/dashboard',         1, 'menu',   'SquaresFourIcon',             1, @EmptyGuid),
(@Func_SystemGroup,           'SystemManagement',          N'Quản trị hệ thống',   '',                       2, 'menu',   'GearIcon',                    1, @EmptyGuid),

-- 1. Phân hệ Người dùng (Con của SystemManagement)
(@Func_SystemUser,            'SystemUser',                N'Quản lý người dùng',  '/cms/system-users',      1, 'menu',   'UsersIcon',                   1, @Func_SystemGroup),
(@Func_SystemUser_Add,        'SystemUser_Add',            N'Thêm người dùng',     '',                       1, 'button', 'PlusIcon',                    1, @Func_SystemUser),
(@Func_SystemUser_Edit,       'SystemUser_Edit',           N'Sửa người dùng',      '',                       2, 'button', 'PencilSimpleIcon',            1, @Func_SystemUser),
(@Func_SystemUser_Delete,     'SystemUser_Delete',         N'Xóa người dùng',      '',                       3, 'button', 'TrashIcon',                   1, @Func_SystemUser),
(@Func_SystemUser_BulkDelete, 'SystemUser_BulkDelete',     N'Xóa nhiều người dùng', '',                      4, 'button', 'TrashIcon',                   1, @Func_SystemUser),

-- 2. Phân hệ Vai trò
(@Func_SystemRole,            'SystemRole',                N'Quản lý vai trò',     '/cms/system-roles',      2, 'menu',   'ShieldCheckIcon',             1, @Func_SystemGroup),
(@Func_SystemRole_Add,        'SystemRole_Add',            N'Thêm vai trò',        '',                       1, 'button', 'PlusIcon',                    1, @Func_SystemRole),
(@Func_SystemRole_Edit,       'SystemRole_Edit',           N'Sửa vai trò',         '',                       2, 'button', 'PencilSimpleIcon',            1, @Func_SystemRole),
(@Func_SystemRole_Delete,     'SystemRole_Delete',         N'Xóa vai trò',         '',                       3, 'button', 'TrashIcon',                   1, @Func_SystemRole),
(@Func_SystemRole_BulkDelete, 'SystemRole_BulkDelete',     N'Xóa nhiều vai trò',   '',                       4, 'button', 'TrashIcon',                   1, @Func_SystemRole),

-- 3. Phân hệ Chức năng
(@Func_SystemFunction,            'SystemFunction',            N'Cây chức năng',       '/cms/system-functions',  3, 'menu',   'TreeStructureIcon',           1, @Func_SystemGroup),
(@Func_SystemFunction_Add,        'SystemFunction_Add',        N'Thêm chức năng',      '',                       1, 'button', 'PlusIcon',                    1, @Func_SystemFunction),
(@Func_SystemFunction_Edit,       'SystemFunction_Edit',       N'Sửa chức năng',       '',                       2, 'button', 'PencilSimpleIcon',            1, @Func_SystemFunction),
(@Func_SystemFunction_Delete,     'SystemFunction_Delete',     N'Xóa chức năng',       '',                       3, 'button', 'TrashIcon',                   1, @Func_SystemFunction),
(@Func_SystemFunction_BulkDelete, 'SystemFunction_BulkDelete', N'Xóa nhiều chức năng', '',                       4, 'button', 'TrashIcon',                   1, @Func_SystemFunction),

-- 4. Phân hệ Cấu hình bảo mật
(@Func_SecuritySetting,      'SystemSecuritySetting',      N'Cấu hình bảo mật',    '/cms/security-settings', 4, 'menu',   'LockKeyIcon',                 1, @Func_SystemGroup),
(@Func_SecuritySetting_Save, 'SystemSecuritySetting_Save', N'Lưu cấu hình',        '',                       1, 'button', 'FloppyDiskIcon',              1, @Func_SecuritySetting),

-- 5. Phân hệ Nhật ký đăng nhập
(@Func_LoginLog,            'LoginLog',                  N'Lịch sử đăng nhập',   '/cms/login-logs',        5, 'menu',   'SignInIcon',                  1, @Func_SystemGroup),
(@Func_LoginLog_Delete,     'LoginLog_Delete',           N'Xóa nhật ký đăng nhập', '',                     1, 'button', 'TrashIcon',                   1, @Func_LoginLog),
(@Func_LoginLog_BulkDelete, 'LoginLog_BulkDelete',       N'Xóa nhiều nhật ký',   '',                       2, 'button', 'TrashIcon',                   1, @Func_LoginLog),

-- 6. Phân hệ Nhật ký thao tác
(@Func_ActionLog,            'ActionLog',                 N'Lịch sử thao tác',    '/cms/action-logs',       6, 'menu',   'ClockCounterClockwiseIcon',   1, @Func_SystemGroup),
(@Func_ActionLog_Delete,     'ActionLog_Delete',          N'Xóa nhật ký thao tác', '',                     1, 'button', 'TrashIcon',                   1, @Func_ActionLog),
(@Func_ActionLog_BulkDelete, 'ActionLog_BulkDelete',      N'Xóa nhiều thao tác',  '',                       2, 'button', 'TrashIcon',                   1, @Func_ActionLog),

-- 7. Phân hệ Nhật ký lỗi
(@Func_ErrorLog,            'ErrorLog',                  N'Lịch sử lỗi hệ thống', '/cms/error-logs',       7, 'menu',   'BugIcon',                     1, @Func_SystemGroup),
(@Func_ErrorLog_Delete,     'ErrorLog_Delete',           N'Xóa nhật ký lỗi',     '',                       1, 'button', 'TrashIcon',                   1, @Func_ErrorLog),
(@Func_ErrorLog_BulkDelete, 'ErrorLog_BulkDelete',       N'Xóa nhiều nhật ký lỗi', '',                     2, 'button', 'TrashIcon',                   1, @Func_ErrorLog);

-- Cập nhật hoặc thêm mới các Functions vào bảng SystemFunction
MERGE [dbo].[SystemFunction] AS Target
USING @TempFunctions AS Source
ON (Target.[Code] = Source.[Code])
WHEN MATCHED THEN
    UPDATE SET 
        Target.[Name] = Source.[Name],
        Target.[Url] = Source.[Url],
        Target.[Order] = Source.[Order],
        Target.[Type] = Source.[Type],
        Target.[Icon] = Source.[Icon],
        Target.[IsShow] = Source.[IsShow],
        Target.[ParentId] = Source.[ParentId],
        Target.[IsDeleted] = 0
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Id], [CreatedAt], [UpdatedAt], [UserCreated], [UserModified], [Code], [Name], [Url], [Order], [Type], [Icon], [IsShow], [ParentId], [IsDeleted])
    VALUES (Source.[Id], @Now, @Now, @EmptyGuid, @EmptyGuid, Source.[Code], Source.[Name], Source.[Url], Source.[Order], Source.[Type], Source.[Icon], Source.[IsShow], Source.[ParentId], 0);

PRINT 'Đã cập nhật danh mục chức năng (SystemFunction).';
GO

-- =========================================================================================
-- 2.5. Gán vai trò cho người dùng (UserRoles)
-- =========================================================================================
DECLARE @RoleId_Admin   UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000001';
DECLARE @RoleId_Manager UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000002';
DECLARE @RoleId_User    UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000003';

DECLARE @UserId_Admin   UNIQUEIDENTIFIER = 'B0000000-0000-0000-0000-000000000001';
DECLARE @UserId_Manager UNIQUEIDENTIFIER = 'B0000000-0000-0000-0000-000000000002';
DECLARE @UserId_User1   UNIQUEIDENTIFIER = 'B0000000-0000-0000-0000-000000000003';

-- Gán role ADMIN cho admin
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRoles] WHERE [UserId] = @UserId_Admin AND [RoleId] = @RoleId_Admin)
    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (@UserId_Admin, @RoleId_Admin);

-- Gán role MANAGER cho manager
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRoles] WHERE [UserId] = @UserId_Manager AND [RoleId] = @RoleId_Manager)
    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (@UserId_Manager, @RoleId_Manager);

-- Gán role USER cho user1
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRoles] WHERE [UserId] = @UserId_User1 AND [RoleId] = @RoleId_User)
    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (@UserId_User1, @RoleId_User);

PRINT 'Đã gán vai trò người dùng (UserRoles).';
GO

-- =========================================================================================
-- 2.6. Phân quyền chức năng cho các Vai trò (SystemRoleFunctions)
-- =========================================================================================
DECLARE @RoleId_Admin   UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000001';
DECLARE @RoleId_Manager UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000002';
DECLARE @RoleId_User    UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000003';

-- 1. Role ADMIN: Gán TOÀN BỘ tất cả các chức năng và quyền thao tác
INSERT INTO [dbo].[SystemRoleFunctions] ([RoleId], [FunctionId])
SELECT @RoleId_Admin, f.[Id]
FROM [dbo].[SystemFunction] f
WHERE f.[IsDeleted] = 0
  AND NOT EXISTS (
      SELECT 1 FROM [dbo].[SystemRoleFunctions] srf 
      WHERE srf.[RoleId] = @RoleId_Admin AND srf.[FunctionId] = f.[Id]
  );

-- 2. Role MANAGER: Gán Dashboard, Quản lý người dùng, Xem Lịch sử thao tác & đăng nhập
INSERT INTO [dbo].[SystemRoleFunctions] ([RoleId], [FunctionId])
SELECT @RoleId_Manager, f.[Id]
FROM [dbo].[SystemFunction] f
WHERE f.[IsDeleted] = 0
  AND f.[Code] IN (
      'Dashboard', 'SystemManagement',
      'SystemUser', 'SystemUser_Add', 'SystemUser_Edit',
      'LoginLog',
      'ActionLog'
  )
  AND NOT EXISTS (
      SELECT 1 FROM [dbo].[SystemRoleFunctions] srf 
      WHERE srf.[RoleId] = @RoleId_Manager AND srf.[FunctionId] = f.[Id]
  );

-- 3. Role USER: Gán chỉ xem Dashboard
INSERT INTO [dbo].[SystemRoleFunctions] ([RoleId], [FunctionId])
SELECT @RoleId_User, f.[Id]
FROM [dbo].[SystemFunction] f
WHERE f.[IsDeleted] = 0
  AND f.[Code] IN ('Dashboard')
  AND NOT EXISTS (
      SELECT 1 FROM [dbo].[SystemRoleFunctions] srf 
      WHERE srf.[RoleId] = @RoleId_User AND srf.[FunctionId] = f.[Id]
  );

PRINT 'Đã phân quyền đầy đủ cho các vai trò (SystemRoleFunctions).';
GO

-- =========================================================================================
-- HOÀN TẤT KHỞI TẠO CƠ SỞ DỮ LIỆU SYSTEMBASE
-- =========================================================================================
PRINT N'======================================================================';
PRINT N'Khởi tạo cơ sở dữ liệu SystemBase thành công!';
PRINT N'======================================================================';
GO
