namespace DVR.Domain.Entities;

public class Specimen
{
    public int SpecimenId { get; set; }
    public int BookId { get; set; }
    public int SalesmanId { get; set; }
    public int? SchoolId { get; set; }
    public int? TeacherContactId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? AllocatedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public string? RecipientName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
