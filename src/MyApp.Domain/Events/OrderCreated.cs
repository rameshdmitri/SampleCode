namespace MyApp.Domain.Events;
public sealed record OrderCreated(Guid OrderId, Guid CustomerId, decimal Total);
