using Microsoft.EntityFrameworkCore;

using Moq;

using VSpark.Data;

namespace VSpark.Tests.Tools;

public static class DbTools
{
    public static IDbContextFactory<SparkDbContext> GetFactory()
    {
        DbContextOptions<SparkDbContext> dbContextOptions = new DbContextOptionsBuilder<SparkDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

        Mock<IDbContextFactory<SparkDbContext>> mockedFactory = new Mock<IDbContextFactory<SparkDbContext>>();

        mockedFactory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SparkDbContext(dbContextOptions));

        return mockedFactory.Object;
    }
}
