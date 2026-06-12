using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Scalar.AspNetCore;

using System.Text;

using VSpark.Data;
using VSpark.Hubs;
using VSpark.Models.Config;

namespace VSpark;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        string dbSource = $"Data Source={Path.Combine(AppContext.BaseDirectory, "SparkData.db")}";

        builder.Services.AddDbContextFactory<SparkDbContext>(options => options.UseSqlite(dbSource));

        builder.Services.AddControllers();

        builder.Services.AddSignalR();

        builder.Services.AddOpenApi();

        // Source of secret must be able to be reconfigurated.
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var jwtSecret = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

        builder.Services.AddAuthentication().AddJwtBearer(options =>
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

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();

            app.MapScalarApiReference();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();

        app.UseStaticFiles();

        app.MapHub<MetricsHub>("/metricsHub");

        using (var scope = app.Services.CreateScope())
        {
            IDbContextFactory<SparkDbContext> dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SparkDbContext>>();

            using SparkDbContext dbContext = dbFactory.CreateDbContext();

            dbContext.Database.EnsureDeleted();

            dbContext.Database.EnsureCreated();
        }

        app.Run();
    }
}
