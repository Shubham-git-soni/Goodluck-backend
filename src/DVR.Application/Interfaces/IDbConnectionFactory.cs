using System.Data;

namespace DVR.Application.Interfaces;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
