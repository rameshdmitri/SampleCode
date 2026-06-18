namespace MyApp.Domain.Events;
public sealed record OrderCancelled(Guid OrderId, Guid CustomerId);
