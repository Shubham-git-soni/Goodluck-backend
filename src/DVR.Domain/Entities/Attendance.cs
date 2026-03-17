namespace DVR.Domain.Entities;

public class Attendance
{
    public int AttendanceId { get; set; }
    public int SalesmanId { get; set; }
    public DateTime AttendanceDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? CheckInLatitude { get; set; }
    public string? CheckInLongitude { get; set; }
    public string? CheckOutLatitude { get; set; }
    public string? CheckOutLongitude { get; set; }
    public string Status { get; set; } = "Present";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
