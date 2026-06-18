namespace MyApp.Application.Orders.Errors;
using MyApp.Application.Common.Models;

public static class OrderErrors
{
    public static readonly Error NotFound         = new("Order.NotFound",     "Order not found.");
    public static readonly Error AlreadyCancelled = new("Order.Conflict",     "Order is already cancelled.");
    public static readonly Error EmptyItems       = new("Order.Validation",   "Order must have at least one item.");
    public static readonly Error CannotCancel     = new("Order.Conflict",     "Order cannot be cancelled in its current status.");
}
