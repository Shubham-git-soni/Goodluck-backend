using Dapper;
using DVR.Application.Common;
using DVR.Application.Interfaces;
using MediatR;

namespace DVR.Application.Features.Dashboard.Queries;

public class SalesmanDashboardDto
{
    public int TotalSchools { get; set; }
    public int TotalBooksellers { get; set; }
    public int VisitsThisMonth { get; set; }
    public int VisitsToday { get; set; }
    public bool CheckedInToday { get; set; }
    public DateTime? TodayCheckIn { get; set; }
    public int PendingTourPlans { get; set; }
    public int PendingExpenses { get; set; }
    public int PendingTadaClaims { get; set; }
    public decimal ExpensesThisMonth { get; set; }
    public int PresentDaysThisMonth { get; set; }
    public decimal SalesTarget { get; set; }
    public decimal SalesAchieved { get; set; }
    public decimal SpecimenBudget { get; set; }
    public decimal SpecimenUsed { get; set; }
    public List<RecentVisitDto> RecentVisits { get; set; } = [];
}

public class GetSalesmanDashboardQuery : IRequest<ApiResponse<SalesmanDashboardDto>>
{
}

public class GetSalesmanDashboardQueryHandler : IRequestHandler<GetSalesmanDashboardQuery, ApiResponse<SalesmanDashboardDto>>
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;

    public GetSalesmanDashboardQueryHandler(IDbConnectionFactory db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<SalesmanDashboardDto>> Handle(GetSalesmanDashboardQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var salesmanId = await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });

        if (!salesmanId.HasValue)
            return ApiResponse<SalesmanDashboardDto>.Fail("Salesman profile not found.");

        var sid = salesmanId.Value;

        var totalSchools = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM Schools WHERE AssignedSalesmanId = @sid AND IsActive = 1", new { sid });
        var totalBooksellers = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM BookSellers WHERE AssignedSalesmanId = @sid AND IsActive = 1", new { sid });
        var visitsThisMonth = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM Visits WHERE SalesmanId = @sid AND MONTH(VisitDate) = MONTH(GETUTCDATE()) AND YEAR(VisitDate) = YEAR(GETUTCDATE())", new { sid });
        var visitsToday = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM Visits WHERE SalesmanId = @sid AND CAST(VisitDate AS DATE) = CAST(GETUTCDATE() AS DATE)", new { sid });
        var todayAttendance = await conn.QueryFirstOrDefaultAsync<Domain.Entities.Attendance>(
            "SELECT * FROM Attendance WHERE SalesmanId = @sid AND CAST(AttendanceDate AS DATE) = CAST(GETUTCDATE() AS DATE)", new { sid });
        var pendingTourPlans = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM TourPlans WHERE SalesmanId = @sid AND Status = 'Draft'", new { sid });
        var pendingExpenses = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM Expenses WHERE SalesmanId = @sid AND Status = 'Pending'", new { sid });
        var pendingTada = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM TadaClaims WHERE SalesmanId = @sid AND Status = 'Draft'", new { sid });
        var expensesThisMonth = await conn.QueryFirstOrDefaultAsync<decimal>(
            "SELECT ISNULL(SUM(Amount), 0) FROM Expenses WHERE SalesmanId = @sid AND MONTH(ExpenseDate) = MONTH(GETUTCDATE()) AND YEAR(ExpenseDate) = YEAR(GETUTCDATE())", new { sid });
        var presentDays = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM Attendance WHERE SalesmanId = @sid AND MONTH(AttendanceDate) = MONTH(GETUTCDATE()) AND YEAR(AttendanceDate) = YEAR(GETUTCDATE()) AND Status = 'Present'", new { sid });

        var salesmanInfo = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT SalesTarget, SalesAchieved, SpecimenBudget, SpecimenUsed FROM Salesmen WHERE SalesmanId = @sid", new { sid });

        var recentVisits = await conn.QueryAsync<RecentVisitDto>(@"
            SELECT TOP 5 v.VisitId, u.FullName AS SalesmanName,
                COALESCE(s.SchoolName, b.ShopName) AS TargetName,
                CASE v.VisitType WHEN 1 THEN 'School' ELSE 'Bookseller' END AS VisitType,
                v.VisitDate
            FROM Visits v
            JOIN Salesmen sm ON v.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Schools s ON v.SchoolId = s.SchoolId
            LEFT JOIN BookSellers b ON v.BookSellerId = b.BookSellerId
            WHERE v.SalesmanId = @sid
            ORDER BY v.CreatedAt DESC", new { sid });

        return ApiResponse<SalesmanDashboardDto>.Ok(new SalesmanDashboardDto
        {
            TotalSchools = totalSchools,
            TotalBooksellers = totalBooksellers,
            VisitsThisMonth = visitsThisMonth,
            VisitsToday = visitsToday,
            CheckedInToday = todayAttendance?.CheckInTime != null,
            TodayCheckIn = todayAttendance?.CheckInTime,
            PendingTourPlans = pendingTourPlans,
            PendingExpenses = pendingExpenses,
            PendingTadaClaims = pendingTada,
            ExpensesThisMonth = expensesThisMonth,
            PresentDaysThisMonth = presentDays,
            SalesTarget = salesmanInfo?.SalesTarget ?? 0,
            SalesAchieved = salesmanInfo?.SalesAchieved ?? 0,
            SpecimenBudget = salesmanInfo?.SpecimenBudget ?? 0,
            SpecimenUsed = salesmanInfo?.SpecimenUsed ?? 0,
            RecentVisits = recentVisits.ToList()
        });
    }
}
