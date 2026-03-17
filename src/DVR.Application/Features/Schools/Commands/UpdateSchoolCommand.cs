using Dapper;
using DVR.Application.Common;
using DVR.Application.DTOs.Schools;
using DVR.Application.Interfaces;
using MediatR;

namespace DVR.Application.Features.Schools.Commands;

public class UpdateSchoolCommand : IRequest<ApiResponse<SchoolDto>>
{
    public int SchoolId { get; set; }
    public UpdateSchoolRequest Request { get; set; } = new();
}

public class UpdateSchoolCommandHandler : IRequestHandler<UpdateSchoolCommand, ApiResponse<SchoolDto>>
{
    private readonly IDbConnectionFactory _db;

    public UpdateSchoolCommandHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<ApiResponse<SchoolDto>> Handle(UpdateSchoolCommand command, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var r = command.Request;

        var existing = await conn.QueryFirstOrDefaultAsync<Domain.Entities.School>(
            "SELECT * FROM Schools WHERE SchoolId = @SchoolId AND IsActive = 1",
            new { command.SchoolId });

        if (existing is null)
            return ApiResponse<SchoolDto>.Fail("School not found.");

        await conn.ExecuteAsync(@"
            UPDATE Schools SET
                SchoolName = COALESCE(@SchoolName, SchoolName),
                PrincipalName = COALESCE(@PrincipalName, PrincipalName),
                Phone = COALESCE(@Phone, Phone),
                Email = COALESCE(@Email, Email),
                Address = COALESCE(@Address, Address),
                City = COALESCE(@City, City),
                State = COALESCE(@State, State),
                Pincode = COALESCE(@Pincode, Pincode),
                SchoolType = COALESCE(@SchoolType, SchoolType),
                Board = COALESCE(@Board, Board),
                TotalStudents = COALESCE(@TotalStudents, TotalStudents),
                Category = COALESCE(@Category, Category),
                AssignedSalesmanId = COALESCE(@AssignedSalesmanId, AssignedSalesmanId),
                IsActive = COALESCE(@IsActive, IsActive),
                Latitude = COALESCE(@Latitude, Latitude),
                Longitude = COALESCE(@Longitude, Longitude),
                SalesTarget = COALESCE(@SalesTarget, SalesTarget),
                PrescribeSubjects = COALESCE(@PrescribeSubjects, PrescribeSubjects),
                UpdatedAt = GETUTCDATE()
            WHERE SchoolId = @SchoolId",
            new
            {
                r.SchoolName, r.PrincipalName, r.Phone, r.Email, r.Address, r.City, r.State, r.Pincode,
                r.SchoolType, r.Board, r.TotalStudents, r.Category, r.AssignedSalesmanId, r.IsActive,
                r.Latitude, r.Longitude, r.SalesTarget, r.PrescribeSubjects, command.SchoolId
            });

        // Sync prescribed books if provided
        if (r.PrescribedBookIds != null)
        {
            await conn.ExecuteAsync("DELETE FROM SchoolPrescribedBooks WHERE SchoolId = @SchoolId", new { command.SchoolId });
            foreach (var bookId in r.PrescribedBookIds)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO SchoolPrescribedBooks (SchoolId, BookId, CreatedAt) VALUES (@SchoolId, @BookId, GETUTCDATE())",
                    new { SchoolId = command.SchoolId, BookId = bookId });
            }
        }

        var updated = await conn.QueryFirstOrDefaultAsync<SchoolDto>(
            "SELECT * FROM Schools WHERE SchoolId = @SchoolId", new { command.SchoolId });

        return ApiResponse<SchoolDto>.Ok(updated!, "School updated successfully.");
    }
}
