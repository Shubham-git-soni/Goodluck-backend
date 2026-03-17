using Dapper;
using DVR.Application.Common;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/locations")]
[Authorize]
public class LocationsController : ControllerBase
{
    private readonly IDbConnectionFactory _db;

    public LocationsController(IDbConnectionFactory db)
    {
        _db = db;
    }

    [HttpGet("states")]
    public async Task<IActionResult> GetStates()
    {
        using var conn = _db.CreateConnection();
        var data = await conn.QueryAsync<StateDto>("SELECT StateId, StateName, StateCode FROM States WHERE IsActive = 1 ORDER BY StateName");
        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpGet("cities")]
    public async Task<IActionResult> GetCities([FromQuery] int? stateId)
    {
        using var conn = _db.CreateConnection();
        var where = stateId.HasValue ? "AND c.StateId = @stateId" : "";
        var sql = $@"SELECT c.CityId, c.CityName, c.StateId, s.StateName
                     FROM Cities c JOIN States s ON c.StateId = s.StateId
                     WHERE c.IsActive = 1 {where} ORDER BY c.CityName";
        var data = await conn.QueryAsync<CityDto>(sql, new { stateId });
        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpGet("stations")]
    public async Task<IActionResult> GetStations([FromQuery] int? cityId)
    {
        using var conn = _db.CreateConnection();
        var where = cityId.HasValue ? "AND st.CityId = @cityId" : "";
        var sql = $@"SELECT st.StationId, st.StationName, st.CityId, c.CityName, c.StateId, s.StateName
                     FROM Stations st
                     JOIN Cities c ON st.CityId = c.CityId
                     JOIN States s ON c.StateId = s.StateId
                     WHERE st.IsActive = 1 {where} ORDER BY st.StationName";
        var data = await conn.QueryAsync<StationDto>(sql, new { cityId });
        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpPost("states")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateState([FromBody] CreateStateRequest request)
    {
        using var conn = _db.CreateConnection();
        var id = await conn.QueryFirstOrDefaultAsync<int>(
            "INSERT INTO States (StateName, StateCode) OUTPUT INSERTED.StateId VALUES (@StateName, @StateCode)", request);
        return Created($"/api/locations/states", ApiResponse<object>.Ok(new { StateId = id }, "State created."));
    }

    [HttpPost("cities")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCity([FromBody] CreateCityRequest request)
    {
        using var conn = _db.CreateConnection();
        var id = await conn.QueryFirstOrDefaultAsync<int>(
            "INSERT INTO Cities (CityName, StateId) OUTPUT INSERTED.CityId VALUES (@CityName, @StateId)", request);
        return Created($"/api/locations/cities", ApiResponse<object>.Ok(new { CityId = id }, "City created."));
    }

    [HttpPost("stations")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateStation([FromBody] CreateStationRequest request)
    {
        using var conn = _db.CreateConnection();
        var id = await conn.QueryFirstOrDefaultAsync<int>(
            "INSERT INTO Stations (StationName, CityId) OUTPUT INSERTED.StationId VALUES (@StationName, @CityId)", request);
        return Created($"/api/locations/stations", ApiResponse<object>.Ok(new { StationId = id }, "Station created."));
    }

    [HttpPut("states/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateState(int id, [FromBody] CreateStateRequest request)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE States SET StateName = @StateName, StateCode = @StateCode WHERE StateId = @id", new { request.StateName, request.StateCode, id });
        return rows > 0 ? Ok(ApiResponse.Ok("State updated.")) : NotFound(ApiResponse.Fail("State not found."));
    }

    [HttpDelete("states/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteState(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE States SET IsActive = 0 WHERE StateId = @id", new { id });
        return rows > 0 ? Ok(ApiResponse.Ok("State deleted.")) : NotFound(ApiResponse.Fail("State not found."));
    }

    [HttpPut("cities/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCity(int id, [FromBody] CreateCityRequest request)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE Cities SET CityName = @CityName WHERE CityId = @id", new { request.CityName, id });
        return rows > 0 ? Ok(ApiResponse.Ok("City updated.")) : NotFound(ApiResponse.Fail("City not found."));
    }

    [HttpDelete("cities/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCity(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE Cities SET IsActive = 0 WHERE CityId = @id", new { id });
        return rows > 0 ? Ok(ApiResponse.Ok("City deleted.")) : NotFound(ApiResponse.Fail("City not found."));
    }

    [HttpPut("stations/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStation(int id, [FromBody] CreateStationRequest request)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE Stations SET StationName = @StationName WHERE StationId = @id", new { request.StationName, id });
        return rows > 0 ? Ok(ApiResponse.Ok("Station updated.")) : NotFound(ApiResponse.Fail("Station not found."));
    }

    [HttpDelete("stations/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteStation(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE Stations SET IsActive = 0 WHERE StationId = @id", new { id });
        return rows > 0 ? Ok(ApiResponse.Ok("Station deleted.")) : NotFound(ApiResponse.Fail("Station not found."));
    }
}

public class CreateStateRequest
{
    public string StateName { get; set; } = string.Empty;
    public string? StateCode { get; set; }
}

public class CreateCityRequest
{
    public string CityName { get; set; } = string.Empty;
    public int StateId { get; set; }
}

public class CreateStationRequest
{
    public string StationName { get; set; } = string.Empty;
    public int CityId { get; set; }
}

public class StateDto
{
    public int StateId { get; set; }
    public string StateName { get; set; } = string.Empty;
    public string? StateCode { get; set; }
}

public class CityDto
{
    public int CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public int StateId { get; set; }
    public string StateName { get; set; } = string.Empty;
}

public class StationDto
{
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public int StateId { get; set; }
    public string StateName { get; set; } = string.Empty;
}
