namespace DVR.Domain.Entities;

public class School
{
    public int SchoolId { get; set; }
    public string SchoolName { get; set; } = string.Empty;
    public string? PrincipalName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public string? SchoolType { get; set; }
    public string? Board { get; set; }
    public int? TotalStudents { get; set; }
    public string? Category { get; set; }
    public int? AssignedSalesmanId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
