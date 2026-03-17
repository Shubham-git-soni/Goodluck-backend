namespace DVR.Application.DTOs.Visits;

public class VisitDto
{
    public int VisitId { get; set; }
    public int SalesmanId { get; set; }
    public string SalesmanName { get; set; } = string.Empty;
    public string VisitType { get; set; } = string.Empty;
    public int? SchoolId { get; set; }
    public string? SchoolName { get; set; }
    public int? BookSellerId { get; set; }
    public string? BookSellerName { get; set; }
    public DateTime VisitDate { get; set; }
    public string? CheckInTime { get; set; }
    public string? CheckOutTime { get; set; }
    public string? Purpose { get; set; }
    public string? Remarks { get; set; }
    public string? Outcome { get; set; }
    public string? FollowUpDate { get; set; }
    public string? CheckInLatitude { get; set; }
    public string? CheckInLongitude { get; set; }
    public string? CheckOutLatitude { get; set; }
    public string? CheckOutLongitude { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VisitQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public int? SalesmanId { get; set; }
    public string? VisitType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Offset => (Page - 1) * PageSize;
}
