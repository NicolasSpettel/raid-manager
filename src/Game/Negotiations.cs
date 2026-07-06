using System;
using Engine;

namespace Game;

/// <summary>How hard you press the board in a contract talk.</summary>
public enum PushIntensity
{
    Gentle,
    Hard,
}

/// <summary>
/// The live state of a contract negotiation (GDD §4, kept deliberately simple): the two live levers
/// (your salary, the board's season expectation), how much patience the board has left, whether they've
/// walked, and their last line. You can always sign the current terms — pushing may improve them, but
/// pushing too hard (especially without Negotiation) makes them pull the offer.
/// </summary>
public sealed record NegotiationState(int Salary, int ExpectationLevel, int Patience, bool Withdrawn, string Response)
{
    public bool CanPush => !Withdrawn && Patience > 0;

    public string ExpectationText => ExpectationLevel switch
    {
        <= 0 => "Just survive the season — keep the guild solvent.",
        1 => "Finish mid-table in the region.",
        2 => "Push for a top-3 regional finish.",
        _ => "Contend for the regional title.",
    };
}

/// <summary>Runs the two-button contract talk. Deterministic given the rng; your Negotiation attribute tilts the odds.</summary>
public static class Negotiations
{
    public static NegotiationState Start(JobOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        int baseSalary = 200 + (offer.AvgHalfStars * 15);              // personal scale (economy-model §2: 200–400 local)
        int expectation = Math.Clamp(offer.AvgHalfStars - 5, 0, 3);    // a stronger roster means a hungrier board
        return new NegotiationState(baseSalary, expectation, Patience: 3, Withdrawn: false,
            "The board offers you the job on their standard terms.");
    }

    public static NegotiationState Push(NegotiationState state, PushIntensity intensity, Manager manager, SeededRng rng)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(rng);

        if (!state.CanPush)
        {
            return state;
        }

        bool hard = intensity == PushIntensity.Hard;
        int negotiation = manager.Of("negotiation");
        int withdrawChance = hard ? Math.Max(2, 22 - negotiation) : 4; // a hard push can blow the whole thing up
        int successChance = (hard ? 40 : 70) + (negotiation * 2);
        int cost = hard ? 2 : 1;
        int roll = rng.NextInt(100);

        if (roll < withdrawChance)
        {
            return state with { Withdrawn = true, Response = "You pushed too hard — they've pulled the offer." };
        }

        int patience = Math.Max(0, state.Patience - cost);

        if (roll < withdrawChance + successChance)
        {
            int salary = state.Salary + (hard ? 80 : 30);
            int expectation = Math.Max(0, state.ExpectationLevel - (hard ? 2 : 1));
            string response = patience == 0
                ? "They give ground — but that's their final word. Take it or leave it."
                : "They soften: lower expectations and a little more pay.";
            return state with { Salary = salary, ExpectationLevel = expectation, Patience = patience, Response = response };
        }

        string held = patience == 0
            ? "They hold firm — this is their final offer."
            : "They won't budge on that.";
        return state with { Patience = patience, Response = held };
    }
}
