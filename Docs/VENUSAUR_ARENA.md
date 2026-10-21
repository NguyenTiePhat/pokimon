# Venusaur — ancient overgrown garden

Stage 3 is freely selectable in the boss catalog. Venusaur has 750 HP.

| Move | Damage | Cooldown | Behavior |
| --- | --- | --- | --- |
| Razor Leaf | 10 per leaf | 2.8s | 0.42s warning; five leaves spaced 20 degrees apart, 5.8 units/s, fixed fan direction and dodgeable gaps |
| Leech Seed | 6 per pulse, 30 maximum | 5.6s | 0.6s warning with seed arcs; radius 1.3 at a fixed target; five pulses 0.6s apart, first 0.3s after landing; expires at 3.2s |
| Vine Whip | 25 once | 4s | 0.49s warning; sweeps a 130-degree sector over 0.55s, reach 3.2; rear and out-of-range positions are safe |
| Solar Beam | 60 once | 12.8s | 1.24s charge at the flower, then 0.8s beam, locked direction, range 16 and half-width 0.8 |

Faster combat balance updated on 2026-10-21. Recovery is 0.6s; approach speed is 1.2 units/s.
Leaves and Solar Beam use the existing opposing-energy clash system. Vines and
ground patches do not. No extra damage is added by clash explosions.

The garden uses muted stone paving, moss seams, perimeter roots, ruined pillars
and large pink flowers outside the playable circle (radius 8.4). Pollen is sparse
and remains at the edges. Seed patches carry their own marked soil circles;
there are no unannounced environmental damage zones or view-obscuring fog.

Sprites: PMDCollab species 0003, original credit CHUNSOFT, with eight-direction shadow anchors. Charge playback runs at 4/3 speed to match the shorter windup. Source archive and extracted originals
are in Docs/Venusaur-PMDCollab.zip and Docs/VenusaurSource; credit is retained in
Assets/Resources/VenusaurBoss/credits.txt.

Editor commands: Pokemon/Create Venusaur Garden and Pokemon/Check Venusaur Boss.
Checks cover warnings, damage, dodges, locked targets, expiry, spell clashes,
victory cleanup and circular bounds. They run in an additive test scene.

Validation: Unity passed 23 combat assertions and an actual Play Mode flow covering
menu entry, arena spawn, entrance, casting, victory and retry. Logs and rendered
screenshots are in Docs/Previews/VenusaurCheck.log, VenusaurPlayMode.log and
Venusaur-PlayMode-HUD.png.
