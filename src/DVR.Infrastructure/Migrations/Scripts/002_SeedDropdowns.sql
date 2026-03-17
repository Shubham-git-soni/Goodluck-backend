-- ============================================================
-- 002_SeedDropdowns.sql
-- Seed all dropdown categories used in the CRM.
-- Safe to run multiple times: uses IF NOT EXISTS pattern.
-- ============================================================

-- Helper: only insert if Category+Value combo not already present
-- Run this after 001_InitialSchema.sql

-- Boards
INSERT INTO Dropdowns (Category, Value, Label, SortOrder)
SELECT * FROM (VALUES
    ('Board', 'CBSE',       'CBSE',        1),
    ('Board', 'ICSE',       'ICSE',        2),
    ('Board', 'StateBoard', 'State Board', 3),
    ('Board', 'IB',         'IB',          4),
    ('Board', 'IGCSE',      'IGCSE',       5),
    ('Board', 'NIOS',       'NIOS',        6)
) AS v(Category, Value, Label, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM Dropdowns WHERE Category = v.Category AND Value = v.Value AND IsActive = 1
);

-- Contact Roles
INSERT INTO Dropdowns (Category, Value, Label, SortOrder)
SELECT * FROM (VALUES
    ('ContactRole', 'Principal',        'Principal',         1),
    ('ContactRole', 'VicePrincipal',    'Vice Principal',    2),
    ('ContactRole', 'Coordinator',      'Coordinator',       3),
    ('ContactRole', 'HeadMaster',       'Head Master',       4),
    ('ContactRole', 'Librarian',        'Librarian',         5),
    ('ContactRole', 'PurchaseHead',     'Purchase Head',     6),
    ('ContactRole', 'Teacher',          'Teacher',           7),
    ('ContactRole', 'Clerk',            'Clerk',             8),
    ('ContactRole', 'Owner',            'Owner',             9),
    ('ContactRole', 'Other',            'Other',            10)
) AS v(Category, Value, Label, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM Dropdowns WHERE Category = v.Category AND Value = v.Value AND IsActive = 1
);

-- Visit Purposes
INSERT INTO Dropdowns (Category, Value, Label, SortOrder)
SELECT * FROM (VALUES
    ('VisitPurpose', 'Introduction',          'Introduction',             1),
    ('VisitPurpose', 'FollowUp',              'Follow-Up',                2),
    ('VisitPurpose', 'NeedMapping',           'Need Mapping',             3),
    ('VisitPurpose', 'SpecimenDistribution',  'Specimen Distribution',    4),
    ('VisitPurpose', 'OrderCollection',       'Order Collection',         5),
    ('VisitPurpose', 'RelationshipBuilding',  'Relationship Building',    6),
    ('VisitPurpose', 'Complaint',             'Complaint',                7),
    ('VisitPurpose', 'Payment',               'Payment Collection',       8),
    ('VisitPurpose', 'EventVisit',            'Event / Exhibition Visit', 9),
    ('VisitPurpose', 'Demo',                  'Demo / Presentation',     10),
    ('VisitPurpose', 'Other',                 'Other',                   11),
    ('VisitPurpose', 'AdoptionFollowUp',      'Adoption Follow-Up',      12),
    ('VisitPurpose', 'SalesReview',           'Sales Review',            13)
) AS v(Category, Value, Label, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM Dropdowns WHERE Category = v.Category AND Value = v.Value AND IsActive = 1
);

-- Need Mapping Types
INSERT INTO Dropdowns (Category, Value, Label, SortOrder)
SELECT * FROM (VALUES
    ('NeedMappingType', 'Class1to5',   'Class 1–5',   1),
    ('NeedMappingType', 'Class6to8',   'Class 6–8',   2),
    ('NeedMappingType', 'Class9to10',  'Class 9–10',  3),
    ('NeedMappingType', 'Class11to12', 'Class 11–12', 4)
) AS v(Category, Value, Label, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM Dropdowns WHERE Category = v.Category AND Value = v.Value AND IsActive = 1
);

-- Subjects
INSERT INTO Dropdowns (Category, Value, Label, SortOrder)
SELECT * FROM (VALUES
    ('Subject', 'Mathematics',      'Mathematics',         1),
    ('Subject', 'Science',          'Science',             2),
    ('Subject', 'Physics',          'Physics',             3),
    ('Subject', 'Chemistry',        'Chemistry',           4),
    ('Subject', 'Biology',          'Biology',             5),
    ('Subject', 'English',          'English',             6),
    ('Subject', 'Hindi',            'Hindi',               7),
    ('Subject', 'SocialStudies',    'Social Studies',      8),
    ('Subject', 'History',          'History',             9),
    ('Subject', 'Geography',        'Geography',          10),
    ('Subject', 'EVS',              'EVS',                11),
    ('Subject', 'ComputerScience',  'Computer Science',   12),
    ('Subject', 'Economics',        'Economics',          13),
    ('Subject', 'BusinessStudies',  'Business Studies',   14),
    ('Subject', 'Accountancy',      'Accountancy',        15)
) AS v(Category, Value, Label, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM Dropdowns WHERE Category = v.Category AND Value = v.Value AND IsActive = 1
);

-- Travel Modes
INSERT INTO Dropdowns (Category, Value, Label, SortOrder)
SELECT * FROM (VALUES
    ('TravelMode', 'Bus',         'Bus',          1),
    ('TravelMode', 'Train',       'Train',        2),
    ('TravelMode', 'Auto',        'Auto',         3),
    ('TravelMode', 'Cab',         'Cab / Taxi',   4),
    ('TravelMode', 'OwnVehicle',  'Own Vehicle',  5),
    ('TravelMode', 'Walking',     'Walking',      6)
) AS v(Category, Value, Label, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM Dropdowns WHERE Category = v.Category AND Value = v.Value AND IsActive = 1
);

