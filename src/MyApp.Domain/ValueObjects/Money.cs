namespace MyApp.Domain.ValueObjects;
using MyApp.Domain.Common;

public sealed class Money : ValueObject
{
    public decimal Amount   { get; }
    public string  Currency { get; }

    private Money() { Amount = 0; Currency = "BRL"; }

    public Money(decimal amount, string currency = "BRL")
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative.");
        Amount   = amount;
        Currency = currency.ToUpper();
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies.");
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int quantity) => new(Amount * quantity, Currency);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}
