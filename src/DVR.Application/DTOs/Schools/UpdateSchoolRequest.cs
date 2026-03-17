namespace DVR.Application.DTOs.Schools;

public class UpdateSchoolRequest
{
    public string? SchoolName { get; set; }
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
    public bool? IsActive { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public decimal? SalesTarget { get; set; }
    public string? PrescribeSubjects { get; set; }
    public List<int>? PrescribedBookIds { get; set; }
}
