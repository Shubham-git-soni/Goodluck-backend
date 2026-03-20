-- ============================================================
-- 005_AlterBooksAddPriceColumns.sql
-- Add MRP, SellingPrice, SpecimenPrice columns to Books.
-- Migrate existing Price -> MRP, keep Price for backward compat.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Books' AND COLUMN_NAME = 'MRP')
    ALTER TABLE Books ADD MRP DECIMAL(10,2) NULL;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Books' AND COLUMN_NAME = 'SellingPrice')
    ALTER TABLE Books ADD SellingPrice DECIMAL(10,2) NULL;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Books' AND COLUMN_NAME = 'SpecimenPrice')
    ALTER TABLE Books ADD SpecimenPrice DECIMAL(10,2) NULL;

-- Migrate old Price data into MRP (use dynamic SQL so column ref is resolved at runtime)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Books' AND COLUMN_NAME = 'Price')
    EXEC('UPDATE Books SET MRP = Price WHERE MRP IS NULL AND Price IS NOT NULL');
