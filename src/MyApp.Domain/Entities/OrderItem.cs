namespace MyApp.Domain.Entities;
using MyApp.Domain.Common;
using MyApp.Domain.ValueObjects;

public sealed class OrderItem : BaseEntity
{
    public Guid   OrderId     { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int    Quantity    { get; private set; }
    public Money  UnitPrice   { get; private set; } = new(0);
    public Money  TotalPrice  => UnitPrice.Multiply(Quantity);

    private OrderItem() { }

    public OrderItem(Guid orderId, string productName, int quantity, Money unitPrice)
    {
        OrderId     = orderId;
        ProductName = productName;
        Quantity    = quantity;
        UnitPrice   = unitPrice;
    }
}
