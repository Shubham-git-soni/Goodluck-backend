-- Migration: Add ActionUrl column to Notifications table
-- Required by NotificationService.SendToUserAsync for deep-link navigation

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Notifications') AND name = N'ActionUrl'
)
BEGIN
    ALTER TABLE Notifications ADD ActionUrl NVARCHAR(500) NULL;
END
