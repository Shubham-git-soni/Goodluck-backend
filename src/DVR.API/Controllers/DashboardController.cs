using DVR.Application.Features.Dashboard.Queries;
using DVR.Application.Interfaces;
using DVR.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public DashboardController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> AdminDashboard()
    {
        var result = await _mediator.Send(new GetAdminDashboardQuery());
        return Ok(result);
    }

    [HttpGet("salesman")]
    public async Task<IActionResult> SalesmanDashboard()
    {
        var result = await _mediator.Send(new GetSalesmanDashboardQuery());
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
