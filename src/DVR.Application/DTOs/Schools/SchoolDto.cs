namespace DVR.Application.DTOs.Schools;

public class SchoolDto
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
    public string? AssignedSalesmanName { get; set; }
    public bool IsActive { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public decimal? SalesTarget { get; set; }
    public string? PrescribeSubjects { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SchoolContactDto> Contacts { get; set; } = [];
    public List<SchoolPrescribedBookDto> PrescribedBooks { get; set; } = [];
}

public class SchoolContactDto
{
    public int ContactId { get; set; }
    public int SchoolId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Subject { get; set; }
    public bool IsPrimary { get; set; }
}

public class SchoolPrescribedBookDto
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int BookId { get; set; }
    public string? Title { get; set; }
    public string? Subject { get; set; }
    public string? ClassYear { get; set; }
    public int? Quantity { get; set; }
}

public class SchoolQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Board { get; set; }
    public int? SalesmanId { get; set; }
    public string? SortBy { get; set; }
    public string? SortDir { get; set; } = "asc";
    public int Offset => (Page - 1) * PageSize;
}
