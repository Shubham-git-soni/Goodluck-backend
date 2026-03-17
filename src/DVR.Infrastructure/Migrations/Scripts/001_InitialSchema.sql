-- =============================================
-- GoodLuck DVR CRM - Initial Database Schema
-- Migration: 001_InitialSchema.sql
-- =============================================

-- Users Table
CREATE TABLE Users (
    UserId          INT IDENTITY(1,1) PRIMARY KEY,
    Username        NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255) NOT NULL,
    FullName        NVARCHAR(200) NOT NULL,
    Email           NVARCHAR(200) NOT NULL,
    Phone           NVARCHAR(20) NULL,
    Role            INT NOT NULL DEFAULT 3,  -- 1=Admin, 2=Manager, 3=Salesman
    ManagerId       INT NULL REFERENCES Users(UserId),
    IsActive        BIT NOT NULL DEFAULT 1,
    ProfilePhotoUrl NVARCHAR(500) NULL,
    DeviceToken     NVARCHAR(500) NULL,
    RefreshToken    NVARCHAR(500) NULL,
    RefreshTokenExpiry DATETIME2 NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Salesmen Table
CREATE TABLE Salesmen (
    SalesmanId      INT IDENTITY(1,1) PRIMARY KEY,
    UserId          INT NOT NULL REFERENCES Users(UserId),
    EmployeeCode    NVARCHAR(50) NOT NULL UNIQUE,
    Territory       NVARCHAR(200) NULL,
    Zone            NVARCHAR(100) NULL,
    State           NVARCHAR(100) NULL,
    City            NVARCHAR(100) NULL,
    ManagerId       INT NULL REFERENCES Users(UserId),
    Designation     NVARCHAR(100) NULL,
    JoiningDate     DATE NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Schools Table
CREATE TABLE Schools (
    SchoolId            INT IDENTITY(1,1) PRIMARY KEY,
    SchoolName          NVARCHAR(300) NOT NULL,
    PrincipalName       NVARCHAR(200) NULL,
    Phone               NVARCHAR(20) NULL,
    Email               NVARCHAR(200) NULL,
    Address             NVARCHAR(500) NULL,
    City                NVARCHAR(100) NULL,
    State               NVARCHAR(100) NULL,
    Pincode             NVARCHAR(10) NULL,
    SchoolType          NVARCHAR(50) NULL,   -- Private / Govt / Semi-Govt
    Board               NVARCHAR(50) NULL,   -- CBSE / ICSE / State
    TotalStudents       INT NULL,
    Category            NVARCHAR(50) NULL,   -- A / B / C
    AssignedSalesmanId  INT NULL REFERENCES Salesmen(SalesmanId),
    IsActive            BIT NOT NULL DEFAULT 1,
    Latitude            NVARCHAR(30) NULL,
    Longitude           NVARCHAR(30) NULL,
    CreatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- School Contacts Table
CREATE TABLE SchoolContacts (
    ContactId       INT IDENTITY(1,1) PRIMARY KEY,
    SchoolId        INT NOT NULL REFERENCES Schools(SchoolId) ON DELETE CASCADE,
    Name            NVARCHAR(200) NOT NULL,
    Designation     NVARCHAR(100) NULL,
    Phone           NVARCHAR(20) NULL,
    Email           NVARCHAR(200) NULL,
    Subject         NVARCHAR(100) NULL,
    IsPrimary       BIT NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- BookSellers Table
CREATE TABLE BookSellers (
    BookSellerId        INT IDENTITY(1,1) PRIMARY KEY,
    ShopName            NVARCHAR(300) NOT NULL,
    OwnerName           NVARCHAR(200) NOT NULL,
    Phone               NVARCHAR(20) NULL,
    Email               NVARCHAR(200) NULL,
    Address             NVARCHAR(500) NULL,
    City                NVARCHAR(100) NULL,
    State               NVARCHAR(100) NULL,
    Pincode             NVARCHAR(10) NULL,
    AssignedSalesmanId  INT NULL REFERENCES Salesmen(SalesmanId),
    IsActive            BIT NOT NULL DEFAULT 1,
    Latitude            NVARCHAR(30) NULL,
    Longitude           NVARCHAR(30) NULL,
    CreatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Books Table
CREATE TABLE Books (
    BookId      INT IDENTITY(1,1) PRIMARY KEY,
    Title       NVARCHAR(300) NOT NULL,
    Author      NVARCHAR(200) NULL,
    Subject     NVARCHAR(100) NULL,
    Class       NVARCHAR(20) NULL,
    Series      NVARCHAR(100) NULL,
    ISBN        NVARCHAR(50) NULL,
    Publisher   NVARCHAR(200) NULL,
    Price       DECIMAL(10,2) NULL,
    IsActive    BIT NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- School Prescribed Books
CREATE TABLE SchoolPrescribedBooks (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    SchoolId    INT NOT NULL REFERENCES Schools(SchoolId) ON DELETE CASCADE,
    BookId      INT NOT NULL REFERENCES Books(BookId),
    ClassYear   NVARCHAR(20) NULL,
    Quantity    INT NULL,
    CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Visits Table
CREATE TABLE Visits (
    VisitId             INT IDENTITY(1,1) PRIMARY KEY,
    SalesmanId          INT NOT NULL REFERENCES Salesmen(SalesmanId),
    VisitType           INT NOT NULL, -- 1=School, 2=Bookseller
    SchoolId            INT NULL REFERENCES Schools(SchoolId),
    BookSellerId        INT NULL REFERENCES BookSellers(BookSellerId),
    VisitDate           DATETIME2 NOT NULL,
    CheckInTime         TIME NULL,
    CheckOutTime        TIME NULL,
    Purpose             NVARCHAR(500) NULL,
    Remarks             NVARCHAR(1000) NULL,
    Outcome             NVARCHAR(500) NULL,
    FollowUpDate        NVARCHAR(50) NULL,
    CheckInLatitude     NVARCHAR(30) NULL,
    CheckInLongitude    NVARCHAR(30) NULL,
    CheckOutLatitude    NVARCHAR(30) NULL,
    CheckOutLongitude   NVARCHAR(30) NULL,
    PhotoUrl            NVARCHAR(500) NULL,
    IsCompleted         BIT NOT NULL DEFAULT 0,
    CreatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Tour Plans Table
CREATE TABLE TourPlans (
    TourPlanId      INT IDENTITY(1,1) PRIMARY KEY,
    SalesmanId      INT NOT NULL REFERENCES Salesmen(SalesmanId),
    PlanDate        DATE NOT NULL,
    PlannedAreas    NVARCHAR(1000) NULL,
    PlannedVisits   NVARCHAR(MAX) NULL,  -- JSON array
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Draft', -- Draft / Submitted / Approved / Rejected
    ApprovedById    INT NULL REFERENCES Users(UserId),
    ApprovedAt      DATETIME2 NULL,
    RejectionReason NVARCHAR(500) NULL,
    Remarks         NVARCHAR(500) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Attendance Table
CREATE TABLE Attendance (
    AttendanceId        INT IDENTITY(1,1) PRIMARY KEY,
    SalesmanId          INT NOT NULL REFERENCES Salesmen(SalesmanId),
    AttendanceDate      DATE NOT NULL,
    CheckInTime         DATETIME2 NULL,
    CheckOutTime        DATETIME2 NULL,
    CheckInLatitude     NVARCHAR(30) NULL,
    CheckInLongitude    NVARCHAR(30) NULL,
    CheckOutLatitude    NVARCHAR(30) NULL,
    CheckOutLongitude   NVARCHAR(30) NULL,
    Status              NVARCHAR(20) NOT NULL DEFAULT 'Present', -- Present / Absent / Leave / Holiday
    Notes               NVARCHAR(500) NULL,
    CreatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UNIQUE (SalesmanId, AttendanceDate)
);

-- Expense Reports Table
CREATE TABLE ExpenseReports (
    ExpenseReportId INT IDENTITY(1,1) PRIMARY KEY,
    SalesmanId      INT NOT NULL REFERENCES Salesmen(SalesmanId),
    ReportMonth     NVARCHAR(20) NOT NULL,
    ReportYear      INT NOT NULL,
    TotalAmount     DECIMAL(12,2) NOT NULL DEFAULT 0,
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Draft', -- Draft / Submitted / Approved / Rejected
    ApprovedById    INT NULL REFERENCES Users(UserId),
    ApprovedAt      DATETIME2 NULL,
    RejectionReason NVARCHAR(500) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Expenses Table
CREATE TABLE Expenses (
    ExpenseId       INT IDENTITY(1,1) PRIMARY KEY,
    SalesmanId      INT NOT NULL REFERENCES Salesmen(SalesmanId),
    ExpenseReportId INT NULL REFERENCES ExpenseReports(ExpenseReportId),
    ExpenseType     NVARCHAR(100) NOT NULL,  -- Travel / Food / Accommodation / Other
    Amount          DECIMAL(10,2) NOT NULL,
    ExpenseDate     DATE NOT NULL,
    Description     NVARCHAR(500) NULL,
    ReceiptUrl      NVARCHAR(500) NULL,
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending / Approved / Rejected
    ApprovedById    INT NULL REFERENCES Users(UserId),
    ApprovedAt      DATETIME2 NULL,
    RejectionReason NVARCHAR(500) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Expense Policies Table
CREATE TABLE ExpensePolicies (
    PolicyId        INT IDENTITY(1,1) PRIMARY KEY,
    PolicyName      NVARCHAR(200) NOT NULL,
    ExpenseType     NVARCHAR(100) NOT NULL,
    MaxAmount       DECIMAL(10,2) NOT NULL,
    ApplicableRole  NVARCHAR(20) NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- TADA Claims Table
CREATE TABLE TadaClaims (
    TadaClaimId         INT IDENTITY(1,1) PRIMARY KEY,
    SalesmanId          INT NOT NULL REFERENCES Salesmen(SalesmanId),
    ClaimMonth          NVARCHAR(20) NOT NULL,
    ClaimYear           INT NOT NULL,
    TravelAmount        DECIMAL(10,2) NOT NULL DEFAULT 0,
    DailyAllowanceAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
    TotalAmount         DECIMAL(10,2) NOT NULL DEFAULT 0,
    Status              NVARCHAR(20) NOT NULL DEFAULT 'Draft', -- Draft / Submitted / Approved / Rejected
    ApprovedById        INT NULL REFERENCES Users(UserId),
    ApprovedAt          DATETIME2 NULL,
    RejectionReason     NVARCHAR(500) NULL,
    Remarks             NVARCHAR(500) NULL,
    SupportingDocUrl    NVARCHAR(500) NULL,
    CreatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Notifications Table
CREATE TABLE Notifications (
    NotificationId  INT IDENTITY(1,1) PRIMARY KEY,
    UserId          INT NOT NULL REFERENCES Users(UserId),
    Title           NVARCHAR(200) NOT NULL,
    Body            NVARCHAR(1000) NOT NULL,
    Type            NVARCHAR(50) NULL,
    ReferenceId     NVARCHAR(50) NULL,
    ReferenceType   NVARCHAR(50) NULL,
    IsRead          BIT NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- User Module Permissions Table
CREATE TABLE UserModulePermissions (
    PermissionId    INT IDENTITY(1,1) PRIMARY KEY,
    UserId          INT NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
    ModuleName      NVARCHAR(100) NOT NULL,
    PermLevel       INT NOT NULL DEFAULT 0,  -- 0=None, 1=View, 2=User, 3=Admin
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UNIQUE (UserId, ModuleName)
);

-- Specimens Table
CREATE TABLE Specimens (
    SpecimenId          INT IDENTITY(1,1) PRIMARY KEY,
    BookId              INT NOT NULL REFERENCES Books(BookId),
    SalesmanId          INT NOT NULL REFERENCES Salesmen(SalesmanId),
    SchoolId            INT NULL REFERENCES Schools(SchoolId),
    TeacherContactId    INT NULL REFERENCES SchoolContacts(ContactId),
    Status              NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending / Allocated / Delivered / Returned
    AllocatedDate       DATETIME2 NULL,
    DeliveredDate       DATETIME2 NULL,
    RecipientName       NVARCHAR(200) NULL,
    Notes               NVARCHAR(500) NULL,
    CreatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- States Table
CREATE TABLE States (
    StateId     INT IDENTITY(1,1) PRIMARY KEY,
    StateName   NVARCHAR(100) NOT NULL UNIQUE,
    StateCode   NVARCHAR(10) NULL,
    IsActive    BIT NOT NULL DEFAULT 1
);

-- Cities Table
CREATE TABLE Cities (
    CityId      INT IDENTITY(1,1) PRIMARY KEY,
    CityName    NVARCHAR(100) NOT NULL,
    StateId     INT NOT NULL REFERENCES States(StateId),
    IsActive    BIT NOT NULL DEFAULT 1
);

-- Stations Table
CREATE TABLE Stations (
    StationId   INT IDENTITY(1,1) PRIMARY KEY,
    StationName NVARCHAR(100) NOT NULL,
    CityId      INT NOT NULL REFERENCES Cities(CityId),
    IsActive    BIT NOT NULL DEFAULT 1
);

-- Dropdowns Table
CREATE TABLE Dropdowns (
    DropdownId  INT IDENTITY(1,1) PRIMARY KEY,
    Category    NVARCHAR(100) NOT NULL,
    Value       NVARCHAR(200) NOT NULL,
    Label       NVARCHAR(200) NOT NULL,
    SortOrder   INT NOT NULL DEFAULT 0,
    IsActive    BIT NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- PM Schedules Table
CREATE TABLE PmSchedules (
    PmScheduleId    INT IDENTITY(1,1) PRIMARY KEY,
    SalesmanId      INT NOT NULL REFERENCES Salesmen(SalesmanId),
    SchoolId        INT NULL REFERENCES Schools(SchoolId),
    ScheduleDate    DATE NOT NULL,
    PurposeType     NVARCHAR(100) NULL,
    Notes           NVARCHAR(500) NULL,
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Scheduled', -- Scheduled / Completed / Cancelled
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Feedback Table
CREATE TABLE Feedback (
    FeedbackId      INT IDENTITY(1,1) PRIMARY KEY,
    SalesmanId      INT NOT NULL REFERENCES Salesmen(SalesmanId),
    SchoolId        INT NULL REFERENCES Schools(SchoolId),
    BookSellerId    INT NULL REFERENCES BookSellers(BookSellerId),
    FeedbackType    NVARCHAR(50) NULL,
    Subject         NVARCHAR(300) NULL,
    Body            NVARCHAR(MAX) NULL,
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Open', -- Open / InProgress / Resolved / Closed
    ResolvedById    INT NULL REFERENCES Users(UserId),
    ResolvedAt      DATETIME2 NULL,
    ResolutionNotes NVARCHAR(1000) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- =============================================
-- Indexes for performance
-- =============================================

CREATE INDEX IX_Schools_AssignedSalesmanId ON Schools(AssignedSalesmanId);
CREATE INDEX IX_Schools_City ON Schools(City);
CREATE INDEX IX_Schools_State ON Schools(State);
CREATE INDEX IX_BookSellers_AssignedSalesmanId ON BookSellers(AssignedSalesmanId);
CREATE INDEX IX_Visits_SalesmanId ON Visits(SalesmanId);
CREATE INDEX IX_Visits_VisitDate ON Visits(VisitDate);
CREATE INDEX IX_Visits_SchoolId ON Visits(SchoolId);
CREATE INDEX IX_Visits_BookSellerId ON Visits(BookSellerId);
CREATE INDEX IX_Attendance_SalesmanId ON Attendance(SalesmanId);
CREATE INDEX IX_Attendance_Date ON Attendance(AttendanceDate);
CREATE INDEX IX_Expenses_SalesmanId ON Expenses(SalesmanId);
CREATE INDEX IX_Expenses_ExpenseDate ON Expenses(ExpenseDate);
CREATE INDEX IX_Notifications_UserId ON Notifications(UserId);
CREATE INDEX IX_Notifications_IsRead ON Notifications(IsRead);
CREATE INDEX IX_TourPlans_SalesmanId ON TourPlans(SalesmanId);
CREATE INDEX IX_TourPlans_Status ON TourPlans(Status);
CREATE INDEX IX_TadaClaims_SalesmanId ON TadaClaims(SalesmanId);
CREATE INDEX IX_Specimens_BookId ON Specimens(BookId);
CREATE INDEX IX_Specimens_SalesmanId ON Specimens(SalesmanId);

-- =============================================
-- Seed Data
-- =============================================

-- Default Admin user (password: Admin@123)
INSERT INTO Users (Username, PasswordHash, FullName, Email, Phone, Role, IsActive, CreatedAt, UpdatedAt)
VALUES (
    'admin',
    '$2a$11$rHV4E1wNqXvG2KpjvvX5YexQmW8H1P4eY.c9CQJ8f5TKP9JXxBUwO', -- Admin@123
    'System Administrator',
    'admin@goodluck.com',
    '9999999999',
    1,
    1,
    GETUTCDATE(),
    GETUTCDATE()
);

-- Default dropdown values
INSERT INTO Dropdowns (Category, Value, Label, SortOrder) VALUES
('SchoolType', 'Private', 'Private', 1),
('SchoolType', 'Govt', 'Government', 2),
('SchoolType', 'SemiGovt', 'Semi-Government', 3),
('Board', 'CBSE', 'CBSE', 1),
('Board', 'ICSE', 'ICSE', 2),
('Board', 'State', 'State Board', 3),
('Board', 'IB', 'International Baccalaureate', 4),
('Category', 'A', 'Category A', 1),
('Category', 'B', 'Category B', 2),
('Category', 'C', 'Category C', 3),
('ExpenseType', 'Travel', 'Travel', 1),
('ExpenseType', 'Food', 'Food', 2),
('ExpenseType', 'Accommodation', 'Accommodation', 3),
('ExpenseType', 'Communication', 'Communication', 4),
('ExpenseType', 'Other', 'Other', 5),
('VisitPurpose', 'Introduction', 'Introduction Visit', 1),
('VisitPurpose', 'FollowUp', 'Follow Up', 2),
('VisitPurpose', 'OrderCollection', 'Order Collection', 3),
('VisitPurpose', 'Specimen', 'Specimen Delivery', 4),
('VisitPurpose', 'Complaint', 'Complaint', 5),
('VisitPurpose', 'Other', 'Other', 6);
