using System.Linq;
using Content;
using Xunit;

namespace Content.Tests;

/// <summary>
/// Drift tests: every tooltip number is a token resolved from the row itself, never hand-typed. This
/// is the cheapest high-value test in the suite — it makes DM1's #1 bug class (tooltip says 187%,
/// engine does 2×) structurally impossible for templated text (content-authoring §Tooltip rules).
/// </summary>
public class TooltipDriftTests
{
    [Fact]
    public void EveryTooltipToken_ResolvesFromItsRow()
    {
        foreach (AbilityRow row in Abilities.Registry.All)
        {
            var fields = row.TooltipFields();
            foreach (string token in Tooltips.Tokens(row.Tooltip))
            {
                Assert.True(fields.ContainsKey(token), $"{row.Id}: tooltip token {{{token}}} has no matching field");
            }
        }
    }

    [Fact]
    public void RenderedTooltip_HasNoUnresolvedTokens()
    {
        foreach (AbilityRow row in Abilities.Registry.All)
        {
            string rendered = Tooltips.Render(row.Tooltip, row.TooltipFields());
            Assert.DoesNotContain('{', rendered);
            Assert.DoesNotContain('}', rendered);
        }
    }

    [Fact]
    public void TooltipProse_HasNoHardCodedNumbers()
    {
        foreach (AbilityRow row in Abilities.Registry.All)
        {
            // Blank the known tokens, leaving only human prose — which must contain no digits.
            var blanked = row.TooltipFields().ToDictionary(kv => kv.Key, _ => string.Empty, System.StringComparer.Ordinal);
            string prose = Tooltips.Render(row.Tooltip, blanked);
            Assert.False(prose.Any(char.IsDigit), $"{row.Id}: tooltip prose has a hard-coded number: '{prose}'");
        }
    }
}
