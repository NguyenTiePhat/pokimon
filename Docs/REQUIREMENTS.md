# Agreed project requirements

- Unity 6000.5.7f1; offline, keyboard and mouse. Blender available for asset work.
- Pixel-art 2D elevated top-down view, as in the supplied Pokémon Arena screenshot.
- Real-time boss fights only; no small enemies.
- Connected stage map inspired by Plants vs. Zombies 2.
- Two bosses: Charizard first, then Blastoise unlocked by winning the first fight.
- Each boss has a themed arena, HP, mana, and its own attacks.
- Player: WASD, 200 HP, 100 regenerating mana.
- Space dashes in movement direction, costs mana, grants no damage immunity.
- E/Q/C regular abilities; X ultimate. Pikachu uses a 15-lightning meter:
  Thunderbolt +3, Quick Attack +2, Iron Tail +5 per completed cast; X consumes 15.
- Hold E to aim while moving; left click casts at the selected target. C immediately
  auto-selects a nearby enemy within 1.8 units; Q immediately applies its self buff.
- Releasing the ability key before clicking casts in the character's facing direction.
- Casting locks movement, has a character animation, and prevents dashing.
- Opposing ranged energy spells clash by remaining damage strength. Equal attacks
  cancel; the stronger continues with the difference. Contact explosions have no
  extra damage or crater. Fire Spin and Thunderbolt participate; Q and melee do not.
- Each ability has independently adjustable cooldown and balance parameters.
- Boss locks aim/target on starting its cast; projectile tracking is per ability.
- Boss attacks must be dodgeable.
- Both sides can stagger after sufficient damage within a short window;
  stagger does not prevent incoming damage.
- Stagger interrupts a cast; the interrupted ability receives a 2-second cooldown.
- Player Pokémon confirmed: Pikachu. On arena entry, run in from the left to the
  center circle with electric trails, charge, and branching pixel lightning, then enable WASD.
  Use Pikachu form 0 from https://sprites.pmdcollab.org/#/0025?form=0.
  Verify directions against source art (rows S, SE, E, NE, N, NW, W, SW).
  Keep facing on stopping, anchor feet using source shadows, and synchronize
  walking to distance travelled; camera follows Pikachu continuously after arrival.
- Pikachu ability kit and test controls: see PIKACHU_ABILITIES.md. Thunderbolt 20
  damage confirmed; cooldown/mana/lightning and all four effects implemented.
- Compact ability HUD: four small square icons at the bottom; lightning count
  beneath X only. E aiming shows its 4-unit range and a cyan double-ring target marker;
  C shows the automatically selected target during its cast. Camera stays unchanged,
  following the supplied screenshot. Cast times: E 0.35s, Q 0.08s, C 0.35s, X 1.5s.
- Charizard: 500 HP. Flamethrower 5 damage per puff / 1s cooldown; Dragon Claw
  25 damage / 8s; Air Slash 12 per blade / 5s; Fire Spin 8 per 0.5s for 3s / 15s.
  Fire Spin is an expanding spiral stream from the mouth, matching the reference.
  All four attacks lock direction during their warning and can be dodged.
- Pikachu hit/stagger: red-white flash and impact sparks; 25 damage within 2s
  stuns for 0.8s with orbiting stars, then 2s stun protection. Still takes damage.
  Interrupted cast cooldown becomes 2s, without refunding mana or ultimate charge.
- Unresolved: Blastoise abilities, boss stagger thresholds/durations, final assets and effects.
- Current camera: partial-arena view, continuous player following with map-edge bounds.
  Middle-mouse pan temporarily overrides following while held; release resumes it.
  Intro centers and locks the camera until the arrival ends. Abilities do not zoom.
- Stage 1 environment: rectangular grey small-brick floor; gold perimeter, halfway
  line, center circle and center spot, inward-facing semicircles at either end.
  Sparse animated decorative fires establish Charizard's fire theme.
- Arena expanded to three times its original width and height (nine times the area).
  Hold the middle mouse button and drag to pan; the map follows the cursor.
  Camera movement is bounded by the map; initial view shows only part of the arena.
- Current authorized implementation: main menu, stage 1 environment, and Pikachu
  player with entrance, WASD/dash, four abilities, mana/lightning HUD and a training
  dummy with movement/immunity toggles. Electro Ball leaves visual craters until
  restarting the arena. Decorative map fire is harmless. Charizard AI, attack
  damage to Pikachu, boss HP, victory/defeat and retry are now implemented.
