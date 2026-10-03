# PACKSPIRE system rework decisions

Status: adopted playable vertical-slice specification, 2026-08-04.

This document overrides older notes where they conflict with the decisions
below. It separates adopted rules from prototype-only implementation so that a
visual mockup is not mistaken for finished game design.

## 1. Storage formula and cargo

- Cargo uses the same storage formula grid as equipment. A second cargo-only
  bag is not part of the standard loop.
- Recovered cargo first enters an unassigned-cargo queue. It cannot interrupt
  the current route node or force an immediate packing puzzle.
- Only a relay node can move unassigned cargo into the storage formula and
  reopen placement. Confirming that relay layout recalculates LINK and color;
  charges already spent during the expedition remain spent.
- A relay can change item placement but not replace the formula's core,
  conduit, resonance, or stability components.
- Shape controls usable cargo volume.
- Durability is the shared cost for equipment use and difficult deliveries.
- Ordinary cargo should be quick to place. Large or cursed cargo may impose
  temporary cell restrictions as an explicit exception.

## 2. Effect ownership

| Rule | Primary result |
| --- | --- |
| Shape | Cargo capacity and placement |
| LINK adjacency | Upgrade or transform battle cards |
| Color matching | Generate delivery seals and their charges |
| Durability | Shared expedition cost |

Do not let LINK and color both provide broad generic bonuses.

## 3. Exploration

The target exploration loop is a branching infernal courier route ledger, not
continuous movement through many small empty grid cells.

- A route is a sequence of meaningful nodes with visible branch information.
- Each edge advances the delivery by a stated number of days.
- The delivery deadline is the only route-wide pressure meter.
- Cargo may change the value of a route, but it does not own a second route HP bar.
- Delivery seals reduce travel days, prevent event delay, or reload another seal.
- The existing grid is legacy/special-content infrastructure. It is not opened
  automatically at every route location.

Delivery seals come from deterministic sources:

1. The current role provides one fallback or signature seal.
2. Storage color matches generate fire, water, wind, and earth delivery seals.
3. LINK adjacency remains exclusively responsible for battle-card upgrades.
4. An heirloom modifies one seal.
5. Consumables provide emergency one-shot seals.
6. Cargo may temporarily grant a special seal.

### Adopted expedition loop

- One delivery uses ten committed route segments: nine branch phases followed
  by the destination operation.
- Committing a station dispatches by location role. Event, battle, cargo,
  relay, and final delivery each use their own resolver and screen flow.
- Equipment exploration cards and card reverse faces are not part of the
  adopted expedition loop. Packing produces battle cards and delivery seals.
- A location resolves once its assigned handler returns a
  `CourierLocationOutcome`. That outcome may change additional days, recovery,
  and performance without coupling route logic to a
  specific minigame.
- Recovery locations open the recovery choice and may add one item to the
  unassigned-cargo queue. Pursuit locations open combat. Events open an event
  choice. Relay and destination are resolved directly by their own rules.
- `MINIGAME` is a reserved location-resolution kind. No standard route node
  requires a minigame yet; future node content can return the same outcome
  contract without changing route progression.
- The expedition succeeds only after the tenth destination location resolves.
- Recovery locations grant unassigned cargo. Relay locations repair HP,
  restore one spent delivery-seal charge, and
  permit one packing pass.
- Combat and events resolve through their own systems. Exceeding the delivery
  deadline or failing a location ends the delivery, including at the destination.

## 4. Roles

- Target four base roles and at most one special role for the first complete
  version.
- Only the active role applies its main passive, active skill, and signature
  delivery seal.
- Each base role has a small branching skill tree.
- Old reaction and composite-role discoveries become a one-slot qualification
  seal where useful.
- Learned-role passive stacking and an endlessly expanding role graph are no
  longer target rules.

Existing role and reaction data remains until migration work is scheduled. New
content must follow the simplified target rather than expanding the legacy
network.

## 5. Characters are authored, not created

- There is no character creator or layered appearance editor in the target.
- The player selects an existing authored courier.
- Each courier keeps a fixed identity, portrait set, trait, and active skill.
- Role progression changes rules and expedition options; it does not rebuild
  the character's visual appearance.
- Additional courier art is normal character content, not interchangeable
  head, outfit, or palette parts.

## 6. UI and code boundaries

- UXML owns fixed structure and named hosts.
- USS owns placement, dimensions, colors, textures, and visual state.
- C# owns option data, events, save state, and assigning runtime images.
- C# must not contain fixed coordinates, decorative colors, or screen-specific
  background styling.

## 7. Prototype acceptance

The prototype is accepted only when:

- exactly four base roles are presented as the active-role choices;
- each base role exposes one signature delivery seal and one of two
  persistent branch choices;
- a legacy advanced role can occupy at most one qualification-seal slot;
- starting an expedition opens the route ledger;
- the ledger offers meaningful branches and dispatches each location through
  event, battle, cargo, relay, or delivery resolution;
- color matching generates delivery seals, no exploration-card reverse face
  appears in packing or the adopted route loop, and LINK affects battle cards;
- the UI compiles and is captured at 1920 x 1080 using 1280 x 720 logical
  coordinates;
- no rendering camera or scene object is added for the UI.
