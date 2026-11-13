using Financeiro.Domain.Entities;

namespace Financeiro.Domain.Tests;

public class PayableTitleStateTests
{
    [Fact]
    public void DraftAllowsForwardTransitionsIncludingEffectiveAndCancelled()
    {
        var title = new PayableTitle();
        Assert.Equal(TitleState.Draft, title.State);
        Assert.True(title.CanTransitionTo(TitleState.Draft));
        Assert.True(title.CanTransitionTo(TitleState.Forecast));
        Assert.True(title.CanTransitionTo(TitleState.Effective));
        Assert.True(title.CanTransitionTo(TitleState.Cancelled));
    }

    [Fact]
    public void ForecastRejectsReturningToDraft()
    {
        var title = new PayableTitle { State = TitleState.Forecast };
        Assert.False(title.CanTransitionTo(TitleState.Draft));
        Assert.True(title.CanTransitionTo(TitleState.Forecast));
        Assert.True(title.CanTransitionTo(TitleState.Effective));
        Assert.True(title.CanTransitionTo(TitleState.Cancelled));
    }

    [Fact]
    public void EffectiveRejectsDowngradingToForecastOrDraft()
    {
        var title = new PayableTitle { State = TitleState.Effective };
        Assert.False(title.CanTransitionTo(TitleState.Draft));
        Assert.False(title.CanTransitionTo(TitleState.Forecast));
        Assert.True(title.CanTransitionTo(TitleState.Effective));
        Assert.True(title.CanTransitionTo(TitleState.Cancelled));
    }

    [Fact]
    public void CancelledIsTerminal()
    {
        var title = new PayableTitle { State = TitleState.Cancelled };
        Assert.False(title.CanTransitionTo(TitleState.Draft));
        Assert.False(title.CanTransitionTo(TitleState.Forecast));
        Assert.False(title.CanTransitionTo(TitleState.Effective));
        Assert.False(title.CanTransitionTo(TitleState.Cancelled));
    }
}
