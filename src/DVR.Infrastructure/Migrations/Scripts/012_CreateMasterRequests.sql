-- Master Requests table: stores school/bookseller add requests from Salesmen
-- pending admin/manager approval before the record is created in the main table

CREATE TABLE MasterRequests (
    RequestId       INT IDENTITY(1,1) PRIMARY KEY,
    Type            NVARCHAR(20)  NOT NULL,   -- 'School' | 'BookSeller'
    EntityName      NVARCHAR(300) NOT NULL,
    City            NVARCHAR(100) NULL,
    State           NVARCHAR(100) NULL,
    Address         NVARCHAR(500) NULL,
    Phone           NVARCHAR(20)  NULL,
    Board           NVARCHAR(100) NULL,       -- for School
    Strength        INT           NULL,       -- for School (total students)
    OwnerName       NVARCHAR(200) NULL,       -- for BookSeller
    GstNumber       NVARCHAR(50)  NULL,       -- for BookSeller
    SalesmanId      INT NOT NULL REFERENCES Salesmen(SalesmanId),
    Status          NVARCHAR(20)  NOT NULL DEFAULT 'Pending',  -- Pending | Approved | Rejected
    ReviewedById    INT NULL REFERENCES Users(UserId),
    ReviewedAt      DATETIME NULL,
    ReviewerNote    NVARCHAR(500) NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_MasterRequests_SalesmanId ON MasterRequests(SalesmanId);
CREATE INDEX IX_MasterRequests_Status     ON MasterRequests(Status);
