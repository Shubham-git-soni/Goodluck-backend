using DVR.Application.Common;
using DVR.Application.DTOs.Auth;
using DVR.Application.Interfaces;
using MediatR;

namespace DVR.Application.Features.Authentication.Commands;

public class LoginCommand : IRequest<ApiResponse<LoginResponse>>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DeviceToken { get; set; }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<LoginResponse>>
{
    private readonly IDbConnectionFactory _db;
    private readonly IJwtService _jwtService;

    public LoginCommandHandler(IDbConnectionFactory db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<ApiResponse<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var user = await Dapper.SqlMapper.QueryFirstOrDefaultAsync<Domain.Entities.User>(
            conn,
            "SELECT * FROM Users WHERE Email = @Email AND IsActive = 1",
            new { request.Email });

        if (user is null || user.PasswordHash != request.Password)
            return ApiResponse<LoginResponse>.Fail("Invalid username or password.");

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var expiry = DateTime.UtcNow.AddDays(7);

        await Dapper.SqlMapper.ExecuteAsync(conn,
            "UPDATE Users SET RefreshToken = @RefreshToken, RefreshTokenExpiry = @Expiry, DeviceToken = COALESCE(@DeviceToken, DeviceToken), UpdatedAt = GETUTCDATE() WHERE UserId = @UserId",
            new { RefreshToken = refreshToken, Expiry = expiry, request.DeviceToken, user.UserId });

        return ApiResponse<LoginResponse>.Ok(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
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
        }, "Login successful.");
    }
}
