-- ============================================================
-- 008_AlterUserModulePermissions.sql
-- Add missing columns to existing UserModulePermissions table.
-- ============================================================

-- Step 1: Rename ModuleName -> Module (must be first, before adding SubModule)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserModulePermissions' AND COLUMN_NAME = 'ModuleName')
    EXEC sp_rename 'UserModulePermissions.ModuleName', 'Module', 'COLUMN';

-- Step 2: Add SubModule
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserModulePermissions' AND COLUMN_NAME = 'SubModule')
    ALTER TABLE UserModulePermissions ADD SubModule NVARCHAR(100) NOT NULL DEFAULT '';

-- Step 3: Add permission flag columns
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserModulePermissions' AND COLUMN_NAME = 'CanView')
    ALTER TABLE UserModulePermissions ADD CanView BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserModulePermissions' AND COLUMN_NAME = 'CanSave')
    ALTER TABLE UserModulePermissions ADD CanSave BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserModulePermissions' AND COLUMN_NAME = 'CanEdit')
    ALTER TABLE UserModulePermissions ADD CanEdit BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserModulePermissions' AND COLUMN_NAME = 'CanDelete')
    ALTER TABLE UserModulePermissions ADD CanDelete BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserModulePermissions' AND COLUMN_NAME = 'CanExport')
    ALTER TABLE UserModulePermissions ADD CanExport BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserModulePermissions' AND COLUMN_NAME = 'CanPrint')
    ALTER TABLE UserModulePermissions ADD CanPrint BIT NOT NULL DEFAULT 0;
