namespace MyApp.Domain.Entities;
using MyApp.Domain.Common;
using MyApp.Domain.Enums;
using MyApp.Domain.Events;
using MyApp.Domain.Exceptions;
using MyApp.Domain.ValueObjects;

public sealed class Order : BaseEntity, IAggregateRoot
{
    private readonly List<OrderItem> _items = [];

    public Guid                    CustomerId { get; private set; }
    public OrderStatus             Status     { get; private set; } = OrderStatus.Pending;
    public DateTime                CreatedAt  { get; private set; } = DateTime.UtcNow;
    public DateTime?               UpdatedAt  { get; private set; }
    public IReadOnlyList<OrderItem> Items     => _items.AsReadOnly();
    public Money Total => _items
        .Aggregate(new Money(0), (acc, i) => acc.Add(i.TotalPrice));

    private Order() { }

    public Order(Guid customerId, IEnumerable<(string name, int qty, Money price)> items)
    {
        CustomerId = customerId;
        foreach (var (name, qty, price) in items)
            _items.Add(new OrderItem(Id, name, qty, price));

        if (!_items.Any())
            throw new DomainException("An order must have at least one item.");

        RaiseDomainEvent(new OrderCreated(Id, CustomerId, Total.Amount));
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException($"Cannot confirm an order with status {Status}.");
        Status    = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            throw new DomainException("Order is already cancelled.");
        if (Status == OrderStatus.Delivered)
            throw new DomainException("Cannot cancel a delivered order.");
        Status    = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new OrderCancelled(Id, CustomerId));
    }
}
