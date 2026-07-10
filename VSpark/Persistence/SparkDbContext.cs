using Microsoft.EntityFrameworkCore;

using VSpark.Models.Data;
using VSpark.Models.Auth;
using VSpark.Models.Auth.Tokens;

namespace VSpark.Persistence;

public class SparkDbContext(DbContextOptions<SparkDbContext> options) : DbContext(options)
{
    public DbSet<IncidentData> Incidents { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }
}
