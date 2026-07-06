# ADR-0006: No code is ported from DM1

**Status:** Accepted · July 2026 · Decided by the developer.

## Decision

Zero source files, functions, or copy-pasted snippets move from `E:\dungeon-manager` into this
repo. What may cross over, and only as prose or re-typed-from-scratch design:

- **Lessons** (docs/dm1-lessons.md is the canonical list).
- **Test patterns** — golden-stream hashing, drift tests, registry completeness tests — as
  *patterns*, re-implemented against the new contracts.
- **Data shapes as inspiration** — e.g. the storage-adapter interface idea, the trait record
  shape — re-designed for N-size raids and the new save aggregate.

## Context

The developer: "fresh repo, I don't think I want anything ported because I don't think it
fulfills our needs anymore." DM1's most reusable-looking pieces are precisely the ones shaped by
its constraints: 5-man slot Records, turn-based ability kits, a UI split into Desktop/Mobile
trees, a save schema with ad-hoc migrations. Porting any of them imports the constraint.

## Rationale

- Every DM1 module embeds at least one assumption this blueprint exists to remove (party size,
  turns, closure-coupled context, hand-written tooltips).
- A porting habit erodes the walls: "just this one file" is how the old architecture leaks in.
- The genuinely valuable DM1 asset is the *post-mortem* (refactor roadmap, audits, sim handoff),
  which ports losslessly as documentation.

## Consequences

- Some things get rebuilt that worked fine (storage adapter, seeded RNG). Accepted cost —
  each is small, and rebuilding them against the new contracts is cheaper than auditing ported
  code for stale assumptions.
- DM1 remains open in a second editor as *reference*, never as a copy source.
- If this rule ever genuinely blocks progress, the exception is a superseding ADR naming the
  file and the audited assumptions — not a quiet copy-paste.
