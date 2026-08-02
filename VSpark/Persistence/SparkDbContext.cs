using Microsoft.EntityFrameworkCore;

using VSpark.Models.Data;
using VSpark.Models.Auth;
using VSpark.Models.Auth.Tokens;
using VSpark.Models.Auth.Sessions;

namespace VSpark.Persistence;

public class SparkDbContext(DbContextOptions<SparkDbContext> options) : DbContext(options)
{
    public DbSet<IncidentData> Incidents { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<AuthSession> Sessions { get; set; }

    public DbSet<BlacklistedJwtToken> JwtBlacklist { get; set; }
}
