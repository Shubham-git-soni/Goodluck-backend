using Dapper;
using DVR.Application.Interfaces;
using DVR.Domain.Entities;

namespace DVR.Infrastructure.Repositories;

public class SalesmanRepository
{
    private readonly IDbConnectionFactory _db;

    public SalesmanRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<Salesman?> GetByIdAsync(int salesmanId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Salesman>(
            "SELECT * FROM Salesmen WHERE SalesmanId = @salesmanId AND IsActive = 1", new { salesmanId });
    }

    public async Task<Salesman?> GetByUserIdAsync(int userId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Salesman>(
            "SELECT * FROM Salesmen WHERE UserId = @userId AND IsActive = 1", new { userId });
    }

    public async Task<IEnumerable<Salesman>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Salesman>("SELECT * FROM Salesmen WHERE IsActive = 1");
    }

    public async Task<IEnumerable<Salesman>> GetByManagerIdAsync(int managerId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Salesman>(
            "SELECT * FROM Salesmen WHERE ManagerId = @managerId AND IsActive = 1", new { managerId });
    }
}
