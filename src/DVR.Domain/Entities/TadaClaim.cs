namespace DVR.Domain.Entities;

public class TadaClaim
{
    public int TadaClaimId { get; set; }
    public int SalesmanId { get; set; }
    public string ClaimMonth { get; set; } = string.Empty;
    public int ClaimYear { get; set; }
    public decimal TravelAmount { get; set; }
    public decimal DailyAllowanceAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Draft";
    public int? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? Remarks { get; set; }
    public string? SupportingDocUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
