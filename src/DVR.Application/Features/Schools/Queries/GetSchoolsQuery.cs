using Dapper;
using DVR.Application.Common;
using DVR.Application.DTOs.Schools;
using DVR.Application.Interfaces;
using DVR.Domain.Enums;
using MediatR;
using System.Linq;

namespace DVR.Application.Features.Schools.Queries;

public class GetSchoolsQuery : IRequest<PaginatedResponse<SchoolDto>>
{
    public SchoolQueryParams Params { get; set; } = new();
}

public class GetSchoolsQueryHandler : IRequestHandler<GetSchoolsQuery, PaginatedResponse<SchoolDto>>
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;

    public GetSchoolsQueryHandler(IDbConnectionFactory db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PaginatedResponse<SchoolDto>> Handle(GetSchoolsQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var p = request.Params;

        var where = new List<string> { "s.IsActive = 1" };
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            where.Add("(s.SchoolName LIKE @Search OR s.PrincipalName LIKE @Search OR s.City LIKE @Search)");
            parameters.Add("Search", $"%{p.Search}%");
        }
        if (!string.IsNullOrWhiteSpace(p.City))
        {
            where.Add("s.City = @City");
            parameters.Add("City", p.City);
        }
        if (!string.IsNullOrWhiteSpace(p.State))
        {
            where.Add("s.State = @State");
            parameters.Add("State", p.State);
        }
        if (!string.IsNullOrWhiteSpace(p.Board))
        {
            where.Add("s.Board = @Board");
            parameters.Add("Board", p.Board);
        }

        if (_currentUser.IsSalesman)
        {
            where.Add("s.AssignedSalesmanId = @CurrentSalesmanId");
            var salesmanId = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });
            parameters.Add("CurrentSalesmanId", salesmanId);
        }
        else if (_currentUser.IsManager)
        {
            where.Add("s.AssignedSalesmanId IN (SELECT SalesmanId FROM Salesmen WHERE ManagerId = @ManagerId)");
            parameters.Add("ManagerId", _currentUser.UserId);
        }
        else if (p.SalesmanId.HasValue)
        {
            where.Add("s.AssignedSalesmanId = @SalesmanId");
            parameters.Add("SalesmanId", p.SalesmanId.Value);
        }

        var whereClause = string.Join(" AND ", where);
        var sortBy = p.SortBy ?? "s.SchoolName";
        var sortDir = p.SortDir?.ToLower() == "desc" ? "DESC" : "ASC";

        parameters.Add("Offset", p.Offset);
        parameters.Add("PageSize", p.PageSize);

        var countSql = $"SELECT COUNT(*) FROM Schools s WHERE {whereClause}";
        var dataSql = $@"
            SELECT s.SchoolId, s.SchoolName, s.PrincipalName, s.Phone, s.Email, s.Address,
                   s.City, s.State, s.Pincode, s.SchoolType, s.Board, s.TotalStudents,
                   s.Category, s.AssignedSalesmanId, s.IsActive, s.Latitude, s.Longitude, s.CreatedAt,
                   u.FullName AS AssignedSalesmanName
            FROM Schools s
            LEFT JOIN Salesmen sm ON s.AssignedSalesmanId = sm.SalesmanId
            LEFT JOIN Users u ON sm.UserId = u.UserId
            WHERE {whereClause}
            ORDER BY {sortBy} {sortDir}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var total = await conn.QueryFirstOrDefaultAsync<int>(countSql, parameters);
        var schools = (await conn.QueryAsync<SchoolDto>(dataSql, parameters)).ToList();

        // Fetch all active contacts for the returned schools in one query
        if (schools.Count > 0)
        {
            var schoolIds = schools.Select(s => s.SchoolId).ToList();
            var contacts = await conn.QueryAsync<SchoolContactDto>(
                @"SELECT ContactId, SchoolId, Name, Designation, Phone, Email, Subject, IsPrimary
                  FROM SchoolContacts
                  WHERE SchoolId IN @schoolIds AND IsDeleted = 0
                  ORDER BY IsPrimary DESC, ContactId ASC",
                new { schoolIds });

            var contactsBySchool = contacts.GroupBy(c => c.SchoolId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var school in schools)
            {
                if (contactsBySchool.TryGetValue(school.SchoolId, out var schoolContacts))
                    school.Contacts = schoolContacts;
            }
        }

        return new PaginatedResponse<SchoolDto>
        {
            Success = true,
            Message = "Schools retrieved.",
            Data = schools,
            Pagination = new PaginationMeta { Page = p.Page, PageSize = p.PageSize, TotalCount = total }
        };
    }
}
