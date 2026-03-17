using Dapper;
using DVR.Application.Common;
using DVR.Application.DTOs.Visits;
using DVR.Application.Interfaces;
using DVR.Domain.Enums;
using MediatR;

namespace DVR.Application.Features.Visits.Commands;

public class CreateSchoolVisitCommand : IRequest<ApiResponse<VisitDto>>
{
    public CreateSchoolVisitRequest Request { get; set; } = new();
}

public class CreateSchoolVisitCommandHandler : IRequestHandler<CreateSchoolVisitCommand, ApiResponse<VisitDto>>
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;

    public CreateSchoolVisitCommandHandler(IDbConnectionFactory db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<VisitDto>> Handle(CreateSchoolVisitCommand command, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var r = command.Request;

        var salesmanId = await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });

        if (!salesmanId.HasValue)
            return ApiResponse<VisitDto>.Fail("Salesman profile not found.");

        var schoolExists = await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT SchoolId FROM Schools WHERE SchoolId = @SchoolId AND IsActive = 1",
            new { r.SchoolId });

        if (!schoolExists.HasValue)
            return ApiResponse<VisitDto>.Fail("School not found.");

        var id = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO Visits (SalesmanId, VisitType, SchoolId, VisitDate, Purpose, Remarks, Outcome,
                FollowUpDate, CheckInLatitude, CheckInLongitude, PhotoUrl, IsCompleted, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.VisitId
            VALUES (@SalesmanId, @VisitType, @SchoolId, @VisitDate, @Purpose, @Remarks, @Outcome,
                @FollowUpDate, @CheckInLatitude, @CheckInLongitude, @PhotoUrl, 0, GETUTCDATE(), GETUTCDATE())",
            new
            {
                SalesmanId = salesmanId.Value,
                VisitType = (int)VisitType.School,
                r.SchoolId, r.VisitDate, r.Purpose, r.Remarks, r.Outcome,
                r.FollowUpDate, r.CheckInLatitude, r.CheckInLongitude, r.PhotoUrl
            });

        var visit = await conn.QueryFirstOrDefaultAsync<VisitDto>(@"
            SELECT v.*, u.FullName AS SalesmanName, s.SchoolName
            FROM Visits v
            JOIN Salesmen sm ON v.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Schools s ON v.SchoolId = s.SchoolId
            WHERE v.VisitId = @id", new { id });

        return ApiResponse<VisitDto>.Ok(visit!, "School visit recorded.");
    }
}
