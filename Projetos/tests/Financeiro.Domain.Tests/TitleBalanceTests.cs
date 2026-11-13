using Financeiro.Domain;

namespace Financeiro.Domain.Tests;

public class TitleBalanceTests
{
    private static readonly DateOnly AsOf = new(2026, 11, 12);

    [Fact]
    public void UntouchedTitleIsOpen()
    {
        var balance = TitleBalance.Calculate(100m, 0m, 0m, 0m, new DateOnly(2026, 12, 1), AsOf);
        Assert.Equal(100m, balance.Balance);
        Assert.Equal(0m, balance.SettledAmount);
        Assert.Equal(SettlementStatus.Open, balance.Status);
    }

    [Fact]
    public void PartialAllocationMarksTitlePartial()
    {
        var balance = TitleBalance.Calculate(100m, 0m, 40m, 0m, new DateOnly(2026, 12, 1), AsOf);
        Assert.Equal(60m, balance.Balance);
        Assert.Equal(40m, balance.SettledAmount);
        Assert.Equal(SettlementStatus.Partial, balance.Status);
    }

    [Fact]
    public void FullSettlementMarksTitlePaid()
    {
        var balance = TitleBalance.Calculate(100m, 0m, 60m, 40m, new DateOnly(2026, 12, 1), AsOf);
        Assert.Equal(0m, balance.Balance);
        Assert.Equal(SettlementStatus.Paid, balance.Status);
    }

    [Fact]
    public void ExcessPaymentSurfacesAsExcessCreditInsteadOfZero()
    {
        var balance = TitleBalance.Calculate(100m, 0m, 120m, 0m, new DateOnly(2026, 12, 1), AsOf);
        Assert.Equal(-20m, balance.Balance);
        Assert.Equal(20m, balance.ExcessCredit);
        Assert.Equal(SettlementStatus.Paid, balance.Status);
    }

    [Fact]
    public void AdjustmentsReduceTheDerivedBalance()
    {
        var balance = TitleBalance.Calculate(100m, -10m, 30m, 0m, new DateOnly(2026, 12, 1), AsOf);
        Assert.Equal(60m, balance.Balance);
        Assert.Equal(SettlementStatus.Partial, balance.Status);
    }

    [Fact]
    public void PastDueOpenTitleIsOverdue()
    {
        var balance = TitleBalance.Calculate(100m, 0m, 0m, 0m, new DateOnly(2026, 11, 11), AsOf);
        Assert.Equal(SettlementStatus.Overdue, balance.Status);
    }

    [Fact]
    public void PastDuePartialTitleIsOverdueWhileKeepingSettledAmount()
    {
        var balance = TitleBalance.Calculate(100m, 0m, 25m, 0m, new DateOnly(2026, 11, 11), AsOf);
        Assert.Equal(75m, balance.Balance);
        Assert.Equal(25m, balance.SettledAmount);
        Assert.Equal(SettlementStatus.Overdue, balance.Status);
    }

    [Fact]
    public void DueTodayIsNotOverdue()
    {
        var balance = TitleBalance.Calculate(100m, 0m, 0m, 0m, AsOf, AsOf);
        Assert.Equal(SettlementStatus.Open, balance.Status);
    }

    [Fact]
    public void UnknownDueDateIsNeverOverdueAndStaysUnknown()
    {
        var balance = TitleBalance.Calculate(100m, 0m, 0m, 0m, null, AsOf);
        Assert.Equal(SettlementStatus.Open, balance.Status);
        Assert.Equal(100m, balance.Balance);
    }
}
