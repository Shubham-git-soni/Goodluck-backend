namespace DVR.Application.DTOs.Salesmen;

public class CreateSalesmanRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string? Territory { get; set; }
    public string? Zone { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public int? ManagerId { get; set; }
    public string? Designation { get; set; }
    public DateTime? JoiningDate { get; set; }
}
