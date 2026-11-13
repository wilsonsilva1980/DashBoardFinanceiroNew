using Financeiro.Domain;

namespace Financeiro.Domain.Tests;

public class CurrencyAmountTests
{
    [Fact]
    public void ExcessCreditIsPreservedRatherThanTruncated()
    {
        var balance = new CurrencyAmount(100m, "BRL").Add(new CurrencyAmount(-120m, "BRL"));
        Assert.Equal(-20m, balance.Amount);
    }

    [Fact]
    public void MixedCurrenciesAndExcessPrecisionAreRejected()
    {
        Assert.Throws<InvalidOperationException>(() => new CurrencyAmount(1m, "BRL").Add(new CurrencyAmount(1m, "USD")));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CurrencyAmount(1.001m, "BRL"));
    }
}
