using Microsoft.Extensions.Logging.Abstractions;

using System.Net;

using VSpark.Models.Auth;
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

    private TokenManager _tokenManager;
    private AuthService _authService;

    [SetUp]
    public void Setup()
    {
        _dbFactory = new MemDbContextFactory(Guid.NewGuid().ToString());
        _dbContext = _dbFactory.CreateDbContext();

        _tokenManager = new TokenManager(ConfigsHelper.JwtSettings, _dbFactory);
        _authService = new AuthService(ConfigsHelper.AuthOptions, _dbFactory, _tokenManager, new NullLogger<AuthService>());
    }

    [TearDown]
    public void TearDown()
    {
        _dbFactory.Dispose();
        _dbContext.Dispose();
    }

    [TestCase("Michael", "Anderson", "mikeuser", "bestpassever")]
    [TestCase("Sarah", "Mitchell", "sarahdev", "wo2wpas22s")]
    [TestCase("Daniel", "Thompson", "danieladmin", "greatestpass")]
    [TestCase("Emma", "Wilson", "emmaoperator", "excit_ing-pass")]
    [TestCase("Robert", "Johnson", "robservice", "#(#$(@)(#@()#*THISMYPASS")]
    [TestCase("Olivia", "Brown", "oliviauser", "_____000__--_-_")]
    public async Task TryRegisterAsync_RegistersSuccessfully_NewUserAndSessionAreExistsInDatabase(string name, string surname, string username, string password)
    {
        RegRequest regRequest = new RegRequest { Username = username, Name = name, Surname = surname, Password = password };

        AuthResponse response = await _authService.TryRegisterAsync(regRequest);

        Assert.Multiple(() => VerifySessionResponseIntegrity(response));

        Assert.That(_dbContext.Users.Count(x => x.Username == regRequest.Username), Is.EqualTo(1), "Looks like we've registered two equal users.");

        User registeredUser = _dbContext.Users.First(x => x.Username == regRequest.Username);

        Assert.Multiple(() =>
        {
            Assert.That(registeredUser.Username, Is.EqualTo(regRequest.Username));
            Assert.That(registeredUser.FirstName, Is.EqualTo(regRequest.Name));
            Assert.That(registeredUser.SecondName, Is.EqualTo(regRequest.Surname));
            Assert.That(registeredUser.Role, Is.EqualTo(ConfigsHelper.AuthOptions.Value.DefaultRole));

            Assert.That(registeredUser.PasswordHash, Is.Not.Null);
            Assert.That(Verify(regRequest.Password, registeredUser.PasswordHash), Is.True);
        });
    }

    [TestCase("Michael", "Anderson", "mikeuser", "bestpassever")]
    [TestCase("Sarah", "Mitchell", "sarahdev", "wo2wpas22s")]
    [TestCase("Daniel", "Thompson", "danieladmin", "greatestpass")]
    [TestCase("Emma", "Wilson", "emmaoperator", "excit_ing-pass")]
    [TestCase("Robert", "Johnson", "robservice", "#(#$(@)(#@()#*THISMYPASS")]
    [TestCase("Olivia", "Brown", "oliviauser", "_____000__--_-_")]
    public async Task TryLoginAsync_LoginSuccessful_NewRefreshSavedInDatabase(string name, string surname, string username, string password)
    {
        RegRequest regRequest = new RegRequest { Username = username, Name = name, Surname = surname, Password = password };

        AuthResponse regResponse = await _authService.TryRegisterAsync(regRequest);

        Assert.Multiple(() => VerifySessionResponseIntegrity(regResponse));

        AuthRequest authRequest = new() { Username = username, Password = password };

        AuthResponse loginResponse = await _authService.TryLoginAsync(authRequest);

        Assert.Multiple(() => VerifySessionResponseIntegrity(loginResponse));

        Assert.That(_dbContext.RefreshTokens.Any(x => x.Token == loginResponse!.Cookies!["Session-Refresh-Token"]), Is.True);
    }

    [TestCase("Michael", "Anderson", "mikeuser", "bestpassever")]
    [TestCase("Sarah", "Mitchell", "sarahdev", "wo2wpas22s")]
    [TestCase("Daniel", "Thompson", "danieladmin", "greatestpass")]
    [TestCase("Emma", "Wilson", "emmaoperator", "excit_ing-pass")]
    [TestCase("Robert", "Johnson", "robservice", "#(#$(@)(#@()#*THISMYPASS")]
    [TestCase("Olivia", "Brown", "oliviauser", "_____000__--_-_")]
    public async Task TryRenewSessionAsync_RenewSuccessful_NewTokenExistsInDatabase_OldTokenIsGone(string name, string surname, string username, string password)
    {
        RegRequest regRequest = new RegRequest { Username = username, Name = name, Surname = surname, Password = password };

        AuthResponse regResponse = await _authService.TryRegisterAsync(regRequest);

        Assert.Multiple(() => VerifySessionResponseIntegrity(regResponse));

        string regRefreshToken = regResponse!.Cookies!["Session-Refresh-Token"];

        AuthResponse renewResponse = await _authService.TryRenewSessionAsync(regRefreshToken);

        Assert.Multiple(() => VerifySessionResponseIntegrity(renewResponse));

        string renewRefreshToken = renewResponse!.Cookies!["Session-Refresh-Token"];

        Assert.That(renewResponse!.Cookies["Session-Refresh-Token"], Is.Not.EqualTo(regRefreshToken));

        Assert.Multiple(() =>
        {
            Assert.That(_dbContext.RefreshTokens.Any(x => x.Token == renewRefreshToken), Is.True);
            Assert.That(_dbContext.RefreshTokens.Any(x => x.Token == regRefreshToken), Is.False);
        });
    }

    [TestCase("Michael", "Anderson", "mikeuser", "bestpassever")]
    [TestCase("Sarah", "Mitchell", "sarahdev", "wo2wpas22s")]
    [TestCase("Daniel", "Thompson", "danieladmin", "greatestpass")]
    [TestCase("Emma", "Wilson", "emmaoperator", "excit_ing-pass")]
    [TestCase("Robert", "Johnson", "robservice", "#(#$(@)(#@()#*THISMYPASS")]
    [TestCase("Olivia", "Brown", "oliviauser", "_____000__--_-_")]
    public async Task TryLogoutAsync_LogoutSuccessful_OldTokenRemoved(string name, string surname, string username, string password)
    {
        RegRequest regRequest = new RegRequest { Username = username, Name = name, Surname = surname, Password = password };

        AuthResponse regResponse = await _authService.TryRegisterAsync(regRequest);

        Assert.Multiple(() => VerifySessionResponseIntegrity(regResponse));

        string regRefreshToken = regResponse!.Cookies!["Session-Refresh-Token"];

        AuthResponse logoutResponse = await _authService.TryLogoutAsync(regRefreshToken);

        Assert.That(logoutResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        Assert.That(_dbContext.RefreshTokens.Any(x => x.Token == regRefreshToken), Is.False);
    }

    [TestCase("Michael", "Anderson", "mikeuser", "bestpassever")]
    [TestCase("Sarah", "Mitchell", "sarahdev", "wo2wpas22s")]
    [TestCase("Daniel", "Thompson", "danieladmin", "greatestpass")]
    [TestCase("Emma", "Wilson", "emmaoperator", "excit_ing-pass")]
    [TestCase("Robert", "Johnson", "robservice", "#(#$(@)(#@()#*THISMYPASS")]
    [TestCase("Olivia", "Brown", "oliviauser", "_____000__--_-_")]
    public async Task TryChangePasswordAsync_ChangedSuccessful_ChangedInDatabase_AllTokensEvaporated(string name, string surname, string username, string password)
    {
        RegRequest regRequest = new RegRequest { Username = username, Name = name, Surname = surname, Password = password };

        AuthResponse regResponse = await _authService.TryRegisterAsync(regRequest);

        Assert.Multiple(() => VerifySessionResponseIntegrity(regResponse));

        string regRefreshToken = regResponse!.Cookies!["Session-Refresh-Token"];

        AuthRequest loginRequest = new AuthRequest() { Username = username, Password = password };

        AuthResponse loginResponse1 = await _authService.TryLoginAsync(loginRequest);
        AuthResponse loginResponse2 = await _authService.TryLoginAsync(loginRequest);

        Assert.Multiple(() => VerifySessionResponseIntegrity(loginResponse1));
        Assert.Multiple(() => VerifySessionResponseIntegrity(loginResponse1));

        List<string> tokensToEvaporate = new()
        {
            loginResponse1!.Cookies!["Session-Refresh-Token"],
            loginResponse2!.Cookies!["Session-Refresh-Token"]
        };

        AuthRequest changeRequest = new AuthRequest { Username = username, Password = password, NewPassword = Guid.NewGuid().ToString() };

        AuthResponse changePasswordResponse = await _authService.TryChangePasswordAsync(changeRequest, regRefreshToken);

        Assert.That(changePasswordResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        User? ourUser = _dbContext.Users.FirstOrDefault(x => x.Username == username);

        Assert.That(ourUser, Is.Not.Null, "Registration method wasn't saved a new user to the database.");

        Assert.Multiple(() =>
        {
            Assert.That(_dbContext.RefreshTokens.Any(x => x.Token == regRefreshToken), Is.True);

            foreach (string token in tokensToEvaporate)
                Assert.That(_dbContext.RefreshTokens.Any(x => x.Token == token), Is.False);
        });
    }

    private void VerifySessionResponseIntegrity(AuthResponse response)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Body, Is.Not.Null.Or.Empty);

        Assert.That(response.Cookies, Is.Not.Null);

        Assert.That(response.Cookies!.ContainsKey("Session-Refresh-Token"), Is.True);
        Assert.That(response.Cookies["Session-Refresh-Token"], Is.Not.Null.Or.Empty);
    }
}
