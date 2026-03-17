-- ============================================================
-- 011_FixUniqueConstraint.sql
-- Replace old UNIQUE(UserId, Module/ModuleName) with
-- UNIQUE(UserId, Module, SubModule) to support sub-modules.
-- ============================================================

-- Drop ALL unique constraints on UserModulePermissions (except PK)
DECLARE @sql NVARCHAR(MAX) = '';
SELECT @sql = @sql + 'ALTER TABLE UserModulePermissions DROP CONSTRAINT [' + kc.name + '];' + CHAR(13)
FROM sys.key_constraints kc
JOIN sys.tables t ON kc.parent_object_id = t.object_id
WHERE t.name = 'UserModulePermissions' AND kc.type = 'UQ';

IF LEN(@sql) > 0
    EXEC(@sql);

-- Add correct unique constraint on (UserId, Module, SubModule)
IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints kc
    JOIN sys.tables t ON kc.parent_object_id = t.object_id
    WHERE t.name = 'UserModulePermissions'
      AND kc.type = 'UQ'
      AND kc.name = 'UQ_UserModulePerm_UserModuleSub'
)
    ALTER TABLE UserModulePermissions
        ADD CONSTRAINT UQ_UserModulePerm_UserModuleSub UNIQUE (UserId, Module, SubModule);
