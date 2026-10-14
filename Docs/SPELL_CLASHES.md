# Opposing spell clashes

User approved: compare strength; equal attacks both disappear, stronger attacks
continue with reduced strength. The clash explosion is visual only, without extra
area damage or a crater. Q, Iron Tail and Dragon Claw do not clash.

Strength uses remaining damage: basic electricity 5, each flame 5, each air blade
12, Thunderbolt 20, Electro Ball 20–100 depending on charge. Contact consumes the
same amount from both attacks. Example: full Electro Ball 100 versus Air Slash 12
leaves an 88-damage ball. Basic electricity 5 versus flame 5 cancels both.

Fire Spin is one continuous opposing stream with initial strength 48 (6 × 8),
not 19 separate collisions for its decorative rings. Its remaining strength / 6
is the damage of each future tick. A 5-strength hit leaves 43 strength and future
ticks deal about 7.17 damage. A full 100-strength orb destroys the stream and
continues with 52 damage. Depleting the stream ends its active cast; its cooldown
still applies. The warning phase cannot clash.

Thunderbolt checks obstacles once at discharge, in path order. It still tracks
the selected enemy, but the new clash rule allows opposing energy to weaken or
block it. The rendered beam stops at the blocking contact. Its visual afterglow
does not repeatedly collide. Released projectiles retain their original heading.

Both projectile sides advance together in `SpellClash.Step`, called by the boss
simulation. Continuous swept collision resolves the earliest contact (spell,
character, or expiry) before later events, including crossings within one frame.
Clashes use the arena ground plane, consistent with existing attack hitboxes;
sprite flight height remains a presentation effect. Friendly shots do not clash.

Explosion size scales with the strength consumed at contact. A canceled ultimate
does not create a crater; a surviving ultimate still creates one when it hits or
lands. Mana, lightning costs and cooldowns are not refunded by a clash.

Validation: `SpellClashCheck.Run`; runtime integration is covered by
`CharizardPlayModeCheck.Run`. Preview: `Docs/Previews/Spell-Clash.png`.

Verified in Unity 6000.5.7f1: 20 clash assertions, 100 Pikachu ability assertions,
29 boss assertions and 22 hit/stun assertions passed. Play Mode also exercised
an equal-strength clash between live projectiles without damage to either actor,
then continued through boss attacks, stun, victory/defeat and training switch.
Logs: `Docs/Previews/SpellClashCheck.log` and `SpellClashRegression.log`.
