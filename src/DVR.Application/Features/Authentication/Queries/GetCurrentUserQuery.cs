using DVR.Application.Common;
using DVR.Application.DTOs.Auth;
using DVR.Application.Interfaces;
using MediatR;

namespace DVR.Application.Features.Authentication.Queries;

public class GetCurrentUserQuery : IRequest<ApiResponse<UserInfo>>
{
}

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, ApiResponse<UserInfo>>
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserQueryHandler(IDbConnectionFactory db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<UserInfo>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var user = await Dapper.SqlMapper.QueryFirstOrDefaultAsync<Domain.Entities.User>(
            conn,
            "SELECT * FROM Users WHERE UserId = @UserId AND IsActive = 1",
            new { _currentUser.UserId });

        if (user is null)
            return ApiResponse<UserInfo>.Fail("User not found.");

        return ApiResponse<UserInfo>.Ok(new UserInfo
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            ProfilePhotoUrl = user.ProfilePhotoUrl
        });
    }
}
