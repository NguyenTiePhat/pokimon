# Gengar — Sân phế tích ma ám

Stage 4 is freely selectable from the paginated main menu. Venusaur's victory
panel also leads to this arena. Open `Assets/Scenes/GengarArena.unity` to play.
Gengar has 800 HP. Existing Pikachu controls, training, retry and camera follow apply.

The circular moonlit brick court uses muted blue and purple paving. Broken arches,
old tombs and decorative shadow pools stay outside the playable radius (8.4 units).
Faint animated mist stays at the perimeter, below all attack warnings.

| Attack | Warning | Damage | Cooldown | Counterplay |
| --- | --- | --- | --- | --- |
| Shadow Ball | 0.53 s, straight lane | 22 | 2.8 s | Step sideways; direction locks at cast start. Projectile travels at 6.5 units/s and participates in energy clashes. |
| Sludge Bomb | 0.9 s, gold ring and arcing bomb | 18 impact; 6 every 0.6 s in poison | 5.6 s | Leave the locked 1.45-unit circle. Poison lasts 3.2 s; first tick comes 0.6 s after impact. |
| Shadow Punch | 0.75 s, cyan ring | 28 | 4.8 s | Leave the locked 1.25-unit circle before Gengar reappears. |
| Phantom Force | 1.35 s, large cyan ring | 55 | 13.6 s | Leave the locked 3.2-unit circle before the strike. |

Ghost attacks hide Gengar after the first 0.25 s of warning; hidden Gengar cannot
take damage. The fixed outer ring, crosshair and radial marks remain visible
throughout the warning; an inner shrinking ring shows the countdown.
The boss reappears at the marked point and each strike damages only once.
Poison has a yellow-green border and bubbles, distinct from decorative darkness.
All attacks and poison clear on victory, defeat or entrance replay.

Faster combat balance updated on 2026-10-21: recovery 0.68s, approach speed 1.2 units/s.

## Assets and validation

Gengar sprite sheets and animation/shadow metadata: PMDCollab/SpriteCollab,
`sprite/0094`, downloaded 2026-10-21 from
https://github.com/PMDCollab/SpriteCollab/tree/master/sprite/0094 .
Original contributor attribution is retained in
`Assets/Resources/GengarBoss/credits.txt`.
The arena and telegraphs are rendered with the project's native pixel drawing tools.

`GengarSetup.RunAll` regenerates the scene, captures the overview/attack previews,
and checks locked targets, warnings, hits, dodges, poison expiry, disappearance,
battle cleanup and movement bounds.
`GengarSetup.RunPlay` checks menu-to-scene loading, entrance, casting, victory and retry.
