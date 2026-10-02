# Shop UI Specification

The existing merchant presentation is retained and aligned with the current
home screen. This is a layout repair, using the existing merchant artwork and
home material kit.

## Layout and information

PanelSettings reference: 1280x720. Validation capture: 1920x1080 (1.5x).

| Region | Logical bounds | Capture bounds |
|---|---|---|
| Header | y=0, h=72 | y=0, h=108 |
| Merchant | x=28, y=88, w=300, h=608 | x=42, y=132, w=450, h=912 |
| Stock | x=336, y=88, w=300, h=608 | x=504, y=132, w=450, h=912 |
| Details | x=644, y=88, w=340, h=608 | x=966, y=132, w=510, h=912 |
| Purchase | x=992, y=88, w=260, h=608 | x=1488, y=132, w=390, h=912 |

- Merchant: scene, transparent character artwork, and one dialogue strip.
- Stock: category controls, item name/type/price, and one selected row.
- Details: name, price, transparent item artwork, geometry/element, performance,
  LINK effect, base battle-card values/counts, and delivery-seal effect.
- Purchase: selected product, current gold, total, unavailable reason, purchase
  and leave controls. It stays fixed while the stock and details scroll.
- Developer preview cannot purchase. The disabled button is visibly subdued.

## Visual sources and hierarchy

| Region | Source | Role |
|---|---|---|
| Screen background | Home `hub-misprint-background-v1` | Background |
| Merchant scene and courier | `MerchantCatalog.Default` resources | Background / object artwork |
| Item hero | Existing `VaultItemDisplayArt` | Transparent object artwork |
| Stock thumbnails | Existing equipment atlas | List thumbnails |
| Purchase action | Home `hub-nav-selected-paper-v1` | Completed component; label only |
| Other information areas | Screen-specific USS | Quiet supporting surfaces |

The home profile is used: dark textured background, warm ivory text, fine gold
rules, cyan selection, and red for the main action. Merchant artwork is the
visual anchor; the selected product and purchase total carry the information
hierarchy. Large paper textures are not stretched across nested detail blocks.

## Ownership

- `PackspireShopView.uxml`: fixed merchant, stock, detail and purchase regions.
- `PackspireShop.uss`: all geometry, typography, imagery and state appearance.
- `ShopScreen.cs`: catalog values, list generation, bindings and operations.

## Verification

Validate the six-item list, weapon/tool filters, selection and detail updates,
the preview purchase lock, and scrolling without moving the purchase controls.
Capture at 1920x1080 and check the shared back/developer controls for overlap.
