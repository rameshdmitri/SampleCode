namespace MyApp.Domain.Tests;
using FluentAssertions;
using MyApp.Domain.Entities;
using MyApp.Domain.Enums;
using MyApp.Domain.Exceptions;
using MyApp.Domain.ValueObjects;

public sealed class OrderTests
{
    [Fact]
    public void Order_CriadaComItens_StatusPending()
    {
        var order = new Order(Guid.NewGuid(), [("Prod A", 2, new Money(50))]);
        order.Status.Should().Be(OrderStatus.Pending);
        order.Total.Amount.Should().Be(100m);
        order.Items.Should().HaveCount(1);
    }

    [Fact]
    public void Order_SemItens_LancaDomainException()
    {
        var act = () => new Order(Guid.NewGuid(), []);
        act.Should().Throw<DomainException>().WithMessage("*at least one item*");
    }

    [Fact]
    public void Order_Cancel_MudaStatusParaCancelled()
    {
        var order = new Order(Guid.NewGuid(), [("Prod", 1, new Money(10))]);
        order.Cancel();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Order_CancelTwice_LancaDomainException()
    {
        var order = new Order(Guid.NewGuid(), [("Prod", 1, new Money(10))]);
        order.Cancel();
        var act = () => order.Cancel();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Order_DomainEvent_DisparadoAoCriar()
    {
        var order = new Order(Guid.NewGuid(), [("Prod", 1, new Money(10))]);
        order.DomainEvents.Should().HaveCount(1);
    }
}
