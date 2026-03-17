using Dapper;
using DVR.Application.Common;
using DVR.Application.DTOs.Visits;
using DVR.Application.Interfaces;
using DVR.Domain.Enums;
using MediatR;

namespace DVR.Application.Features.Visits.Commands;

public class CreateBooksellerVisitCommand : IRequest<ApiResponse<VisitDto>>
{
    public CreateBooksellerVisitRequest Request { get; set; } = new();
}

public class CreateBooksellerVisitCommandHandler : IRequestHandler<CreateBooksellerVisitCommand, ApiResponse<VisitDto>>
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;

    public CreateBooksellerVisitCommandHandler(IDbConnectionFactory db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<VisitDto>> Handle(CreateBooksellerVisitCommand command, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var r = command.Request;

        var salesmanId = await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });

        if (!salesmanId.HasValue)
            return ApiResponse<VisitDto>.Fail("Salesman profile not found.");

        var bsExists = await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT BookSellerId FROM BookSellers WHERE BookSellerId = @BookSellerId AND IsActive = 1",
            new { r.BookSellerId });

        if (!bsExists.HasValue)
            return ApiResponse<VisitDto>.Fail("Bookseller not found.");

        var id = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO Visits (SalesmanId, VisitType, BookSellerId, VisitDate, Purpose, Remarks, Outcome,
                FollowUpDate, CheckInLatitude, CheckInLongitude, PhotoUrl, IsCompleted, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.VisitId
            VALUES (@SalesmanId, @VisitType, @BookSellerId, @VisitDate, @Purpose, @Remarks, @Outcome,
                @FollowUpDate, @CheckInLatitude, @CheckInLongitude, @PhotoUrl, 0, GETUTCDATE(), GETUTCDATE())",
            new
            {
                SalesmanId = salesmanId.Value,
                VisitType = (int)VisitType.Bookseller,
                r.BookSellerId, r.VisitDate, r.Purpose, r.Remarks, r.Outcome,
                r.FollowUpDate, r.CheckInLatitude, r.CheckInLongitude, r.PhotoUrl
            });

        var visit = await conn.QueryFirstOrDefaultAsync<VisitDto>(@"
            SELECT v.*, u.FullName AS SalesmanName, b.ShopName AS BookSellerName
            FROM Visits v
            JOIN Salesmen sm ON v.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN BookSellers b ON v.BookSellerId = b.BookSellerId
            WHERE v.VisitId = @id", new { id });

        return ApiResponse<VisitDto>.Ok(visit!, "Bookseller visit recorded.");
    }
}
