-- ============================================================
-- 004_SeedPublishers.sql
-- Seed Publisher dropdown category.
-- ============================================================

INSERT INTO Dropdowns (Category, Value, Label, SortOrder, IsActive, CreatedAt, UpdatedAt)
SELECT v.Category, v.Value, v.Label, v.SortOrder, 1, GETUTCDATE(), GETUTCDATE()
FROM (VALUES
    ('Publisher', 'GoodluckPublications', 'Goodluck Publications', 1),
    ('Publisher', 'VidhyarthiPrakashan',  'Vidhyarthi Prakashan',  2)
) AS v(Category, Value, Label, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM Dropdowns WHERE Category = v.Category AND Value = v.Value AND IsActive = 1
);
