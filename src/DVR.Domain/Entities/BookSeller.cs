namespace DVR.Domain.Entities;

public class BookSeller
{
    public int BookSellerId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public int? AssignedSalesmanId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
