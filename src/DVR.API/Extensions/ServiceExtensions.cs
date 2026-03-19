using System.Text;
using DVR.Application.Interfaces;
using DVR.Infrastructure.Data;
using DVR.Infrastructure.Repositories;
using DVR.Infrastructure.Services;
using FluentValidation;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace DVR.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DVR.Application.Features.Authentication.Commands.LoginCommand).Assembly);
        });

        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(DVR.Application.Features.Authentication.Commands.LoginCommand).Assembly);

        // Infrastructure
        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // HttpClient for FCM push notifications
        services.AddHttpClient();

        // Notification services - use decorator pattern for real-time SignalR
        services.AddScoped<NotificationService>();
        services.AddScoped<INotificationService>(sp =>
        {
            var inner = sp.GetRequiredService<NotificationService>();
            var hubContext = sp.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<DVR.API.Hubs.NotificationHub>>();
            return new DVR.API.Services.RealtimeNotificationService(inner, hubContext);
        });

        // Repositories
        services.AddScoped<SchoolRepository>();
        services.AddScoped<SalesmanRepository>();
        services.AddScoped<UserRepository>();

        // HttpContextAccessor for CurrentUserService
        services.AddHttpContextAccessor();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret not configured.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // Support SignalR JWT from query string
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var accessToken = ctx.Request.Query["access_token"];
                        var path = ctx.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            ctx.Token = accessToken;
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("AdminOrManager", policy => policy.RequireRole("Admin", "Manager"));
            options.AddPolicy("AllRoles", policy => policy.RequireRole("Admin", "Manager", "Salesman"));
        });

        return services;
    }

    public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection")));

        services.AddHangfireServer();

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy("DVRCorsPolicy", policy =>
            {
                policy.WithOrigins(origins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
