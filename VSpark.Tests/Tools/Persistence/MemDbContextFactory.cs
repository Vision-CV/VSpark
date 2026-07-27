using Microsoft.EntityFrameworkCore;

using VSpark.Persistence;

namespace VSpark.Tests.Tools.Persistence;

internal class MemDbContextFactory(string dbName) : IDbContextFactory<SparkDbContext>, IDisposable
{
    public SparkDbContext CreateDbContext()
    {
        DbContextOptions<SparkDbContext> sparkContextOptions = new DbContextOptionsBuilder<SparkDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new SparkDbContext(sparkContextOptions);
    }

    public Task<SparkDbContext> CreateDbContextAsync(CancellationToken cancellationToken) => Task.FromResult(CreateDbContext());

    public void Dispose()
    {
        using SparkDbContext dbContext = CreateDbContext();

        dbContext.Database.EnsureDeleted();
    }
}
