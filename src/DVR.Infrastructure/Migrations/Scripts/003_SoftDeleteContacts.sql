-- Migration 003: Soft delete for SchoolContacts
-- Adds IsDeleted flag so contacts can be hidden from UI but restored from DB

ALTER TABLE SchoolContacts
    ADD IsDeleted BIT NOT NULL DEFAULT 0;

ALTER TABLE SchoolContacts
    ADD DeletedAt DATETIME2 NULL;
