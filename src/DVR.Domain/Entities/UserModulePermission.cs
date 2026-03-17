using DVR.Domain.Enums;

namespace DVR.Domain.Entities;

public class UserModulePermission
{
    public int PermissionId { get; set; }
    public int UserId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public PermLevel PermLevel { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
