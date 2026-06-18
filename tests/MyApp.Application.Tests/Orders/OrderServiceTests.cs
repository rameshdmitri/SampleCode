namespace MyApp.Application.Tests.Orders;
using FluentAssertions;
using Moq;
using MyApp.Application.Auth.Errors;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Orders.DTOs;
using MyApp.Application.Orders.Errors;
using MyApp.Application.Orders.Services;
using MyApp.Application.Orders.Validators;
using MyApp.Application.Tests.Common;
using MyApp.Domain.Entities;
using MyApp.Domain.Interfaces;
using MyApp.Domain.ValueObjects;

public sealed class OrderServiceTests : TestBase
{
    private readonly Mock<IOrderRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork>      _uowMock  = new();
    private readonly OrderService           _sut;

    public OrderServiceTests()
    {
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _sut = new OrderService(_repoMock.Object, _uowMock.Object,
                                CurrentUserMock.Object, new CreateOrderValidator());
    }

    // ── GetById ───────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_OrderExiste_RetornaDto()
    {
        var userId = CurrentUserMock.Object.Id;
        var order  = CriarOrder(userId);

        _repoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);

        var result = await _sut.GetByIdAsync(order.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerId.Should().Be(userId);
    }

    [Fact]
    public async Task GetByIdAsync_OrderNaoEncontrada_RetornaNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Order?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_CustomerAcessandoOrderAieia_RetornaForbidden()
    {
        var outroUserId = Guid.NewGuid();
        var order       = CriarOrder(outroUserId);

        _repoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);

        CurrentUserMock.Setup(u => u.IsOwnerOrAdmin(outroUserId)).Returns(false);

        var result = await _sut.GetByIdAsync(order.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.Forbidden);
    }

    // ── Create ────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_DadosValidos_RetornaIdEUsaCurrentUserId()
    {
        var userId  = CurrentUserMock.Object.Id;
        Order? saved = null;

        _repoMock.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
                 .Callback<Order, CancellationToken>((o, _) => saved = o)
                 .Returns(Task.CompletedTask);

        var request = new CreateOrderRequest([new("Produto A", 2, 50m)]);
        var result  = await _sut.CreateAsync(request);

        result.IsSuccess.Should().BeTrue();
        saved!.CustomerId.Should().Be(userId);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ItensFaltando_RetornaErro()
    {
        var request = new CreateOrderRequest([]);
        var result  = await _sut.CreateAsync(request);

        result.IsFailure.Should().BeTrue();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Cancel ────────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_AdminCancelaPedidoQualquer_Sucesso()
    {
        SetupAsAdmin();
        var order = CriarOrder(Guid.NewGuid());

        _repoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);

        var result = await _sut.CancelAsync(order.Id);

        result.IsSuccess.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_CustomerCancelaPedidoAlheio_RetornaForbidden()
    {
        var outroUserId = Guid.NewGuid();
        var order       = CriarOrder(outroUserId);

        _repoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);

        CurrentUserMock.Setup(u => u.IsOwnerOrAdmin(outroUserId)).Returns(false);

        var result = await _sut.CancelAsync(order.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.Forbidden);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Helper ────────────────────────────────────────────────

    private static Order CriarOrder(Guid customerId) =>
        new(customerId, [("Produto X", 1, new Money(100m))]);
}
