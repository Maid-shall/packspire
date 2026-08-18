# Ash City background set

`ash-standard-master-v1.png` is the approved-dimensions composition master for the
standard-road Gate. It is not a production parallax layer and must not be shipped as
the final flattened background.

Production layers derived after composition review:

- `ash-sky-day-v1.png` / `ash-sky-dusk-v1.png` /
  `ash-sky-night-v1.png`: fixed opaque sky plates used for progress-based blending
- `ash-far-silhouette-a-v1.png` / `ash-far-silhouette-b-v1.png`: alternating
  low-contrast distant city silhouettes
- `ash-mid-architecture-a-v1.png` / `ash-mid-architecture-b-v1.png`: alternating
  medium-distance architecture
- `ash-streetback-a-v1.png` / `ash-streetback-b-v1.png`: alternating anchored
  parapet, rail and sparse lamp layers
- `ash-ground-standard-tile-v3.png`: horizontal side-scroll road with a shallow
  walkable top face and no central vanishing point
- `ash-mid-wide-a-v1.png` / `ash-streetback-wide-a-v1.png` /
  `ash-ground-wide-tile-v2.png`: gatefront and inspection-plaza profile
- `ash-mid-narrow-a-v1.png` / `ash-streetback-narrow-a-v1.png`: dense
  maintenance-passage architecture. Runtime currently combines these with the
  standard stone road so the courier has an unambiguous walkable surface.
- `ash-ground-narrow-tile-v2.png`: archived high-bridge cross-section experiment.
  Its rail and understructure did not read clearly as ground, so it is not assigned
  to the current Catalog. Replace it only with a dedicated narrow street or an
  unmistakable iron-grate walking surface.

`*-scroll-source-*` files retain the generated source used by the horizontal-tile
tool. Runtime code references only the `*-tile-*` files. The atmosphere veil remains
separate and must not hide the skyline or courier silhouette.

All layers follow
`docs/concepts/seamless-journey-v1/JOURNEY_BACKGROUND_ART_SPEC.md`.

## Master generation prompt

Use case: stylized-concept. Production composition master for a Unity 2D
side-scrolling background. Match the existing dark gothic-industrial Ash City art:
black iron, soot, narrow red-orange window light, muted teal oxidation, and a
hand-painted illustrated finish. Repaint every element as one cohesive environment.
Use an exact wide 16:9 side-on orthographic view with no UI or character. Keep the
playable ground contact line at 73.5 percent of image height. Show a standard-width
stone delivery street with more visible walkable depth than the old narrow bridge,
but not a plaza. Keep the road level across both edges. Leave the left third visually
quiet for the courier and put most architectural interest at center-right. Make the
smoky sky, distant skyline, middle architecture, street-back railings, and road slab
visually separable. Use weak cool daylight from upper-left, sparse warm windows,
lower contrast and saturation in distant layers. No people, creatures, carts,
standalone props, signs, text, logo, UI, checkerboard, frame, vignette, giant gate,
or obvious repetition. Keep both edges continuation-friendly.
