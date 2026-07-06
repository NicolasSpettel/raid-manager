using System;

namespace Game;

/// <summary>
/// Morale — a distinct axis from condition (GDD §8: "wants to raid? needs motivating?"). It moves with
/// results (boss kills lift it, wipes sink it), with playing time (a benched raider sulks — they want to
/// raid), and with holidays (granted = happy, denied = "giga mad", GDD §5). It feeds combat gently and,
/// later, drama/retention. Baseline ~70 (a content raider) resolves to neutral, so it only bites at the
/// extremes. First-pass numbers.
/// </summary>
public static class MoraleModel
{
    /// <summary>Gentle combat performance multiplier from morale (percent). A happy raider tries harder; a miserable one checks out.</summary>
    public static int PerformancePct(int morale) => Math.Clamp(90 + ((morale * 15) / 100), 88, 105);

    /// <summary>Move a raider's morale after a week: results (guild-wide) + whether they were benched + any holiday.</summary>
    public static int AfterWeek(int morale, int kills, int wipes, bool benched, bool holidayThisWeek, bool holidayGranted)
    {
        int delta = (kills * 2) - (wipes * 3);
        if (benched)
        {
            delta -= 6; // sat out — wants to raid
        }

        if (holidayThisWeek)
        {
            delta += holidayGranted ? 4 : -8; // day off granted vs denied
        }

        return Math.Clamp(morale + delta, 0, 100);
    }
}
