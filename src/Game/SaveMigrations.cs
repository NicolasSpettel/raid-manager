using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Game;

/// <summary>One save-format migration: transforms the untyped JSON from <see cref="From"/> to From+1.</summary>
public sealed record Migration(int From, string Note, Func<JsonNode, JsonNode> Migrate);

/// <summary>
/// The migration registry — the antidote to DM1's ad-hoc <c>normalizeSavedRunState</c> (six inlined
/// migrations with the version stuck at 1). Every breaking save change bumps
/// <see cref="CurrentVersion"/> and appends one ordered, documented migration; a frozen fixture of the
/// old version round-trips in CI (save-format.md §Migration registry).
/// </summary>
public static class SaveMigrations
{
    /// <summary>The current save format version. Bumped by every breaking change.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Ordered {from → from+1} migrations. Empty at v1 — the fold is a no-op until v2 exists.</summary>
    public static IReadOnlyList<Migration> All { get; } = new List<Migration>();
}
