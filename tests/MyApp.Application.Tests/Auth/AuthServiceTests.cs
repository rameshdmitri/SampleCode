namespace MyApp.Application.Tests.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using MyApp.Application.Auth.DTOs;
using MyApp.Application.Auth.Errors;
using MyApp.Application.Common.Constants;
using MyApp.Application.Common.Interfaces;
using MyApp.Infrastructure.Identity;

public sealed class AuthServiceTests
{
    private readonly Mock<UserManager<AppUser>>            _userMgr;
    private readonly Mock<SignInManager<AppUser>>          _signInMgr;
    private readonly Mock<RoleManager<IdentityRole<Guid>>> _roleMgr;
    private readonly Mock<IJwtTokenService>                _jwtSvc  = new();
    private readonly AuthService                           _sut;

    public AuthServiceTests()
    {
        var store = new Mock<IUserStore<AppUser>>();
        _userMgr  = new Mock<UserManager<AppUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var ctxAcc  = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFac = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
        _signInMgr  = new Mock<SignInManager<AppUser>>(
            _userMgr.Object, ctxAcc.Object, claimsFac.Object, null!, null!, null!, null!);

        var roleStore = new Mock<IRoleStore<IdentityRole<Guid>>>();
        _roleMgr = new Mock<RoleManager<IdentityRole<Guid>>>(
            roleStore.Object, null!, null!, null!, null!);

        _jwtSvc.Setup(j => j.GenerateAsync(
            It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync("fake.jwt.token");

        _sut = new AuthService(_userMgr.Object, _signInMgr.Object, _roleMgr.Object, _jwtSvc.Object);
    }

    [Fact]
    public async Task LoginAsync_CredenciaisValidas_RetornaToken()
    {
        var user = new AppUser { Id = Guid.NewGuid(), Email = "u@test.com", FullName = "User", IsActive = true };

        _userMgr.Setup(m => m.FindByEmailAsync("u@test.com")).ReturnsAsync(user);
        _signInMgr.Setup(m => m.CheckPasswordSignInAsync(user, "Pass@1", true))
                  .ReturnsAsync(SignInResult.Success);
        _userMgr.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([AppRoles.Customer]);

        var result = await _sut.LoginAsync(new LoginRequest("u@test.com", "Pass@1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("fake.jwt.token");
        result.Value.Roles.Should().Contain(AppRoles.Customer);
    }

    [Fact]
    public async Task LoginAsync_UsuarioNaoExiste_RetornaInvalidCredentials()
    {
        _userMgr.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);

        var result = await _sut.LoginAsync(new LoginRequest("nao@existe.com", "Pass@1"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.InvalidCredentials);
    }

    [Fact]
    public async Task RegisterAsync_EmailJaUsado_RetornaEmailAlreadyInUse()
    {
        var user = new AppUser { Email = "existing@test.com" };
        _userMgr.Setup(m => m.FindByEmailAsync("existing@test.com")).ReturnsAsync(user);

        var result = await _sut.RegisterAsync(
            new RegisterRequest("Nome", "existing@test.com", "Pass@1", AppRoles.Customer));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.EmailAlreadyInUse);
    }
}
