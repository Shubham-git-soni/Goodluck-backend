namespace DVR.Application.DTOs.Salesmen;

public class UpdateSalesmanRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Territory { get; set; }
    public string? Zone { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public int? ManagerId { get; set; }
    public string? Designation { get; set; }
    public DateTime? JoiningDate { get; set; }
    public bool? IsActive { get; set; }
}
