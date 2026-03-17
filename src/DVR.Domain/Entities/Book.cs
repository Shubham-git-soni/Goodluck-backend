namespace DVR.Domain.Entities;

public class Book
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Subject { get; set; }
    public string? Class { get; set; }
    public string? Series { get; set; }
    public string? ISBN { get; set; }
    public string? Publisher { get; set; }
    public decimal? Price { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
