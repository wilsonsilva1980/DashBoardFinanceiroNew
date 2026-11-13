using Financeiro.Domain;

namespace Financeiro.Domain.Tests;

public class RecurrenceCalendarTests
{
    [Fact]
    public void MonthlyOccurrencesAdvanceByOneMonthFromTheAnchor()
    {
        var occurrences = RecurrenceCalendar.Plan(new DateOnly(2026, 1, 15), 5, 1, new DateOnly(2026, 2, 1), new DateOnly(2026, 4, 30), 150m, "BRL");
        Assert.Equal(new[] { new DateOnly(2026, 2, 5), new DateOnly(2026, 3, 5), new DateOnly(2026, 4, 5) }, occurrences.Select(x => x.DueDate).ToArray());
    }

    [Fact]
    public void QuarterlyOccurrencesAdvanceByThreeMonths()
    {
        var occurrences = RecurrenceCalendar.Plan(new DateOnly(2026, 1, 10), 20, 3, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), 400m, "USD");
        Assert.Equal(new[] { new DateOnly(2026, 1, 20), new DateOnly(2026, 4, 20), new DateOnly(2026, 7, 20), new DateOnly(2026, 10, 20) }, occurrences.Select(x => x.DueDate).ToArray());
    }

    [Fact]
    public void NominalDayFallsBackToTheLastDayOfShorterMonths()
    {
        var occurrences = RecurrenceCalendar.Plan(new DateOnly(2026, 1, 31), 31, 1, new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31), 900m, "BRL");
        Assert.Equal(new[] { new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 31) }, occurrences.Select(x => x.DueDate).ToArray());
    }

    [Fact]
    public void OccurrencesCarryTheCycleAmountAndCurrencyOfTheirPriceWindow()
    {
        var occurrences = RecurrenceCalendar.Plan(new DateOnly(2026, 1, 31), 31, 1, new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31), 1250m, "USD");
        Assert.Equal(new[] { 1250m, 1250m, 1250m }, occurrences.Select(x => x.CycleAmount).ToArray());
        Assert.Equal(new[] { "USD", "USD", "USD" }, occurrences.Select(x => x.Currency).ToArray());
    }

    [Fact]
    public void FutureOccurrencesAfterAPriceChangeUseTheNewCycleAmount()
    {
        var beforePriceChange = RecurrenceCalendar.Plan(new DateOnly(2026, 1, 31), 31, 1, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), 100m, "BRL");
        var afterPriceChange = RecurrenceCalendar.Plan(new DateOnly(2026, 1, 31), 31, 1, new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31), 150m, "BRL");
        Assert.All(beforePriceChange, occurrence => Assert.Equal(100m, occurrence.CycleAmount));
        Assert.All(afterPriceChange, occurrence => Assert.Equal(150m, occurrence.CycleAmount));
    }

    [Fact]
    public void OccurrenceKeysScopeRecurrenceAndPeriod()
    {
        var recurrenceId = Guid.NewGuid();
        var first = RecurrenceCalendar.OccurrenceKey(recurrenceId, 2026, 5);
        var second = RecurrenceCalendar.OccurrenceKey(Guid.NewGuid(), 2026, 5);
        Assert.NotEqual(first, second);
        Assert.Equal($"{recurrenceId}:2026-05", first);
    }
}
