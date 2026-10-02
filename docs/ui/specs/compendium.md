# Compendium UI Specification

Approved reference:
`docs/ui/references/compendium/compendium-ui-target-approved.png`

Logical layout uses the current 1280x720 PanelSettings reference. The approved
1920x1080 image is a 1.5x presentation reference, not a direct USS coordinate
source.

## Information Layout

- Header: shared management back action, archive crest, `図鑑`.
- Index: fixed equipment, role, and enemy tabs; dynamic discovered/unknown rows.
- Equipment record hero: item art, rank/type, name, description, and every
  unique rotational occupancy pattern.
- Occupancy diagrams contain geometry only. Random element colors are not
  recorded.
- Card area: the existing expedition battle/exploration card component at its
  established portrait size. The selected face is controlled by two explicit
  segmented buttons.
- Fixed information order: LINK effect, then acquisition information.
- Acquisition information contains source and acquisition tier only. Rarity is
  already present above the item name and is not repeated.
- Archive note and page arrow navigate to the selected record's lore page.

## Component Sources

| Region | Production source |
|---|---|
| Header and category icons | Shared management chrome atlas |
| Category and index frames | Home material kit, quiet index rows and cyan selection |
| Item art | Existing equipment artwork through `VaultItemArt` |
| Occupancy frame | Existing `occupancy-grid` image plus neutral cells |
| Card | `BuildEquipmentCardFacePreview` |
| LINK panel | Shared `VaultEffectRecord` with the cyan Vault frame |
| Acquisition panel | Quiet archive information surface and gold rules |
| Page arrow | Existing `round-next` action image |

## Ownership

- `PackspireCompendiumView.uxml`: fixed tabs, record hierarchy, fixed
  information panels, card and shape insertion points.
- `PackspireVaultCodexFinal.uss`: dimensions, images, colors, typography, and
  semantic selected/display states.
- C# presenter: catalog values, discovered rows, unique shape generation,
  card data, click handlers, and semantic state classes only.

The camera, PanelSettings, and renderer configuration are outside this screen's
scope.

## Equipment record fit

The specimen summary fills its column instead of retaining the old 55% width.
The name stays on one line above the wrapped description. The item artwork is
bounded by the specimen column, with a 250x250 logical image (375x375 capture
pixels). Occupancy diagrams have a quiet dark background so paper artwork does
not cover the cells. The index footer also uses a quiet supporting surface.
