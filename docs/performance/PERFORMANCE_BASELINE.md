# PACKSPIRE performance baseline

Target: Windows and WebGL, 60 fps, 1280x720 logical UI at a 1920x1080 Game View.

Capture the following custom markers in a Development Build or the Unity Profiler.
Use a 600-frame sample after 10 seconds of warm-up and report median, 95th percentile,
maximum, and GC Alloc per frame.

| Marker | Initial budget |
|---|---:|
| `Packspire.Journey.Update` | 0.50 ms |
| `Packspire.Journey.Environment` | 0.30 ms |
| `Packspire.Journey.MiniGame` | 0.20 ms |
| `Packspire.Journey.Battle.Refresh` | 1.00 ms per refresh |
| `Packspire.Battle.Refresh` | 1.00 ms per refresh |
| UI GC Alloc during passive travel | 0 B/frame |

Required stress captures:

- Passive journey at x1 and x2 speed.
- Rain, mist, and close foreground enabled.
- Each roadside mini-game for at least 20 seconds.
- Battle with 5 and 10 cards, one and three enemies, and five status effects.
- Open and close draw/discard overlays repeatedly.

Do not replace SpriteRenderer weather with ParticleSystem based on estimates alone.
Reconsider rain only if its marker exceeds 0.30 ms on target WebGL hardware or the
active environmental element count exceeds 200.
