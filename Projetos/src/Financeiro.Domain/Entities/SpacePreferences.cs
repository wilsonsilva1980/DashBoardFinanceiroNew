namespace Financeiro.Domain.Entities;

public sealed class SpacePreferences : ScopedEntity
{
    public int RecurrenceHorizon { get; set; } = 12;
    public string InvalidDayRule { get; set; } = "MonthEnd";
}
