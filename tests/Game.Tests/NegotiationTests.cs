using System.Collections.Generic;
using Content;
using Engine;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>Contract talks (GDD §4): pushing spends patience or backfires, and Negotiation tilts the odds.</summary>
public class NegotiationTests
{
    private static JobOffer Offer() =>
        new(new GuildId("g"), "Ashvale Wardens", "Ashvale", PrestigeTier.Local, 25, 6, 8000, 500, "just survive", "a rival");

    private static Manager Manager(int negotiation) =>
        new("Mgr", 40, "Ironreach", "guild_officer", new Dictionary<string, int> { ["negotiation"] = negotiation });

    [Fact]
    public void Start_HasFullPatience_AndIsSignable()
    {
        NegotiationState state = Negotiations.Start(Offer());
        Assert.True(state.CanPush);
        Assert.False(state.Withdrawn);
        Assert.Equal(3, state.Patience);
    }

    [Fact]
    public void Push_EitherWithdraws_OrConsumesPatience()
    {
        NegotiationState start = Negotiations.Start(Offer());
        NegotiationState after = Negotiations.Push(start, PushIntensity.Gentle, Manager(10), new SeededRng(1));
        Assert.True(after.Withdrawn || after.Patience < start.Patience);
    }

    [Fact]
    public void Withdrawn_CannotBePushedFurther()
    {
        NegotiationState withdrawn = Negotiations.Start(Offer()) with { Withdrawn = true };
        Assert.False(withdrawn.CanPush);
        Assert.Same(withdrawn, Negotiations.Push(withdrawn, PushIntensity.Hard, Manager(10), new SeededRng(1)));
    }

    [Fact]
    public void HardPush_BackfiresMoreOften_ForWeakNegotiators()
    {
        int weakWithdraws = 0;
        int strongWithdraws = 0;
        for (ulong seed = 0; seed < 300; seed++)
        {
            NegotiationState start = Negotiations.Start(Offer());
            if (Negotiations.Push(start, PushIntensity.Hard, Manager(3), new SeededRng(seed)).Withdrawn)
            {
                weakWithdraws++;
            }

            if (Negotiations.Push(start, PushIntensity.Hard, Manager(18), new SeededRng(seed)).Withdrawn)
            {
                strongWithdraws++;
            }
        }

        Assert.True(weakWithdraws > strongWithdraws, $"weak={weakWithdraws} strong={strongWithdraws}");
    }
}
