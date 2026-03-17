using Dapper;
using DVR.Application.Common;
using DVR.Application.DTOs.Auth;
using DVR.Application.Features.Authentication.Commands;
using DVR.Application.Features.Authentication.Queries;
using DVR.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;

    public AuthController(IMediator mediator, IDbConnectionFactory db, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _db = db;
        _currentUser = currentUser;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _mediator.Send(new LoginCommand
        {
            Email = request.Email,
            Password = request.Password,
            DeviceToken = request.DeviceToken
        });
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _mediator.Send(new RefreshTokenCommand { RefreshToken = request.RefreshToken });
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Users SET RefreshToken = NULL, RefreshTokenExpiry = NULL, UpdatedAt = GETUTCDATE() WHERE UserId = @UserId",
            new { _currentUser.UserId });
        return Ok(ApiResponse.Ok("Logged out successfully."));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var result = await _mediator.Send(new GetCurrentUserQuery());
        return result.Success ? Ok(result) : NotFound(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        using var conn = _db.CreateConnection();
        var user = await conn.QueryFirstOrDefaultAsync<DVR.Domain.Entities.User>(
            "SELECT * FROM Users WHERE UserId = @UserId", new { _currentUser.UserId });

        if (user is null || user.PasswordHash != request.CurrentPassword)
            return BadRequest(ApiResponse.Fail("Current password is incorrect."));

        await conn.ExecuteAsync(
            "UPDATE Users SET PasswordHash = @NewPassword, UpdatedAt = GETUTCDATE() WHERE UserId = @UserId",
            new { NewPassword = request.NewPassword, _currentUser.UserId });

        return Ok(ApiResponse.Ok("Password changed successfully."));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        using var conn = _db.CreateConnection();
        var user = await conn.QueryFirstOrDefaultAsync<DVR.Domain.Entities.User>(
            "SELECT * FROM Users WHERE Email = @Email AND IsActive = 1", new { request.Email });

        // Always return success to prevent email enumeration
        return Ok(ApiResponse.Ok("If the email exists, a reset link has been sent."));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        return Ok(ApiResponse.Ok("Password reset is not yet implemented. Contact your administrator."));
    }

    [Authorize]
    [HttpPost("register-device")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Users SET DeviceToken = @DeviceToken, UpdatedAt = GETUTCDATE() WHERE UserId = @UserId",
            new { request.DeviceToken, _currentUser.UserId });
        return Ok(ApiResponse.Ok("Device registered."));
    }

    [Authorize]
    [HttpDelete("unregister-device")]
    public async Task<IActionResult> UnregisterDevice()
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Users SET DeviceToken = NULL, UpdatedAt = GETUTCDATE() WHERE UserId = @UserId",
            new { _currentUser.UserId });
        return Ok(ApiResponse.Ok("Device unregistered."));
    }
}
