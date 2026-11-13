using Financeiro.Application.Catalogs;

namespace Financeiro.Application.Tests;

public class CatalogValidationTests
{
    [Fact]
    public void MoneyPrecisionRejectsRoundingAndNegativeLimits()
    {
        CatalogValidation.Money(99999999999999999.99m);
        CatalogValidation.Money(null);
        Assert.Throws<CatalogValidationException>(() => CatalogValidation.Money(0.001m));
        Assert.Throws<CatalogValidationException>(() => CatalogValidation.Money(-1m));
        Assert.Throws<CatalogValidationException>(() => CatalogValidation.Money(decimal.MaxValue));
    }
    [Fact]
    public void UnknownDayRemainsValidButImpossibleDaysAreRejected()
    {
        CatalogValidation.Day(null);
        CatalogValidation.Day(31);
        Assert.Throws<CatalogValidationException>(() => CatalogValidation.Day(0));
        Assert.Throws<CatalogValidationException>(() => CatalogValidation.Day(32));
    }
    [Fact]
    public void PaginationCannotOverflowOrRequestUnboundedPages()
    {
        Assert.Equal(25, new PageRequest(2, 25).Offset());
        Assert.Throws<CatalogValidationException>(() => new PageRequest(int.MaxValue, 100).Offset());
        Assert.Throws<CatalogValidationException>(() => new PageRequest(0, 25).Offset());
        Assert.Throws<CatalogValidationException>(() => new PageRequest(1, 101).Offset());
    }

    [Fact]
    public void CurrencyValidationAcceptsSupportedIsoCodesAndRejectsOthers()
    {
        CatalogValidation.Currency("USD");
        CatalogValidation.Currency("BRL");
        Assert.Throws<CatalogValidationException>(() => CatalogValidation.Currency("US"));
        Assert.Throws<CatalogValidationException>(() => CatalogValidation.Currency("usd"));
        Assert.Throws<CatalogValidationException>(() => CatalogValidation.Currency("XYZ"));
        Assert.Throws<CatalogValidationException>(() => CatalogValidation.Currency(null));
    }
}
