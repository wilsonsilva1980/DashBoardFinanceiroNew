namespace Financeiro.Domain;

/// <summary>A two-decimal monetary value; negative balances remain visible.</summary>
public readonly record struct CurrencyAmount
{
    public decimal Amount { get; }
    public string Currency { get; }

    public CurrencyAmount(decimal amount, string currency)
    {
        if (decimal.Round(amount, 2) != amount || Math.Abs(amount) > 99999999999999999.99m)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount exceeds decimal(19,2) precision.");
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        if (currency.Length != 3 || currency.Any(c => c is < 'A' or > 'Z'))
            throw new ArgumentException("Currency must be an uppercase ISO code.", nameof(currency));
        Amount = amount;
        Currency = currency;
    }

    public CurrencyAmount Add(CurrencyAmount other)
    {
        if (Currency != other.Currency) throw new InvalidOperationException("Explicit conversion is required before combining currencies.");
        return new CurrencyAmount(checked(Amount + other.Amount), Currency);
    }
}
