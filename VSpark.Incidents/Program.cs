using VSpark.Incidents.Rpcs;

namespace VSpark.Incidents
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContextFactory<>

            builder.Services.AddGrpc();

            var app = builder.Build();

            app.MapGrpcService<IncidentsGrpc>();

            app.Run();
        }
    }
}
