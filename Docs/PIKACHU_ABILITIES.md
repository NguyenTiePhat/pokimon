# Pikachu training arena

User-requested kit implemented on 2026-10-14. Thunderbolt damage confirmed by
the user at 20. All balance fields are editable on the runtime PikachuCombat
component, grouped by ability.

Left mouse fires one small electric projectile toward the cursor: 5 damage,
5 mana, 1-second cooldown. It travels straight up to 6 units at 10 units/second,
can miss, and disappears on first hit or at maximum range. No lightning charge
or crater is generated. Movement remains available. Clicking HUD controls does
not fire; E aiming and X charging retain priority for their own left-click casts.

| Key | Ability | Mana | Cooldown | Effect | Lightning |
| --- | --- | --- | --- | --- | --- |
| E | Thunderbolt | 30 | 15s | 0.35s charge; targeted 20 damage, subject to immunity and opposing spell clashes | +3 |
| Q | Quick Attack | 30 | 20s | 0.08s cast; 5s run/dash speed x1.5, cast speed x1.3, thin cyan aura | +2 |
| C | Iron Tail | 20 | 10s | 0.35s leap/swing within 1.8 units; 25 damage and 40% slow for 2s | +5 |
| X | Electro Ball | 70 | 60s | Up to 1.5s charge; 20–100 damage, fixed flight destination, dodgeable, permanent crater | consumes 15 |

HUD revision requested 2026-10-14: four 56x56 icon squares at the bottom center,
with small E/Q/C/X labels, cooldown overlays, and charge progress. The lightning
count appears only below the X icon. Full ability text cards and the persistent
instruction/message panel have been removed. Invalid casts show a brief message.
The training controls are collapsed behind the “Luyện tập” button by default.
HP/mana and the dummy's health bar remain visible.

Holding E shows a cyan cast-range ring around Pikachu (4 world units)
and a double-ring ground reticle with inward markers, based on the supplied
reference image. The reticle snaps to a valid target's feet, turns red for invalid
selection, and is clamped to range elsewhere. Range visualization and eligibility
use the same settings. Aiming and casting never change camera zoom; the camera
continuously follows Pikachu's ground position, including the Iron Tail leap.
C shows its 1.8-unit range and automatically selected target during the cast.
No changes to damage, cooldown, mana or lightning requirements.

Thunderbolt: hold E to aim while moving; left click the dummy to
commit. Releasing without clicking selects a target in the facing cone. Invalid
target, range, mana or cooldown checks do not consume resources. Thunderbolt
locks a live target on starting its cast and follows that target even if it moves
away. Immunity is checked at damage time. Opposing spells can now weaken or block
the discharge; see `SPELL_CLASHES.md`. Press C to auto-select a live nearby
target regardless of pointer or facing direction, without a manual aiming step.
Iron Tail locks the attack location,
so a target that moves out of the landing area can avoid the hit/slow.

Q is self-targeted and begins immediately on key press. Recasting does not stack
the buff. Quick Attack increases casting speed, not damage or mana regeneration.

Holding X starts charging immediately and locks movement. Aim with the mouse;
left click releases toward the pointer. Releasing X throws in the facing
direction at the start of the charge. At maximum charge it throws toward the
current aimed point automatically. Damage scales linearly from 20 to 100 with
charge (50% = 60 damage). Destination is fixed when released, never homing.
A swept ground-path collision prevents fast projectiles passing through targets.
Hit or landing triggers one area-damage event and one crater, including on misses.

Mana and cooldown are committed when casting begins; regular abilities grant
their listed lightning on completion (not per hit or per VFX branch), capped at
15. Electro Ball consumes 15 at charge start. No additional damage-based charge
is awarded in this prototype, superseding the earlier provisional charge rule.

Space dashes in movement direction, falling back to facing when stationary.
Prototype dash balance: 10 mana, 0.16s at 11 units/s, 0.55s cooldown; no immunity.
Casts prevent walking/dashing; aiming E does not. Mana capacity is 100, with
8/s regeneration after a 2s delay from spending; no regeneration while casting.
The training dummy never attacks. In boss mode, Charizard damages Pikachu's
200 HP; reaching zero ends the fight. See `CHARIZARD_BOSS.md`.

Hit reaction and stagger (user approved): each damaging hit produces a 0.28s
red/white sprite flash and impact sparks. Taking 25 total damage in a rolling
2-second window stuns Pikachu for 0.8s, with a Hurt pose, three orbiting gold
stars and a visible countdown. Movement, dash, aiming and attacks are blocked.
Damage still applies. A stun interrupts the current cast and sets that ability's
remaining cooldown to 2s; spent mana and ultimate lightning are not refunded.
Interrupted abilities do not resolve or grant lightning. Already fired projectiles
continue flying. After the stun ends, 2s protection prevents another stun but does
not prevent damage; hits during stun/protection do not accumulate toward another
stun. Defeat, entrance replay and a fresh scene clear the stun state.

The dummy starts at (3,0) with 1000 HP. Toggle movement to test tracking/misses
and slow, or immunity to test blocked damage. A defeated dummy must be reset.
“Hồi hình nhân” restores only the dummy. “Nạp thử 15 sét” refills mana/lightning
and cooldowns for testing; neither removes craters. “Đánh lại” reloads the scene,
clearing all craters, projectiles, buff state, and training settings.

Crater holes are persistent visual terrain depressions, not collision pits or
fall hazards. They remain walkable; this request did not specify falling rules.
Replay of the entrance leaves existing craters. Leaving/reloading the arena
starts a new attempt with clean ground.

Art: original Pikachu Walk/Charge/Shoot/Swing frames and shadow alignment from
the already downloaded PMDCollab base-form package. The wooden straw dummy,
cyan aura, electric beam/orb, explosion, and craters are game-generated pixel art.
No third-party dependencies. Unity's built-in Screen Capture module is enabled
for visual QA of the actual HUD in Play Mode. Charizard combat is documented
in `CHARIZARD_BOSS.md`; player stagger is implemented, boss stagger remains pending.

Checks: PikachuCombatCheck (deterministic ability regressions and render previews),
PikachuPreviewCheck (movement/intro regressions), and Play Mode smoke tests.

Verified in Unity 6000.5.7f1: 67 combat/aim/HUD assertions passed, movement/intro
regressions passed, and all four abilities ran through real Awake/Update in Play
Mode. The smoke test also exercised menu cleanup, a fresh arena attempt and
entrance replay. Real Game View captures `Docs/Previews/Hud-Aim-E.png` and
`Hud-Auto-C.png` cover the compact HUD, ultimate counter, and target rings.
The current checks use `AutoTargetCheck.log` and `AutoTargetVisualCheck.log`.
ScreenCapture needs a rendered Game View, so run the visual smoke test with
`-pikachuVisualCheck` without `-batchmode`; ordinary batch mode skips screenshots.
Manual keyboard feel remains for user playtesting.

Hit/stun validation: `PikachuHitCheck.Run` passed 22 assertions covering the
rolling damage window, hit/stun visuals, input locks, damage during stun,
including 25 HP lost over 1.5s and at the exact 2s boundary,
protection expiry, E/X/dash interruption, and death/intro cleanup. Play Mode
also verified a real Dragon Claw hit activates stars and blocks controls, then
restores control with stun protection. Logs: `PikachuHitCheck.log` and
`PikachuStunPlayMode.log`; preview: `Docs/Previews/Pikachu-Stunned.png`.
