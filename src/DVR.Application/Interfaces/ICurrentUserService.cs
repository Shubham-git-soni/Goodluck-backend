using DVR.Domain.Enums;

namespace DVR.Application.Interfaces;

public interface ICurrentUserService
{
    int UserId { get; }
    string Username { get; }
    Role Role { get; }
    bool IsAdmin { get; }
    bool IsManager { get; }
    bool IsSalesman { get; }
}
