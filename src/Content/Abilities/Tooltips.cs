using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Content;

/// <summary>
/// Renders a tooltip template by interpolating <c>{field}</c> tokens from a row's own fields. One
/// module, shared by the UI and the drift test, so "the tooltip shows the engine's numbers" is
/// enforced rather than hoped for (content-authoring §Tooltip template rules).
/// </summary>
public static partial class Tooltips
{
    /// <summary>Replace every <c>{field}</c> token with the field's value; unknown tokens are left intact (drift test catches them).</summary>
    public static string Render(string template, IReadOnlyDictionary<string, string> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        return TokenRegex().Replace(template, match =>
            fields.TryGetValue(match.Groups[1].Value, out string? value) ? value : match.Value);
    }

    /// <summary>The field names referenced by a template's <c>{field}</c> tokens.</summary>
    public static IEnumerable<string> Tokens(string template)
    {
        foreach (Match match in TokenRegex().Matches(template))
        {
            yield return match.Groups[1].Value;
        }
    }

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex TokenRegex();
}