-- Feedback Categories
INSERT INTO Dropdowns (Category, Value, Label, SortOrder)
SELECT * FROM (VALUES
    ('FeedbackCategory', 'ProductQuality',  'Product Quality',  1),
    ('FeedbackCategory', 'Delivery',        'Delivery',         2),
    ('FeedbackCategory', 'Pricing',         'Pricing',          3),
    ('FeedbackCategory', 'Service',         'Service',          4),
    ('FeedbackCategory', 'Availability',    'Availability',     5),
    ('FeedbackCategory', 'Support',         'Support',          6),
    ('FeedbackCategory', 'Other',           'Other',            7)
) AS v(Category, Value, Label, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM Dropdowns WHERE Category = v.Category AND Value = v.Value AND IsActive = 1
);

-- Discount Categories
INSERT INTO Dropdowns (Category, Value, Label, SortOrder)
SELECT * FROM (VALUES
    ('DiscountCategory', 'Standard',    'Standard',    1),
    ('DiscountCategory', 'Special',     'Special',     2),
    ('DiscountCategory', 'Festival',    'Festival',    3),
    ('DiscountCategory', 'Bulk',        'Bulk Order',  4),
    ('DiscountCategory', 'Loyalty',     'Loyalty',     5),
    ('DiscountCategory', 'Promotional', 'Promotional', 6)
) AS v(Category, Value, Label, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM Dropdowns WHERE Category = v.Category AND Value = v.Value AND IsActive = 1
);

-- Payment Statuses
INSERT INTO Dropdowns (Category, Value, Label, SortOrder)
SELECT * FROM (VALUES
    ('PaymentStatus', 'Pending',   'Pending',   1),
    ('PaymentStatus', 'Paid',      'Paid',      2),
    ('PaymentStatus', 'Overdue',   'Overdue',   3),
    ('PaymentStatus', 'PartPaid',  'Part Paid', 4),
    ('PaymentStatus', 'Refunded',  'Refunded',  5)
) AS v(Category, Value, Label, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM Dropdowns WHERE Category = v.Category AND Value = v.Value AND IsActive = 1
);

-- Visit Statuses
INSERT INTO Dropdowns (Category, Value, Label, SortOrder)
SELECT * FROM (VALUES
    ('VisitStatus', 'Completed',   'Completed',   1),
    ('VisitStatus', 'Scheduled',   'Scheduled',   2),
    ('VisitStatus', 'Cancelled',   'Cancelled',   3),
    ('VisitStatus', 'NoShow',      'No Show',     4)
) AS v(Category, Value, Label, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM Dropdowns WHERE Category = v.Category AND Value = v.Value AND IsActive = 1
);

-- TA/DA Statuses
INSERT INTO Dropdowns (Category, Value, Label, SortOrder)
SELECT * FROM (VALUES
    ('TadaStatus', 'Draft',      'Draft',      1),
    ('TadaStatus', 'Submitted',  'Submitted',  2),
    ('TadaStatus', 'Approved',   'Approved',   3),
    ('TadaStatus', 'Rejected',   'Rejected',   4),
    ('TadaStatus', 'OnHold',     'On Hold',    5)
) AS v(Category, Value, Label, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM Dropdowns WHERE Category = v.Category AND Value = v.Value AND IsActive = 1
);

-- Approval Statuses
INSERT INTO Dropdowns (Category, Value, Label, SortOrder)
SELECT * FROM (VALUES
    ('ApprovalStatus', 'Pending',   'Pending',   1),
    ('ApprovalStatus', 'Approved',  'Approved',  2),
    ('ApprovalStatus', 'Rejected',  'Rejected',  3),
    ('ApprovalStatus', 'OnHold',    'On Hold',   4)
) AS v(Category, Value, Label, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM Dropdowns WHERE Category = v.Category AND Value = v.Value AND IsActive = 1
);

-- Specimen Conditions
INSERT INTO Dropdowns (Category, Value, Label, SortOrder)
SELECT * FROM (VALUES
    ('SpecimenCondition', 'Good',        'Good',        1),
    ('SpecimenCondition', 'Damaged',     'Damaged',     2),
    ('SpecimenCondition', 'Lost',        'Lost',        3)
) AS v(Category, Value, Label, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM Dropdowns WHERE Category = v.Category AND Value = v.Value AND IsActive = 1
);

-- Manager Types
INSERT INTO Dropdowns (Category, Value, Label, SortOrder)
SELECT * FROM (VALUES
    ('ManagerType', 'AreaManager',      'Area Manager',     1),
    ('ManagerType', 'RegionalManager',  'Regional Manager', 2),
    ('ManagerType', 'ZonalManager',     'Zonal Manager',    3)
) AS v(Category, Value, Label, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM Dropdowns WHERE Category = v.Category AND Value = v.Value AND IsActive = 1
);

-- Compliance Statuses
INSERT INTO Dropdowns (Category, Value, Label, SortOrder)
SELECT * FROM (VALUES
    ('ComplianceStatus', 'Compliant',     'Compliant',     1),
    ('ComplianceStatus', 'NonCompliant',  'Non-Compliant', 2),
    ('ComplianceStatus', 'Partial',       'Partial',       3)
) AS v(Category, Value, Label, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM Dropdowns WHERE Category = v.Category AND Value = v.Value AND IsActive = 1
);
