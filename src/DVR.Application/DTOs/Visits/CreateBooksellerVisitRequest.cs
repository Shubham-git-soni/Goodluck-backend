namespace DVR.Application.DTOs.Visits;

public class CreateBooksellerVisitRequest
{
    public int BookSellerId { get; set; }
    public DateTime VisitDate { get; set; }
    public string? Purpose { get; set; }
    public string? Remarks { get; set; }
    public string? Outcome { get; set; }
    public string? FollowUpDate { get; set; }
    public string? CheckInLatitude { get; set; }
    public string? CheckInLongitude { get; set; }
    public string? PhotoUrl { get; set; }
}
