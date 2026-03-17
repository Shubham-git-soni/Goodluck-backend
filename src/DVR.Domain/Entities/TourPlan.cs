namespace DVR.Domain.Entities;

public class TourPlan
{
    public int TourPlanId { get; set; }
    public int SalesmanId { get; set; }
    public DateTime PlanDate { get; set; }
    public string? PlannedAreas { get; set; }
    public string? PlannedVisits { get; set; }
    public string Status { get; set; } = "Draft";
    public int? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
