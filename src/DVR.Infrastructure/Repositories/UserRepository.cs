using Dapper;
using DVR.Application.Interfaces;
using DVR.Domain.Entities;

namespace DVR.Infrastructure.Repositories;

public class UserRepository
{
    private readonly IDbConnectionFactory _db;

    public UserRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE UserId = @userId AND IsActive = 1", new { userId });
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Username = @username AND IsActive = 1", new { username });
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Email = @email AND IsActive = 1", new { email });
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<User>("SELECT * FROM Users WHERE IsActive = 1 ORDER BY FullName");
    }

    public async Task<int> CreateAsync(User user)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO Users (Username, PasswordHash, FullName, Email, Phone, Role, ManagerId, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.UserId
            VALUES (@Username, @PasswordHash, @FullName, @Email, @Phone, @Role, @ManagerId, 1, GETUTCDATE(), GETUTCDATE())",
            user);
    }

    public async Task<bool> UpdatePasswordAsync(int userId, string passwordHash)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE Users SET PasswordHash = @passwordHash, UpdatedAt = GETUTCDATE() WHERE UserId = @userId",
            new { userId, passwordHash });
        return rows > 0;
    }
}
