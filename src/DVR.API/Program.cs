using DbUp;
using DVR.API.Extensions;
using DVR.API.Middleware;
using Hangfire;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/dvr-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opt.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Application services
builder.Services.AddApplicationServices(builder.Configuration);

// JWT
builder.Services.AddJwtAuthentication(builder.Configuration);

// Hangfire
builder.Services.AddHangfireServices(builder.Configuration);

// CORS
builder.Services.AddCorsPolicy(builder.Configuration);

// Swagger
builder.Services.AddSwaggerDocumentation();

// SignalR
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// DbUp migrations
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
EnsureDatabase.For.SqlDatabase(connectionString);
var upgrader = DeployChanges.To
    .SqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(
        typeof(DVR.Infrastructure.Data.SqlConnectionFactory).Assembly,
        s => s.StartsWith("DVR.Infrastructure.Migrations.Scripts."))
    .WithVariablesDisabled()
    .WithTransactionPerScript()
    .LogToConsole()
    .Build();

if (upgrader.IsUpgradeRequired())
{
    var result = upgrader.PerformUpgrade();
    if (!result.Successful)
    {
        Log.Fatal(result.Error, "Database migration failed");
        return;
    }
}

// Middleware pipeline
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseSwaggerDocumentation();

app.UseHttpsRedirection();
app.UseCors("DVRCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire");

app.MapControllers();
app.MapHub<DVR.API.Hubs.NotificationHub>("/hubs/notifications");

// Background jobs
RecurringJob.AddOrUpdate("daily-attendance-check",
    () => Console.WriteLine("Daily attendance check running"),
    Cron.Daily(22, 0));

app.Run();
