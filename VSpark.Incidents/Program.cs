using Microsoft.EntityFrameworkCore;

using Npgsql;

using VSpark.Incidents.Models.Configs;
using VSpark.Incidents.Persistence;
using VSpark.Incidents.Rpcs;

namespace VSpark.Incidents
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionSettings = builder.Configuration.GetSection("DbConnection");
            var artifactsConfig = builder.Configuration.GetSection("ArtifactsConfig");

            NpgsqlConnectionStringBuilder connectionStringBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = connectionSettings["Host"],
                Port = connectionSettings.GetValue<int>("Port"),
                Database = connectionSettings["Database"],
                Username = connectionSettings["Username"],
                Password = connectionSettings["Password"]
            };

            builder.Services.AddDbContextFactory<EventDbContext>(options => options.UseNpgsql(connectionStringBuilder.ConnectionString));

            builder.Services.AddGrpc();

            builder.Services.Configure<ArtifactsConfig>(artifactsConfig);

            var app = builder.Build();

            app.MapGrpcService<IncidentsGrpc>();

            app.Run();
        }
    }
}
