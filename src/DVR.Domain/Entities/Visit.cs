using DVR.Domain.Enums;

namespace DVR.Domain.Entities;

public class Visit
{
    public int VisitId { get; set; }
    public int SalesmanId { get; set; }
    public VisitType VisitType { get; set; }
    public int? SchoolId { get; set; }
    public int? BookSellerId { get; set; }
    public DateTime VisitDate { get; set; }
    public TimeSpan? CheckInTime { get; set; }
    public TimeSpan? CheckOutTime { get; set; }
    public string? Purpose { get; set; }
    public string? Remarks { get; set; }
    public string? Outcome { get; set; }
    public string? FollowUpDate { get; set; }
    public string? CheckInLatitude { get; set; }
    public string? CheckInLongitude { get; set; }
    public string? CheckOutLatitude { get; set; }
    public string? CheckOutLongitude { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
