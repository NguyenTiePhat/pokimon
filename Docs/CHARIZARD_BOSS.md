# Charizard boss prototype

The arena now starts a boss fight by default. Pikachu has 200 HP, Charizard has
500 HP. Faster combat balance updated on 2026-10-21:

| Attack | Damage | Cooldown | Warning / execution |
| --- | --- | --- | --- |
| Flamethrower | 5 per fire puff | 0.8s | 0.3s warning, three straight puffs 0.12s apart |
| Dragon Claw | 25 once per dash | 6.4s | 0.6s lift with glowing claws, 0.5s dash up to 7 units |
| Air Slash | 12 per blade | 4s | 0.49s warning, three curved blades at -20/0/+20 degrees |
| Fire Spin | 8 each 0.5s, six ticks | 12s | 0.83s warning, 3s expanding spiral fire stream up to 7 units |

Fire Spin follows the user's reference image: a stream of expanding orange/red
fire rings coming from Charizard's mouth. It is not a circular trap centered on
Pikachu. The attack locks its direction at warning start. All attacks have ground
warnings; moving outside the locked path avoids damage. Dash grants no immunity.
Cooldowns count from warning start; the boss never casts two abilities at once.
AI rotates available attacks, rests 0.49s between casts and approaches if farther
than 4.5 units away at 1.92 units/s. Iron Tail's slow affects the boss's approach speed.

Pikachu's existing E/C targeting and electric projectiles damage Charizard.
The boss cannot attack during Pikachu's entrance. Victory or defeat stops the
fight, clears hostile projectiles, and offers a fresh attempt. The four-icon
player HUD remains compact. Open “Luyện tập” and choose “Chuyển sang hình nhân”
to return to the dummy; “Đấu Charizard” switches back with a fresh scene.

Sprites: PMDCollab / SpriteCollab, species 0006, base form.
Downloaded source archive: `Docs/Charizard-PMDCollab.zip` from
https://spriteserver.pmdcollab.org/assets/0006/sprites.zip .
Original contributor credits: `Assets/Resources/CharizardBoss/credits.txt` from
https://raw.githubusercontent.com/PMDCollab/SpriteCollab/master/sprite/0006/credits.txt .
Idle, Walk, Charge, Shoot, Attack and Faint sheets use source shadow anchors.
Fire rings, air blades, claws and warnings are generated in pixel art at runtime.

Pikachu now reacts to hits and staggers at 25 damage in 2s: a 0.8s stun, then
2s stun protection. See `PIKACHU_ABILITIES.md` for interruption rules.
This stage does not yet implement boss mana, boss stagger or Blastoise progression.
Validation entry points: `CharizardBossCheck.Run` and `CharizardPlayModeCheck.Run`.

Validation completed in Unity 6000.5.7f1: 29 boss assertions and 100 Pikachu
regression assertions passed. Play Mode covered entrance, E hitting the boss,
Fire Spin damaging Pikachu, victory, defeat, fresh retry and a training/boss
round-trip without runtime errors. Logs: `Docs/Previews/CharizardBossCheck.log`,
`CharizardPikachuRegression.log`, `CharizardFinalPlayMode.log`.
Real Game View capture: `Docs/Previews/Charizard-Battle-HUD.png`.
