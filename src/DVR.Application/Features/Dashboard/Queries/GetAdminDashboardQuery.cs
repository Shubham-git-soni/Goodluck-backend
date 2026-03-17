using Dapper;
using DVR.Application.Common;
using DVR.Application.Interfaces;
using MediatR;

namespace DVR.Application.Features.Dashboard.Queries;

public class AdminDashboardDto
{
    public int TotalSalesmen { get; set; }
    public int TotalSchools { get; set; }
    public int TotalBooksellers { get; set; }
    public int TodayVisits { get; set; }
    public int TodayCheckIns { get; set; }
    public int PendingTourPlans { get; set; }
    public int PendingExpenses { get; set; }
    public int PendingTadaClaims { get; set; }
    public int PendingApprovals { get; set; }
    public decimal TotalExpensesThisMonth { get; set; }
    public List<SalesmanActivityDto> TopSalesmen { get; set; } = [];
    public List<RecentVisitDto> RecentVisits { get; set; } = [];
}

public class SalesmanActivityDto
{
    public int SalesmanId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int VisitsThisMonth { get; set; }
    public int PresentDays { get; set; }
}

public class RecentVisitDto
{
    public int VisitId { get; set; }
    public string SalesmanName { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string VisitType { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
}

public class GetAdminDashboardQuery : IRequest<ApiResponse<AdminDashboardDto>>
{
}

public class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, ApiResponse<AdminDashboardDto>>
{
    private readonly IDbConnectionFactory _db;

    public GetAdminDashboardQueryHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<ApiResponse<AdminDashboardDto>> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var totalSalesmen = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM Salesmen WHERE IsActive = 1");
        var totalSchools = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM Schools WHERE IsActive = 1");
        var totalBooksellers = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM BookSellers WHERE IsActive = 1");
        var todayVisits = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM Visits WHERE CAST(VisitDate AS DATE) = CAST(GETUTCDATE() AS DATE)");
        var todayCheckIns = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM Attendance WHERE CAST(AttendanceDate AS DATE) = CAST(GETUTCDATE() AS DATE) AND CheckInTime IS NOT NULL");
        var pendingTourPlans = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM TourPlans WHERE Status = 'Submitted'");
        var pendingExpenses = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM ExpenseReports WHERE Status = 'Submitted'");
        var pendingTadaClaims = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM TadaClaims WHERE Status = 'Submitted'");
        var totalExpenses = await conn.QueryFirstOrDefaultAsync<decimal>(
            "SELECT ISNULL(SUM(Amount), 0) FROM Expenses WHERE MONTH(ExpenseDate) = MONTH(GETUTCDATE()) AND YEAR(ExpenseDate) = YEAR(GETUTCDATE())");

        var topSalesmen = await conn.QueryAsync<SalesmanActivityDto>(@"
            SELECT TOP 5 sm.SalesmanId, u.FullName,
                COUNT(v.VisitId) AS VisitsThisMonth,
                COUNT(a.AttendanceId) AS PresentDays
            FROM Salesmen sm
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Visits v ON sm.SalesmanId = v.SalesmanId AND MONTH(v.VisitDate) = MONTH(GETUTCDATE()) AND YEAR(v.VisitDate) = YEAR(GETUTCDATE())
            LEFT JOIN Attendance a ON sm.SalesmanId = a.SalesmanId AND MONTH(a.AttendanceDate) = MONTH(GETUTCDATE()) AND YEAR(a.AttendanceDate) = YEAR(GETUTCDATE()) AND a.Status = 'Present'
            WHERE sm.IsActive = 1
            GROUP BY sm.SalesmanId, u.FullName
            ORDER BY VisitsThisMonth DESC");

        var recentVisits = await conn.QueryAsync<RecentVisitDto>(@"
            SELECT TOP 10 v.VisitId, u.FullName AS SalesmanName,
                COALESCE(s.SchoolName, b.ShopName) AS TargetName,
                CASE v.VisitType WHEN 1 THEN 'School' ELSE 'Bookseller' END AS VisitType,
                v.VisitDate
            FROM Visits v
            JOIN Salesmen sm ON v.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Schools s ON v.SchoolId = s.SchoolId
            LEFT JOIN BookSellers b ON v.BookSellerId = b.BookSellerId
            ORDER BY v.CreatedAt DESC");

        return ApiResponse<AdminDashboardDto>.Ok(new AdminDashboardDto
        {
            TotalSalesmen = totalSalesmen,
            TotalSchools = totalSchools,
            TotalBooksellers = totalBooksellers,
            TodayVisits = todayVisits,
            TodayCheckIns = todayCheckIns,
            PendingTourPlans = pendingTourPlans,
            PendingExpenses = pendingExpenses,
            PendingTadaClaims = pendingTadaClaims,
            PendingApprovals = pendingTourPlans + pendingExpenses + pendingTadaClaims,
            TotalExpensesThisMonth = totalExpenses,
            TopSalesmen = topSalesmen.ToList(),
            RecentVisits = recentVisits.ToList()
        });
    }
}
