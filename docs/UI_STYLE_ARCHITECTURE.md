# PACKSPIRE UI Style Architecture

Updated: 2026-07-22

## Load order

1. `PackspireTheme.uss` ? global typography, colors, generic controls, transitions
2. `PackspirePacking.uss` ? storage-rite screen only
3. `PackspireRoute.uss` ? expedition route and exploration only
4. `PackspireRoster.uss` ? character roster only
5. `PackspireBattle.uss` ? battle only
6. `PackspirePolish.uss` ? final shared pop-dark chrome only
7. `PackspireManagement.uss` ? status, vault, compendium, faction, expedition overview
8. `PackspireMeta.uss` ? layer hosts, readability, expedition detail, roster, heirloom
9. `PackspireCommerce.uss` ? shop, reward, game-over and game-clear result
10. `PackspireHub.uss` ? hub-only final overrides

Later files may override earlier files. Preserve this order unless the ownership model is deliberately changed.

## Ownership rules

- Shared visual tokens and generic components belong in `Theme` or the small shared `Polish` layer.
- Screen geometry belongs in the screen-owning stylesheet.
- Do not add screen-specific selectors to `Theme`.
- Do not duplicate a selector in a later sheet merely to compensate for an unclear earlier owner; move the declaration to the correct owner.
- Use class state (`ps-selected`, `ps-locked`, `ps-new`) instead of rebuilding geometry for interaction feedback.
- UI Toolkit unsupported CSS such as `z-index`, `last-child`, and `border-*-style` must not be added.
- Prefer hierarchy order for front/back placement and solid border colors or opacity for preview states.

## Selective-frame foundation (2026-07-23)

Shared classes live in `PackspirePolish.uss`:

- Surfaces: `ps-surface-quiet`, `ps-surface-outer`, `ps-frame-focal`
- List: `ps-list-item` (+ `ps-selected` / `ps-locked` / `ps-list-item-mark`)
- Type: `ps-typo-screen|section|item|body|secondary|value|eyebrow`
- Actions: `ps-action-primary|secondary|nav`
- Separator: `ps-sep-ink`

Expedition preparation is the first product screen using this system. Other screens keep legacy chrome until a follow-up rollout.

