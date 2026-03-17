using System.Security.Claims;
using DVR.Application.Interfaces;
using DVR.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace DVR.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public int UserId
    {
        get
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User?.FindFirst("sub")?.Value;
            return claim != null ? int.Parse(claim) : 0;
        }
    }

    public string Username => User?.FindFirst(ClaimTypes.Name)?.Value
        ?? User?.FindFirst("unique_name")?.Value
        ?? string.Empty;

    public Role Role
    {
        get
        {
            var roleClaim = User?.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<Role>(roleClaim, out var role) ? role : Role.Salesman;
        }
    }

    public bool IsAdmin => Role == Role.Admin;
    public bool IsManager => Role == Role.Manager;
    public bool IsSalesman => Role == Role.Salesman;
}
