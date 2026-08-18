# Journey animation prototype

> Historical prototype note. Current journey priorities and acceptance criteria live in
> [`JOURNEY_PRODUCT_PLAN.md`](JOURNEY_PRODUCT_PLAN.md).

This is an isolated feasibility test for a fixed courier travelling through a
multi-layer scrolling world, plus limited-pose couriers with optional programmatic
motion support.
It does not replace the current route or battle screens. The scene now also contains
a playable travel-loop slice to test whether continuous travel has enough interaction.

## Test scene

`Assets/Scenes/JourneyAnimationPrototype.unity`

Open it from `F10 DEV > 常時移動アニメ試験(DEV)` while the game is running.

- The default presentation keeps the courier near the left third and scrolls four
  independently moving world layers: far skyline, architecture, ground and foreground.
- `F2` retains the old cross-screen movement as a direct comparison.
- The same scene compares a mini courier using two or four authored poses,
  the same two poses without procedural support, and the earlier Kain cycle.
- The courier position advances every rendered frame using `Time.deltaTime`.
- The travel root, body-motion root, body sprite and ground shadow are separate.
- Programmatic support is deliberately subtle: small bob, contact squash, peak
  stretch, forward tilt, and a shadow that stays on the ground.
- Stopping eases the travel speed and motion blend back to a planted contact pose.
- A loop lasts about 12 seconds. A roadside parcel appears once, followed by a
  fog-covered swap between the red-lamp street and drowned archive districts.
- The pickup is optional and has no miss penalty. Hold the hook input to extend it,
  then release while its tip overlaps the parcel's timing rings.
- `PERFECT` adds an identified tier-one item to the existing run `lootBag`; `GOOD`
  adds an unidentified item with one durability lost. A scene opened without a live
  run records preview loot only.

## Controls

- `F1`: fixed courier plus four-layer parallax (default)
- `F2`: previous cross-screen travel presentation
- `F6`: mini courier, two poses plus programmatic motion (default)
- `F7`: mini courier, four poses plus programmatic motion
- `F8`: mini courier, the same two poses without programmatic motion
- `F9`: previous Kain eight-pose comparison
- Use the upper-right buttons when keyboard focus is unreliable.
- `Space`: walk / ease to a stop and land
- `E` or left mouse: hold to extend the pickup hook, release to resolve
- `R`: restart the travel loop in the first district
- `T`: immediately test the fog/biome transition
- `Escape`: return to the main game scene

The parallax prototype art is stored under
`Assets/Resources/Art/JourneyPrototype/Parallax` and is separated by production role.
The drowned district replaces only the far and mid layers; ground and foreground
remain shared and are cool-tinted, which is the intended low-asset-cost pattern.
Adjacent-pose crossfading remains rejected because it creates a visible double image.
No bone rig, mesh deformation, or body-part separation is used in this test.

## Acceptance check

Evaluate silhouette consistency, foot sliding, backpack stability, coat-tail motion,
frame-to-frame identity, and whether the character remains readable against the city.
The generated walk cycle is prototype art: if motion is viable, production sheets should
be authored with locked character references and corrected contact poses.
