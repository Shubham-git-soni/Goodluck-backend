-- ============================================================
-- 007_CreateUserPermissions.sql
-- Module-level permissions per user.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserPermissions')
CREATE TABLE UserPermissions (
    PermissionId  INT IDENTITY(1,1) PRIMARY KEY,
    UserId        INT NOT NULL REFERENCES Users(UserId),
    Module        NVARCHAR(100) NOT NULL,
    SubModule     NVARCHAR(100) NOT NULL,
    PermLevel     NVARCHAR(20)  NOT NULL DEFAULT 'None',  -- None / View / User / Admin
    CanView       BIT NOT NULL DEFAULT 0,
    CanSave       BIT NOT NULL DEFAULT 0,
    CanEdit       BIT NOT NULL DEFAULT 0,
    CanDelete     BIT NOT NULL DEFAULT 0,
    CanExport     BIT NOT NULL DEFAULT 0,
    CanPrint      BIT NOT NULL DEFAULT 0,
    UpdatedAt     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_UserPermissions_User_Module UNIQUE (UserId, Module, SubModule)
);
