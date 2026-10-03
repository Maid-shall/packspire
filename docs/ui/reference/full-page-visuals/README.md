# PACKSPIRE Full-page Visual References

These images define composition and visual hierarchy for non-home screens. The
current home screen remains the primary brand reference. Generated Japanese copy
is illustrative; implementation copy and game data remain authoritative.

## Screen map

| File | ScreenId / purpose |
| --- | --- |
| `01-character-roster.png` | `Character` |
| `02-status-role-dossier.png` | `Status`, `Role` |
| `03-faction-contract.png` | `Faction` |
| `04-expedition-dispatch.png` | `Expedition` |
| `05-packing-manifest.png` | `Pack` |
| `06-vault-archive.png` | `Vault` |
| `07-heirloom-provenance.png` | `Heirloom` |
| `08-compendium-entry.png` | `Compendium` |
| `09-gridboard-route.png` | `GridBoard` |
| `10-battle-docket.png` | `Battle` |
| `11-shop-counter.png` | `Shop` |
| `12-reward-claim.png` | `Reward` |
| `13-event-casefile.png` | `Event` |
| `14-game-over-report.png` | `GameOver` |
| `15-game-clear-receipt.png` | `GameClear` |

## Shared visual contract

- Treat each screen as a different form in one infernal courier archive, not as
  an independent UI theme.
- The large left navigation reel belongs to `Hub` only. Management screens use
  their own Back button and an on-demand menu; do not reproduce the Hub reel.
- `Character` selects an existing courier from the roster. It is not a character
  creator and must not expose appearance-editing controls.
- Use matte obsidian, ivory paper, vermilion selection/action, tiny cyan
  registration marks, and thin brass rules.
- Reserve large paper surfaces for actual documents, contracts, manifests, and
  receipts. Do not wrap every region in a decorative frame.
- Keep characters and equipment unframed; never expose a generated square image
  boundary behind a focal illustration.
- Compact card state is a retained receipt stub. Expanded card state is a full
  postal docket in a stable reading lane. The wax seal spans the stub and main
  ticket.
- LINK is postal routing/authorization. Occupied cells are cargo volume. Color is
  route registration. These meanings must stay consistent across screens.
- Each equipment instance has one occupied shape. Battle and exploration may
  grant different docket content, but never use different occupied shapes.
- The active storage core is currently a `6x4` board (24 cells). Do not infer
  weight, carrying-capacity, or other new mechanics from illustrative labels.
- Match information hierarchy and proportions, not generated placeholder text.
- Implement at the current `1280x720` logical coordinate system and follow
  `UI_IMPLEMENTATION_RULES.md` and `UI_ARCHITECTURE.md`.
