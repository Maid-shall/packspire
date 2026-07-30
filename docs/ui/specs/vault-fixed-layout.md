# Vault Fixed Layout

## Visual target

- Approved composition target: `docs/ui/references/vault/vault-ui-target-v2-pop-dark.png`
- The target image defines density, hierarchy, color rhythm, and silhouette only.
- Production implementation uses the existing VaultCodex and shared card assets listed below.
- Do not crop UI components or text from the generated target image.

## Coordinate system

- Logical canvas: 1280 x 720
- Validation aspect ratio: 16:9
- Panel scaling: `ScaleWithScreenSize`, reference 1280 x 720, match 0.5
- Layout containers use logical coordinates. Do not copy 1920 x 1080 capture pixels.

## Major regions

| Region | X | Y | Width | Height |
|---|---:|---:|---:|---:|
| Header | 20 | 8 | 1240 | 70 |
| Back action | 20 | 16 | 118 | 42 |
| Brand | 152 | 8 | 400 | 70 |
| Inventory | 24 | 82 | 566 | 624 |
| Detail | 604 | 82 | 652 | 624 |
| Inventory header | 0 | 0 | 554 | 112 |
| Inventory viewport | 0 | 112 | 554 | 464 |
| Inventory footer | 0 | 576 | 554 | 48 |
| Item art | 0 | 0 | 318 | 310 |
| Item information | 332 | 0 | 320 | 310 |
| Battle card | 0 | 322 | 230 | 302 |
| Effects | 238 | 322 | 414 | 298 |

## Detail subregions

| Region | X | Y | Width | Height |
|---|---:|---:|---:|---:|
| Item artwork safe area | 14 | 4 | 290 | 278 |
| Durability | 14 | 282 | 290 | 26 |
| Rank and type | 0 | 8 | 320 | 18 |
| Item name | 0 | 28 | 320 | 48 |
| Description | 0 | 78 | 320 | 60 |
| Occupancy pattern | 0 | 148 | 320 | 158 |
| Card header | 8 | 4 | 214 | 32 |
| Shared card host | 31 | 38 | 168 | 252 |
| Color effect | 0 | 0 | 414 | 112 |
| Link effect | 0 | 116 | 414 | 112 |
| Actions | 0 | 232 | 414 | 66 |

## Asset policy

- Header and identity flourishes: `Art/UI/PopDark/VaultCodexV3`
- Category controls: `Art/UI/PopDark/VaultCodexV4/category-tab-*`
- Inventory cells: `Art/UI/PopDark/VaultCodexV4/item-cell-*`
- Item focus: item display texture over `VaultCodexV3/magic-circle`
- Section dividers: `VaultCodexV3/header-flourish`
- Effects: `VaultCodexV5/effect-panel-color-v5` and `effect-panel-link-v5`
- Actions: `VaultCodexV3/round-lock` and `round-next`
- Back action: `ManagementV4/secondary-action-v2` with the shared Back icon
- Battle and exploration cards: shared `PackspireBattle.uss` presentation and shared card builders
- Layout containers remain transparent. Do not replace authored assets with CSS rectangles.

## Interaction policy

- The Vault owns its back action; the shared navigation back button is hidden on this screen.
- Category controls keep identical geometry in normal and selected states.
- The sort control opens the image-backed in-screen choice menu. It never cycles modes on click.
- The lock action is dim when unlocked and illuminated only while `ItemInstance.insured` is true.
- The record arrow is shown only for heirloom items and opens the heirloom record. It never selects the next inventory item.
- Vault occupancy reads the selected `ItemInstance` through `BackpackSystem.Layout(def, 0, item)` and displays exactly one shape. Shape variants belong to the Compendium only.
- Occupied cells retain their `Element` and receive the matching fire, water, wind, or earth tint.
- The card host displays the same raw `ps-battle-card` used by expedition and battle screens.
- `VaultStyleSheets` must include `PackspireBattle.uss`; otherwise the card contents lose their absolute layout.
- Do not add `ps-equipment-card-preview` to the fixed Vault card and do not replace its authored card artwork with the equipment display art.

## Responsive policy

The complete composition is fixed to the 1280 x 720 logical canvas. Only the
inventory contents scroll. Text may wrap inside its assigned safe area but may
not resize or move neighboring regions.
