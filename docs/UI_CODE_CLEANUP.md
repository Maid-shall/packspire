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
- Styles are loaded in the following deliberate cascade order from
  `PackspireUiFoundation.Root.cs`: `Theme`, `Packing`, `Route`, `Roster`,
  `Battle`, `Polish`, then `Hub`.
- `PackspireTheme.uss` owns shared/base styles. Screen-specific work belongs in
  its matching file. `Polish` remains the intentional late shared override
  layer; do not append another global override file after it.
- `.ps-book-*` and `.ps-tabletop-*` are not dead yet; see the screen table above.

## Cleanup completed in the first pass

- Removed unsupported `z-index` declarations.
- Removed the unused Archive gallery and looping facility-reel implementation.
- Removed unused `AddBookTabs` and unused `HubFacilityCatalog` lookup helpers.
- Removed dead archive/facility-reel USS selectors and exact duplicate rules.
- Centralized product-screen clearing and full-rebuild fallbacks.

## Cleanup completed in the second pass

- Split the former 8,414-line style sheet into seven ordered files by screen
  responsibility without changing cascade order.
- Removed the unreferenced legacy 2.5D Hub and old Hub-home style generations.
- Removed the last old Hub-home-only selectors left in shared polish rules.
- Kept active Book and Tabletop styles because transitional screens still use
  them.

## Deferred cleanup

1. Remove the remaining older `.ps-home-*` family only after confirming no
   transitional Book screen depends on its shared descendant styling.
2. Migrate Faction and Expedition away from Tabletop before deleting that CSS.
3. Migrate Reward, Shop and GameOver away from BookShell before deleting Book
   CSS and assets.
4. Reduce intentional duplicate selectors within each screen file during that
   screen's visual rewrite; do not blindly merge cross-file cascade overrides.
