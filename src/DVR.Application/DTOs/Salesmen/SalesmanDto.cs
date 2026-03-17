namespace DVR.Application.DTOs.Salesmen;

public class SalesmanDto
{
    public int SalesmanId { get; set; }
    public int UserId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Territory { get; set; }
    public string? Zone { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public string? Designation { get; set; }
    public DateTime? JoiningDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SalesmanPerformanceDto
{
    public int SalesmanId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int TotalVisits { get; set; }
    public int SchoolVisits { get; set; }
    public int BooksellerVisits { get; set; }
    public int TotalSchools { get; set; }
    public int TotalBooksellers { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public decimal TotalExpenses { get; set; }
    public int PendingTourPlans { get; set; }
    public int ApprovedTourPlans { get; set; }
}
