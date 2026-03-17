-- ============================================================
-- 006_AlterBooksRenameSeriestoBoard.sql
-- Rename Series column to Board in Books table.
-- ============================================================

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Books' AND COLUMN_NAME = 'Series')
    AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Books' AND COLUMN_NAME = 'Board')
BEGIN
    EXEC sp_rename 'Books.Series', 'Board', 'COLUMN';
END
