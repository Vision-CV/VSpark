using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Npgsql;

using Scalar.AspNetCore;

using System.Text;

using VSpark.AuthSchemes;
using VSpark.AuthSchemes.Configs;
using VSpark.Hubs;
using VSpark.Middleware;
using VSpark.Models.Config;
using VSpark.Orchestrator.Services.Rpcs;
using VSpark.Persistence;
using VSpark.Protos;
using VSpark.Services.Auth;
using VSpark.Services.Background;

namespace VSpark;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionSettings = builder.Configuration.GetSection("DbConnection");

        NpgsqlConnectionStringBuilder connectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = connectionSettings["Host"],
            Port = connectionSettings.GetValue<int>("Port"),
            Database = connectionSettings["Database"],
            Username = connectionSettings["Username"],
            Password = connectionSettings["Password"]
        };

        builder.Services.AddDbContextFactory<SparkDbContext>(options => options.UseNpgsql(connectionStringBuilder.ConnectionString));

        builder.Services.AddControllers();

        builder.Services.AddSignalR();

        builder.Services.AddOpenApi();

        // The source of the secret must be configurable.
        var authSettings = builder.Configuration.GetSection("AuthSettings");
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var jwtSecret = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

        builder.Services.AddAuthentication(options => options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme)
            .AddScheme<ApiKeySchemeOptions, ApiKeyHandler>("X-API", options => { })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "visdash",

                    ValidateAudience = true,
                    ValidAudience = "visdash_client",

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(jwtSecret),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),

                    NameClaimType = "username",
                    RoleClaimType = "role"
                };
            });

        builder.Services.Configure<JwtSettings>(jwtSettings);
        builder.Services.Configure<AuthSettings>(authSettings);

        builder.Services.AddSingleton<ITokenManager, TokenManager>();
        builder.Services.AddSingleton<ISessionManager, SessionManager>();
        builder.Services.AddSingleton<IJwtBlacklistRepository, JwtBlacklistRepository>();

        builder.Services.AddSingleton<IncidentsBridge>();

        builder.Services.AddScoped<IAuthService, AuthService>();

        builder.Services.AddHostedService<SessionsCleanupWorker>();
        builder.Services.AddHostedService<JwtBlacklistCleanupWorker>();

        // TODO: Configurable source required.
        builder.Services.AddGrpcClient<IncidentService.IncidentServiceClient>(options => options.Address = new Uri(""));

        builder.Logging.AddConsole();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();

            app.MapScalarApiReference();
        }

        app.UseCors();

        app.UseHttpsRedirection();

        app.UseAuthentication();

        app.UseAuthorization();

        app.UseMiddleware<JwtBlacklist>();

        app.MapControllers();

        app.UseStaticFiles();

        app.MapHub<MetricsHub>("/metricsHub");

        app.Run();
    }
}
