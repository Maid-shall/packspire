# Legacy expedition runtime boundary

The seamless journey scene is the production expedition runtime.

## Production path

`PackspireGame.StartRun` creates the real `RunState`, creates its
`CourierRouteState`, and loads `JourneyAnimationPrototype`. The scene attaches to
that same `RunState`; it must not clone battle, route, inventory, or health data.
Battle victory opens the existing `ScreenId.Reward` product screen and returns to
the same live run before resuming the route. Journey completion returns through
`PackspireGame.UiFinishSeamlessJourney` so the existing run-result and save pipeline
remains authoritative. Developer-menu journey previews use this same connected
runtime; the isolated run exists only as a direct-scene editor recovery fallback.

## Frozen legacy path

The following code and assets are retained as reference and QA fallback only:

- `ScreenId.Route` and `PackspireUiFoundation.CourierRoute*.cs`
- `ScreenId.Battle`, `PackspireUiFoundation.Battle*.cs`,
  `PackspireBattleView.uxml`, and `PackspireBattle.uss`
- the seal-grid combat presentation

Normal expedition startup must not navigate to these screens. Do not add new
features to them. Only make narrowly scoped fixes needed to keep old content or
comparison previews compiling until the replacement covers every remaining case.

The standalone legacy battle UI source is excluded from normal compilation behind
`PACKSPIRE_LEGACY_BATTLE_UI`. Its source remains available for visual comparison,
but enabling it is an explicit maintenance action. Shared card presentation helpers
needed by current screens live in `PackspireUiFoundation.LegacyBattleCompatibility`
and remain compiled independently of the retired screen.

## Journey presentation boundaries

`JourneyTravelGameplayPrototype` is split by responsibility: view binding, route
ledger, developer controls, world actors, legacy battle bridge, and realtime battle
presentation are separate partials. `JourneyWalkCyclePrototype` owns courier motion
and pose only; `JourneyParallaxWorld` owns the scrolling background layers and their
runtime assets. Enemy identity, stats, art references, and realtime timeline are
authored in `JourneyBattleEncounterProfile` assets so adding or swapping an encounter
does not require editing the journey controller.

## Still shared, not legacy

`RunState`, `CourierRouteSystem`, `BattleSystem`, card definitions, inventory,
save/result handling, and common card views remain shared domain assets. The new
journey presentation should call these systems instead of copying their rules.
