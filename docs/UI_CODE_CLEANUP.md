# UI code / USS cleanup

Updated: 2026-07-22

This note records the UI paths that are safe to maintain or remove. It is an
architecture note, not a visual-design specification.

## Current screen ownership

| Area | Current implementation | Status |
|---|---|---|
| Hub | `PackspireUiFoundation.Hub.cs`, `ps-hub-v3*` | Current |
| Character roster | `PackspireUiFoundation.CharacterRoster.cs` | Current |
| Status / Vault / Compendium | management shell in `MetaScreens.cs` | Current |
| Packing | storage-rite UI in `PreparationScreens.cs` | Current |
| Exploration / Battle | Route UI | Current |
| Faction | Tabletop desk | Transitional, still used |
| Expedition | Tabletop desk | Transitional, still used |
| Reward / Shop / GameOver | Book shell | Transitional, still used |

Do not remove Book or Tabletop code/styles until every transitional screen has
an explicit replacement and Play Mode QA has passed.

## Root and rebuild rules

- Product screens currently share one `screenRoot`.
- `ClearScreenTree()` is the single reset point for cached screen references.
- Full rebuild fallbacks must call `RebuildScreen(builder)` rather than clearing
  `screenRoot` independently.
- Packing captures its scroll offsets before calling `ClearScreenTree()`.
- Selection-only changes should use the existing partial-refresh methods. Full
  rebuilds are reserved for structural changes or explicit fallback recovery.

## USS rules

- UI Toolkit draw order comes from visual-tree order. Do not add `z-index`.
- Do not use unsupported pseudo classes such as `:last-child`.
- `PackspireTheme.uss` remains the loaded style sheet. New visual work should
  extend current screen families instead of creating another global override
  layer at the end of the file.
- The old `.ps-home-*`, `.ps-hub-home*`, and `.ps-v2-*` families have no C#
  references in the current product path. They are candidates for a dedicated
  mechanical removal pass after a Unity compile and visual baseline capture.
- `.ps-book-*` and `.ps-tabletop-*` are not dead yet; see the screen table above.

## Cleanup completed in the first pass

- Removed unsupported `z-index` declarations.
- Removed the unused Archive gallery and looping facility-reel implementation.
- Removed unused `AddBookTabs` and unused `HubFacilityCatalog` lookup helpers.
- Removed dead archive/facility-reel USS selectors and exact duplicate rules.
- Centralized product-screen clearing and full-rebuild fallbacks.

## Deferred cleanup

1. Remove the zero-reference `.ps-home-*`, `.ps-hub-home*`, and `.ps-v2-*`
   blocks in a separate commit, then compare Hub, F10, Character and Packing.
2. Migrate Faction and Expedition away from Tabletop before deleting that CSS.
3. Migrate Reward, Shop and GameOver away from BookShell before deleting Book
   CSS and assets.
4. Split `PackspireTheme.uss` by screen family only after verifying Unity import
   order; do not perform a blind selector merge because later rules currently
   provide intentional cascade overrides.
