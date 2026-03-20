-- =============================================
-- Migration: Add Sales Targets and Specimen Budgets to Salesmen
-- Description: Adds SalesTarget, SalesAchieved, SpecimenBudget, SpecimenUsed fields
-- Author: System
-- Date: 2026-03-17
-- =============================================

-- Add new columns to Salesmen table
ALTER TABLE Salesmen
ADD SalesTarget DECIMAL(18, 2) NULL DEFAULT 0,
    SalesAchieved DECIMAL(18, 2) NULL DEFAULT 0,
    SpecimenBudget DECIMAL(18, 2) NULL DEFAULT 0,
    SpecimenUsed DECIMAL(18, 2) NULL DEFAULT 0;

-- Add helpful comment
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Monthly/Annual sales target for the salesman',
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'Salesmen',
    @level2type = N'COLUMN', @level2name = 'SalesTarget';

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Actual sales achieved',
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'Salesmen',
    @level2type = N'COLUMN', @level2name = 'SalesAchieved';

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Total specimen budget allocated',
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'Salesmen',
    @level2type = N'COLUMN', @level2name = 'SpecimenBudget';

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Specimen budget utilized',
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'Salesmen',
    @level2type = N'COLUMN', @level2name = 'SpecimenUsed';

GO

-- Update existing salesmen with default values (optional)
-- UPDATE Salesmen
-- SET SalesTarget = 1200000,
--     SalesAchieved = 0,
--     SpecimenBudget = 200000,
--     SpecimenUsed = 0
-- WHERE SalesTarget IS NULL;

PRINT 'Migration 004: Sales targets and specimen budgets added successfully';
