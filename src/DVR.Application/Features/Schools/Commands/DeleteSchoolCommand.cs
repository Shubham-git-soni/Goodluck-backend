using Dapper;
using DVR.Application.Common;
using DVR.Application.Interfaces;
using MediatR;

namespace DVR.Application.Features.Schools.Commands;

public class DeleteSchoolCommand : IRequest<ApiResponse>
{
    public int SchoolId { get; set; }
}

public class DeleteSchoolCommandHandler : IRequestHandler<DeleteSchoolCommand, ApiResponse>
{
    private readonly IDbConnectionFactory _db;

    public DeleteSchoolCommandHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<ApiResponse> Handle(DeleteSchoolCommand command, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var rows = await conn.ExecuteAsync(
            "UPDATE Schools SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE SchoolId = @SchoolId AND IsActive = 1",
            new { command.SchoolId });

        return rows > 0
            ? ApiResponse.Ok("School deleted.")
            : ApiResponse.Fail("School not found.");
    }
}
