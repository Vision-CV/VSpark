using System.IdentityModel.Tokens.Jwt;

using VSpark.Models.Auth;
using VSpark.Models.Auth.Tokens;
using VSpark.Persistence;
using VSpark.Services.Auth;
using VSpark.Tests.Tools.Persistence;
using VSpark.Tests.Tools.Settings;
using VSpark.Tests.Tools.Utils;

namespace VSpark.Tests;

public class TokenManagerTests
{
    // Attention! Instances of this field have a per-test lifecycle. Do not touch em.
    private MemDbContextFactory _dbFactory;
    private SparkDbContext _dbContext;

    private JwtBlacklistRepository _jwtBlacklist;
    private TokenManager _tokenManager;

    [SetUp]
    public void Setup()
    {
        _dbFactory = new MemDbContextFactory(Guid.NewGuid().ToString());
        _dbContext = _dbFactory.CreateDbContext();

        _jwtBlacklist = new JwtBlacklistRepository(_dbFactory);
        //_tokenManager = new TokenManager(ConfigsHelper.JwtSettings, _dbFactory, _jwtBlacklist);
    }

    [TearDown]
    public void TearDown()
    {
        _dbFactory.Dispose();
        _dbContext.Dispose();
    }
}
