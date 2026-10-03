# Courier route ledger implementation spec

Status: map-first ten-segment product vertical slice, 2026-08-05.

## Runtime frame

- Physical QA capture: 1920 x 1080.
- UI Toolkit logical space: 1280 x 720.
- Fixed structure: `PackspireCourierRouteView.uxml`.
- Screen visuals, map coordinates, and link geometry: `PackspireCourierRoute.uss`.
- Runtime data, semantic state, and interactions: `PackspireUiFoundation.CourierRoute.cs`.
- Map pan, zoom, fit, and focus behavior: `PackspireUiFoundation.CourierRoute.Map.cs`.
- Courier marker, walk-cycle, day ticks, and arrival handoff:
  `PackspireUiFoundation.CourierRoute.Travel.cs`.
- Route rules and node topology: `CourierRouteSystem.cs`.

## Composition

1. The route map occupies the complete frame and remains the visual subject.
2. The title is a compact upper-left ledger registration, not a separate panel.
3. HP, recovered cargo, and remaining days form one compact upper-right HUD.
4. Remaining days use a small numeral plus a segmented deadline meter. It is
   turn/day based and must never look like a real-time clock.
5. Selecting an available node enlarges its stamp and opens a small rectangular
   paper slip beside it. Selecting the same node again commits that route.
6. There is no normal `proceed` button. A finish command appears only after the
   delivery is complete or failed.
7. Delivery seals live in a fixed drawer on the right edge. Its narrow tab is
   always visible; the stamp rack overlays the map only while open.
8. Relay packing is a contextual action and remains hidden outside a relay.
9. Committing a route starts a visible courier traversal. The destination
   resolver starts only after the marker arrives.

The map has no permanent legend, right-side dossier, or bottom status slab.
Node kind, day cost, danger, and result are written on the selected-node slip.

## Modular material map

- `misprint-background-route-v1.png` is the replaceable map/background layer.
- `courier-route-dossier-rect-v1.png` is the reusable node-detail paper.
- Each node type uses one complete stamped impression from
  `CourierRoutePrototype`; state tint never stacks another frame over it.
- `courier-route-link-handdrawn-v1.png` is the route stroke. Runtime state only
  changes semantic tint and weight.
- `courier-route-walk-sheet-v1.png` is the six-frame walk cycle for the current
  matching courier. Other couriers use their existing front portrait as a
  truthful fallback until their own walk sheet exists.
- Shared Obsidian Misprint surfaces provide the drawer, index tab, small HUD,
  and contextual commands. Runtime text is never baked into generated art.
- A full-screen generated composition is never used as implementation art.

## Runtime states

- Route node: unknown, available, selected, travel target, current, resolved.
- Known node kinds keep stable ink colors. Selection uses scale, the detail
  slip, and the selected route stroke.
- The courier marker owns current-location emphasis. While travelling, the
  source remains current, the destination is a travel target, and the traversed
  part of the hand-drawn route fills in cyan.
- Every topology edge is generated from `source.next`. Disconnected nodes may
  be inspected when revealed, but cannot be committed.
- Detail slip: hidden, inspected, or selected-for-travel.
- Seal drawer: closed or open.
- Seal: charged, spent, selected.
- Expedition: active, complete, failed.
- Travel: idle or moving. Moving stores source, destination, and day cost in the
  route state so a view rebuild can restore the transition.

## Interaction contract

- Drag empty map space to pan; use the wheel to zoom.
- Focus-current and fit-route controls remain at the lower-left map edge.
- First activation of an available node selects and explains it.
- Second activation of the same selected node commits it. This is a stable
  two-stage action, not a timing-sensitive double-click.
- Map pan, wheel zoom, fit, and focus remain available while the courier moves.
- Day cost advances in visible steps during movement. The arrival resolver is
  never dispatched before the final frame.
- Opening the seal drawer must not resize or refit the map.
- Selecting a seal shows its effect inside the drawer. Use remains an explicit
  drawer action.
- More than four seals are reached by vertical scrolling.

## Gameplay contract

- There is one delivery contract and one destination per expedition.
- Committing an edge adds its day cost. A delivery fails when elapsed days
  exceed the deadline.
- Player damage remains HP damage and HP is always visible.
- A route node dispatches to exactly one resolver: event, battle, recovery,
  relay, delivery, or a future minigame.
- GridBoard is not an automatic intermediate step.
- Color matches generate delivery seals; LINK remains a battle-card rule.
- Recovered cargo remains unassigned until a relay is completed.

## Validation checklist

- The map pans, wheel-zooms, focuses the current node, and fits the route.
- The initial state exposes exactly the edges in `dispatch.next`.
- A disconnected node cannot be committed.
- Selection alone does not move; reselecting the same available node does.
- The courier walks from the source stamp to the target stamp, the remaining-day
  display advances with the journey, and node resolution begins on arrival.
- The detail slip stays readable and does not become a permanent side panel.
- The closed drawer leaves only its right-edge tab visible.
- More than four seals remain reachable through vertical scrolling.
- The normal route screen has no large proceed button or bottom status slab.
- Remaining days, HP, and recovered cargo update after resolution.
- No rendering camera or runtime render target is introduced.
