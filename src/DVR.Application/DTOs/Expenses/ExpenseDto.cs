namespace DVR.Application.DTOs.Expenses;

public class ExpenseDto
{
    public int ExpenseId { get; set; }
    public int SalesmanId { get; set; }
    public string SalesmanName { get; set; } = string.Empty;
    public int? ExpenseReportId { get; set; }
    public string ExpenseType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public string? ReceiptUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExpenseQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? SalesmanId { get; set; }
    public string? Status { get; set; }
    public string? ExpenseType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Offset => (Page - 1) * PageSize;
}

public class ExpenseReportDto
{
    public int ExpenseReportId { get; set; }
    public int SalesmanId { get; set; }
    public string SalesmanName { get; set; } = string.Empty;
    public string ReportMonth { get; set; } = string.Empty;
    public int ReportYear { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<ExpenseDto> Expenses { get; set; } = [];
}

public class ExpensePolicyDto
{
    public int PolicyId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public string ExpenseType { get; set; } = string.Empty;
    public decimal MaxAmount { get; set; }
    public string? ApplicableRole { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
