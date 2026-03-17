using Dapper;
using DVR.Application.Interfaces;
using DVR.Domain.Entities;

namespace DVR.Infrastructure.Repositories;

public class SchoolRepository
{
    private readonly IDbConnectionFactory _db;

    public SchoolRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<School?> GetByIdAsync(int schoolId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<School>(
            "SELECT * FROM Schools WHERE SchoolId = @schoolId AND IsActive = 1", new { schoolId });
    }

    public async Task<IEnumerable<School>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<School>("SELECT * FROM Schools WHERE IsActive = 1 ORDER BY SchoolName");
    }

    public async Task<int> CreateAsync(School school)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO Schools (SchoolName, PrincipalName, Phone, Email, Address, City, State, Pincode,
                SchoolType, Board, TotalStudents, Category, AssignedSalesmanId, Latitude, Longitude, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.SchoolId
            VALUES (@SchoolName, @PrincipalName, @Phone, @Email, @Address, @City, @State, @Pincode,
                @SchoolType, @Board, @TotalStudents, @Category, @AssignedSalesmanId, @Latitude, @Longitude, 1, GETUTCDATE(), GETUTCDATE())",
            school);
    }

    public async Task<bool> UpdateAsync(School school)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Schools SET
                SchoolName = @SchoolName, PrincipalName = @PrincipalName, Phone = @Phone,
                Email = @Email, Address = @Address, City = @City, State = @State,
                Pincode = @Pincode, SchoolType = @SchoolType, Board = @Board,
                TotalStudents = @TotalStudents, Category = @Category,
                AssignedSalesmanId = @AssignedSalesmanId, IsActive = @IsActive,
                Latitude = @Latitude, Longitude = @Longitude, UpdatedAt = GETUTCDATE()
            WHERE SchoolId = @SchoolId", school);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int schoolId)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE Schools SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE SchoolId = @schoolId", new { schoolId });
        return rows > 0;
    }
}
