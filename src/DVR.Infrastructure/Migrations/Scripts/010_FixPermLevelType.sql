-- ============================================================
-- 010_FixPermLevelType.sql
-- Ensure PermLevel column is NVARCHAR(20), not INT.
-- Uses EXEC() for UPDATE to avoid parse-time column validation.
-- ============================================================

-- Only run if PermLevel is still INT
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'UserModulePermissions'
      AND COLUMN_NAME = 'PermLevel'
      AND DATA_TYPE = 'int'
)
BEGIN
    -- Drop default constraint on PermLevel (INT column)
    DECLARE @cn NVARCHAR(200);
    SELECT @cn = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    JOIN sys.tables t ON c.object_id = t.object_id
    WHERE t.name = 'UserModulePermissions' AND c.name = 'PermLevel';

    IF @cn IS NOT NULL
        EXEC('ALTER TABLE UserModulePermissions DROP CONSTRAINT [' + @cn + ']');

    -- Add new NVARCHAR column
    ALTER TABLE UserModulePermissions ADD PermLevelNew NVARCHAR(20) NOT NULL DEFAULT 'None';

    -- Migrate values using EXEC to avoid parse-time column check
    EXEC('UPDATE UserModulePermissions SET PermLevelNew = CASE PermLevel
        WHEN 0 THEN ''None''
        WHEN 1 THEN ''View''
        WHEN 2 THEN ''User''
        WHEN 3 THEN ''Admin''
        ELSE ''None'' END');

    -- Drop old INT column
    ALTER TABLE UserModulePermissions DROP COLUMN PermLevel;

    -- Rename new column to PermLevel
    EXEC sp_rename 'UserModulePermissions.PermLevelNew', 'PermLevel', 'COLUMN';
END
