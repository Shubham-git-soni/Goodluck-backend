namespace DVR.Domain.Entities;

public class Salesman
{
    public int SalesmanId { get; set; }
    public int UserId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string? Territory { get; set; }
    public string? Zone { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public int? ManagerId { get; set; }
    public string? Designation { get; set; }
    public DateTime? JoiningDate { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal SalesTarget { get; set; }
    public decimal SalesAchieved { get; set; }
    public decimal SpecimenBudget { get; set; }
    public decimal SpecimenUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
