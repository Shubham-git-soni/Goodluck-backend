using DVR.Application.Common;
using DVR.Application.DTOs.Auth;
using DVR.Application.Interfaces;
using MediatR;

namespace DVR.Application.Features.Authentication.Commands;

public class RefreshTokenCommand : IRequest<ApiResponse<LoginResponse>>
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<LoginResponse>>
{
    private readonly IDbConnectionFactory _db;
    private readonly IJwtService _jwtService;

    public RefreshTokenCommandHandler(IDbConnectionFactory db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<ApiResponse<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var user = await Dapper.SqlMapper.QueryFirstOrDefaultAsync<Domain.Entities.User>(
            conn,
            "SELECT * FROM Users WHERE RefreshToken = @RefreshToken AND RefreshTokenExpiry > GETUTCDATE() AND IsActive = 1",
            new { request.RefreshToken });

        if (user is null)
            return ApiResponse<LoginResponse>.Fail("Invalid or expired refresh token.");

        var accessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var expiry = DateTime.UtcNow.AddDays(7);

        await Dapper.SqlMapper.ExecuteAsync(conn,
            "UPDATE Users SET RefreshToken = @RefreshToken, RefreshTokenExpiry = @Expiry, UpdatedAt = GETUTCDATE() WHERE UserId = @UserId",
            new { RefreshToken = newRefreshToken, Expiry = expiry, user.UserId });

        return ApiResponse<LoginResponse>.Ok(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(30),
            User = new UserInfo
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role.ToString(),
                ProfilePhotoUrl = user.ProfilePhotoUrl
            }
        }, "Token refreshed.");
    }
}
