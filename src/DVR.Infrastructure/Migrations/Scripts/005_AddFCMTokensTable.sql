-- =============================================
-- Migration: Add FCM Tokens Table for Push Notifications
-- Description: Stores Firebase Cloud Messaging tokens for push notifications
-- Author: System
-- Date: 2026-03-17
-- =============================================

-- Create FCMTokens table
CREATE TABLE FCMTokens (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Token NVARCHAR(500) NOT NULL,
    DeviceType NVARCHAR(50) NULL DEFAULT 'Web',
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_FCMTokens_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

-- Create index for faster lookups
CREATE INDEX IX_FCMTokens_UserId ON FCMTokens(UserId);
CREATE UNIQUE INDEX IX_FCMTokens_Token ON FCMTokens(Token);

-- Add helpful comments
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Firebase Cloud Messaging token for push notifications',
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'FCMTokens',
    @level2type = N'COLUMN', @level2name = 'Token';

GO

PRINT 'Migration 005: FCM Tokens table created successfully';
