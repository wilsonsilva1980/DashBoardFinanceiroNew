namespace Financeiro.Domain;

/// <summary>A generated recurrence occurrence: the month it belongs to, its resolved due date, and the price it was generated with.</summary>
public sealed record RecurrenceOccurrence(int Index, DateOnly PeriodStart, DateOnly DueDate, decimal CycleAmount, string Currency);

/// <summary>Calendar math for recurrence occurrences: monthly, quarterly, and annual cycles from an anchor date with nominal-day handling.</summary>
public static class RecurrenceCalendar
{
    public static int LastDayOfMonth(int year, int month) => DateTime.DaysInMonth(year, month);

    public static DateOnly AddMonths(DateOnly date, int months)
    {
        var totalMonths = (date.Year * 12) + (date.Month - 1) + months;
        var year = totalMonths / 12;
        var month = (totalMonths % 12) + 1;
        return new DateOnly(year, month, Math.Clamp(date.Day, 1, LastDayOfMonth(year, month)));
    }

    public static DateOnly ResolveDueDate(DateOnly anchorDate, int nominalDay, int intervalMonths, int occurrenceIndex)
    {
        var occurrenceDate = AddMonths(anchorDate, intervalMonths * occurrenceIndex);
        var day = Math.Clamp(nominalDay, 1, LastDayOfMonth(occurrenceDate.Year, occurrenceDate.Month));
        return new DateOnly(occurrenceDate.Year, occurrenceDate.Month, day);
    }

    /// <summary>Plans the occurrences of a recurrence between two dates, stepping by the interval in months.</summary>
    public static IReadOnlyList<RecurrenceOccurrence> Plan(DateOnly anchorDate, int nominalDay, int intervalMonths, DateOnly from, DateOnly to, decimal cycleAmount, string currency)
    {
        var occurrences = new List<RecurrenceOccurrence>();
        if (intervalMonths < 1 || to < from) return occurrences;
        var index = 0;
        while (true)
        {
            var dueDate = ResolveDueDate(anchorDate, nominalDay, intervalMonths, index);
            if (dueDate > to) break;
            if (dueDate >= from) occurrences.Add(new(index, new DateOnly(dueDate.Year, dueDate.Month, 1), dueDate, cycleAmount, currency));
            index++;
        }
        return occurrences;
    }

    /// <summary>Stable occurrence key that scopes a generated title to a recurrence and its period.</summary>
    public static string OccurrenceKey(Guid recurrenceId, int periodYear, int periodMonth) => $"{recurrenceId}:{periodYear:00}-{periodMonth:00}";
}
