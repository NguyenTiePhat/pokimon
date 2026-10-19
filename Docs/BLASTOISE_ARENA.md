# Stage 2: flooded water temple

Approved prototype balance (2026-10-19): Blastoise 650 HP, Pikachu 200 HP.

| Ability | Damage | Cooldown | Warning / behavior |
| --- | --- | --- | --- |
| Hydro Pump | 20, once per cast | 4s | 0.45s initial-lane warning, then two cannon jets converge into a 2.4s water beam; tracks at up to 40 degrees/s; range 10, half-width 0.55 |
| Water Pulse | at most 24 | 6s | 0.35s warning, a 4.5 units/s orb aimed at a fixed point up to 8 units away; direct hit 12, expanding ring 12 |
| Rapid Spin | 25, once per cast | 8s | 0.45s directional warning, then travels 4 units in 0.65s; dodge sideways |
| Hydro Cannon | 60, once per cast | 18s | 1.1s warning, then a 0.8s mouth-fired tracking jet; range 18, warning half-width 0.65; tapered radius 0.13–0.5, gently moving curved centerline shared by visuals and collision |

Hydro Pump holds its warning direction, then turns toward Pikachu during firing;
keep moving sideways or dash past the beam to outrun its limited rotation.
Hydro Cannon tracks at up to 65 degrees/s during firing; its trailing heading follows with a 0.32s response time, bending the stream as it turns. Water Pulse and Rapid Spin lock their aim at cast start. Boss movement stops while warning
or casting, except the committed Rapid Spin movement. Water Pulse's wave expands
from radius 0.2 to 1.8 over 0.65s, hits a character once as the ring passes, and
can be avoided even after dodging the projectile. A pulse canceled by a spell
clash produces no damaging wave. Hydro Pump and Hydro Cannon participate in
the existing strength-based clash rules; Rapid Spin is melee and does not.

Hydro Pump originates at two shoulder cannon sockets and joins
1.35 units ahead of the boss. Both narrow branches and the main beam participate
in damage and spell clashes. Foam, moving white water streaks and spray show
the flow. Hydro Cannon charges at the mouth and releases one narrower, smoothly
tapered water stream, with moving highlights and foam at the tip. The initial
warning shows the starting aim; during firing the curved stream tracks the player.
Water Pulse charges at the mouth before its orb travels to the marked
point; Rapid Spin originates from the rotating shell itself.

The stage is a circular stone court beside an ancient shrine, surrounded by a
blue lake, shallow radial channels and broken columns outside the walkable ring.
The center stays open. Ground movement and dashes clamp to radius 8.4; there is
no global slipping or slowing. Reflections, ripples and droplets animate on the
water. Four fountain tiles at (±6,0)/(0,±6) activate one at a time: each 10-second
cycle warns for 1.5s, then erupts for 1s, dealing 5 damage at most once to Pikachu
within 0.85 units. They pause during entrance and after battle ends.

Both stages can be selected freely from the main menu, without a previous win
or saved progress. The Charizard victory screen also offers “MÀN 02”.
Replay, retry, menu, training and return-to-boss
flows retain the current arena; a retry resets HP, casts, waves and crater state.
The Unity scene `Assets/Scenes/BlastoiseArena.unity` can also be opened directly
for testing. Both battle scenes are registered in EditorBuildSettings.

Art: original PMDCollab base-form Blastoise (0009), downloaded from
https://spriteserver.pmdcollab.org/assets/0009/sprites.zip . Archive retained as
`Docs/Blastoise-PMDCollab.zip`; contributor credits retained in
`Assets/Resources/BlastoiseBoss/credits.txt`. Idle/Walk/Charge/Shoot/Attack/Rotate
use the original shadow anchors. The water temple, water effects, markers and
fountains are project-generated pixel art. This source has no standalone Faint
sheet, so a defeated boss is dimmed rather than substituting an unrelated sprite.

Checks: `BlastoiseBossCheck.Run` (30 deterministic assertions and ability previews),
`WaterTemplePlayModeCheck.RunAll` (old ability/clash/stun regressions plus stage
unlock, both scenes, HUD captures, dodge, retry and training flows). Tests suppress
saving progression so they do not change the player's persisted unlock state.
Use `-waterVisualCheck` without `-batchmode` to capture the real Game View.

No boss mana/stagger or audio has been added in this step.

Validation completed in Unity 6000.5.7f1: 201 assertions passed (30 Blastoise,
100 Pikachu, 29 Charizard, 22 hit/stun and 20 spell-clash checks). Real Play Mode
passed stage-1 victory/unlock, stage-2 spawn and floor rendering, warning and safe
side dodge, Pikachu damage, defeat/retry, water victory, and training round-trip.
Logs: `Docs/Previews/WaterTempleFinalCheck.log`, `WaterTemplePlayMode.log`.
Reviewed images: `Blastoise-Temple-Overview.png`, `WaterTemple-Warning-HUD.png`,
`WaterTemple-Cannon-HUD.png` in `Docs/Previews`.
