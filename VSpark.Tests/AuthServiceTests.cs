using Microsoft.Extensions.Logging.Abstractions;

using System.Net;

using VSpark.Models.Auth;
using VSpark.Models.Auth.Tokens;
using VSpark.Persistence;
using VSpark.Services.Auth;
using VSpark.Tests.Tools.Persistence;
using VSpark.Tests.Tools.Settings;

using static BCrypt.Net.BCrypt;

namespace VSpark.Tests;

public class AuthServiceTests
{
    // TODO: Add integrational tests of the jwt blacklist correct work.

    // Attention! Instances of this field have a per-test lifecycle. Do not touch em.
    private MemDbContextFactory _dbFactory;
    private SparkDbContext _dbContext;

    private JwtBlacklistRepository _jwtBlacklist;
    private TokenManager _tokenManager;
    private AuthService _authService;

    [SetUp]
    public void Setup()
    {
        _dbFactory = new MemDbContextFactory(Guid.NewGuid().ToString());
        _dbContext = _dbFactory.CreateDbContext();

        _jwtBlacklist = new JwtBlacklistRepository(_dbFactory);
        //_tokenManager = new TokenManager(ConfigsHelper.JwtSettings, _dbFactory, _jwtBlacklist);
        //_authService = new AuthService(ConfigsHelper.AuthOptions, _dbFactory, _tokenManager, new NullLogger<AuthService>());
    }

    [TearDown]
    public void TearDown()
    {
        _dbFactory.Dispose();
        _dbContext.Dispose();
    }

    // TODO: Old tests have been evaporated by refactor. Next commit will return tests.
    // Previous auth system version tests you can find in previous versions of the repo.

    private void VerifySessionResponseIntegrity(AuthResponse response)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Body, Is.Not.Null.Or.Empty);

        Assert.That(response.Cookies, Is.Not.Null);

        Assert.That(response.Cookies!.ContainsKey("Session-Refresh-Token"), Is.True);
        Assert.That(response.Cookies["Session-Refresh-Token"], Is.Not.Null.Or.Empty);
    }
}
