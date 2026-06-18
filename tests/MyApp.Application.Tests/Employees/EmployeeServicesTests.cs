namespace MyApp.Application.Tests.Employees;
using FluentAssertions;
using Moq;
using MyApp.Application.Common.Constants;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Employees.DTOs;
using MyApp.Application.Employees.Services;
using MyApp.Domain.Entities;
using MyApp.Domain.Interfaces;

public sealed class EmployeeServicesTests
{
    private readonly Mock<IUnitOfWork>         _uow      = new();
    private readonly Mock<IEmployeeRepository> _repo     = new();
    private readonly Mock<IMapper>             _mapper   = new();
    private readonly Mock<ICurrentUser>        _current  = new();
    private readonly Guid                       _userId  = Guid.NewGuid();
    private readonly EmployeeServices           _sut;

    public EmployeeServicesTests()
    {
        _uow.Setup(u => u.Employees).Returns(_repo.Object);
        _uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _current.Setup(c => c.Id).Returns(_userId);
        _current.Setup(c => c.IsInRole(It.IsAny<string>())).Returns(false);

        _mapper.Setup(m => m.Map<EmployeeResponseDto>(It.IsAny<Employee>()))
               .Returns<Employee>(EmployeeResponseDto.FromEntity);

        _sut = new EmployeeServices(_uow.Object, _mapper.Object, _current.Object);
    }

    private Employee NovoEmployee(string userId, string? managerId = null) =>
        new("João Silva", "joao@test.com", userId, managerId);

    // ── Caso 1: GetByIdAsync ──────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_NaoEncontrado_RetornaNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
             .ReturnsAsync((Employee?)null);

        var result = await _sut.GetByIdAsync(99);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.NotFound");
    }

    [Fact]
    public async Task GetByIdAsync_CustomerVendoOutroEmployee_RetornaForbidden()
    {
        var employee = NovoEmployee(userId: Guid.NewGuid().ToString());
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(employee);
        // não é Admin nem Manager, e UserId != CurrentUserId

        var result = await _sut.GetByIdAsync(1);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.Forbidden");
    }

    [Fact]
    public async Task GetByIdAsync_AdminVendoQualquer_RetornaDto()
    {
        _current.Setup(c => c.IsInRole(AppRoles.Admin)).Returns(true);
        var employee = NovoEmployee(userId: Guid.NewGuid().ToString());
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(employee);

        var result = await _sut.GetByIdAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.FullName.Should().Be("João Silva");
    }

    [Fact]
    public async Task GetByIdAsync_DonoVendoProprio_RetornaDto()
    {
        var employee = NovoEmployee(userId: _userId.ToString());
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(employee);

        var result = await _sut.GetByIdAsync(1);

        result.IsSuccess.Should().BeTrue();
    }

    // ── Caso 2: DeactivateAsync ───────────────────────────────

    [Fact]
    public async Task DeactivateAsync_NaoAdmin_RetornaForbidden()
    {
        // guard EnsureRole(Admin) falha logo no início
        var result = await _sut.DeactivateAsync(1);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.Forbidden");
        _repo.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateAsync_AdminDesativandoProprio_RetornaConflict()
    {
        _current.Setup(c => c.IsInRole(AppRoles.Admin)).Returns(true);
        var employee = NovoEmployee(userId: _userId.ToString());
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(employee);

        var result = await _sut.DeactivateAsync(1);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.Conflict");
    }

    [Fact]
    public async Task DeactivateAsync_AdminDesativandoOutro_Sucesso()
    {
        _current.Setup(c => c.IsInRole(AppRoles.Admin)).Returns(true);
        var employee = NovoEmployee(userId: Guid.NewGuid().ToString());
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(employee);

        var result = await _sut.DeactivateAsync(1);

        result.IsSuccess.Should().BeTrue();
        employee.IsActive.Should().BeFalse();
        employee.UpdatedBy.Should().Be(_userId.ToString());
        _uow.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Caso 3: GetMyTeamAsync ────────────────────────────────

    [Fact]
    public async Task GetMyTeamAsync_Admin_RetornaTodos()
    {
        _current.Setup(c => c.IsInRole(AppRoles.Admin)).Returns(true);
        var all = new List<Employee> { NovoEmployee("a"), NovoEmployee("b") };
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(all);
        _mapper.Setup(m => m.Map<IEnumerable<EmployeeResponseDto>>(It.IsAny<object>()))
               .Returns(all.Select(EmployeeResponseDto.FromEntity));

        var result = await _sut.GetMyTeamAsync();

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.GetByManagerUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetMyTeamAsync_Manager_RetornaApenasSeuTime()
    {
        _current.Setup(c => c.IsInRole(AppRoles.Manager)).Returns(true);
        var team = new List<Employee> { NovoEmployee("x", _userId.ToString()) };
        _repo.Setup(r => r.GetByManagerUserIdAsync(_userId.ToString(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(team);
        _mapper.Setup(m => m.Map<IEnumerable<EmployeeResponseDto>>(It.IsAny<object>()))
               .Returns(team.Select(EmployeeResponseDto.FromEntity));

        var result = await _sut.GetMyTeamAsync();

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.GetByManagerUserIdAsync(_userId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
