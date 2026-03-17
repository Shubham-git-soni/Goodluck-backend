namespace DVR.Application.DTOs.Expenses;

public class CreateExpenseRequest
{
    public string ExpenseType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public string? ReceiptUrl { get; set; }
    public int? ExpenseReportId { get; set; }
}

public class CreateExpenseReportRequest
{
    public string ReportMonth { get; set; } = string.Empty;
    public int ReportYear { get; set; }
    public List<int>? ExpenseIds { get; set; }
}

public class CreateExpensePolicyRequest
{
    public string PolicyName { get; set; } = string.Empty;
    public string ExpenseType { get; set; } = string.Empty;
    public decimal MaxAmount { get; set; }
    public string? ApplicableRole { get; set; }
}

public class ApproveRejectRequest
{
    public string? Reason { get; set; }
}
