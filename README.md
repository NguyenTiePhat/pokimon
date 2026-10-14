# Pokémon Boss Arena — Pikachu vs Charizard

Unity 6000.5.7f1. Open this folder through Unity Hub, open
`Assets/Scenes/MainMenu.unity`, and press Play.
If the scene is missing, use **Pokemon > Create Main Menu Scene** once.

Implemented: responsive 16:9 main menu, Charizard/Blastoise cards, start opens stage 1,
volume preference, and quit confirmation. Escape closes dialogs. Quit stops Play
Mode in the Editor. Stage 1 now starts a 500-HP Charizard boss fight with four
telegraphed attacks, player damage, victory/defeat and retry. See
`Docs/CHARIZARD_BOSS.md` for the approved balance and controls. The “Luyện tập”
panel switches to the training dummy or back to Charizard with a fresh scene.
No stage-selection map, music, or saved progression yet.
Pikachu flashes and emits impact sparks when hit. Taking 25 damage within 2s
causes a 0.8s stun with orbiting stars, followed by 2s stun protection. A stun
interrupts a casting ability, sets its cooldown to 2s, and refunds no resources.
Opposing energy attacks now clash: equal strength cancels both; the stronger
attack continues with reduced damage. Contact explosions are visual only.
See `Docs/SPELL_CLASHES.md` for Fire Spin and Thunderbolt interactions.

Stage 1: `Assets/Scenes/CharizardArena.unity` contains a grey brick arena matching
the supplied sketch, gold court markings and ten animated decorative fires.
Use the top-right button or Escape to return to the menu. Fire does not deal damage.
The arena is now 3x wider and taller. Hold the middle mouse button and drag to
pan the camera; camera bounds prevent panning beyond the map. The initial view
shows the center area instead of fitting the entire arena onto the screen.
The floor and flame sprites use point filtering and uncompressed pixel textures.
`Pokemon > Create Charizard Arena` regenerates this prototype scene and its art;
it replaces manual edits to that scene. Preview: `Docs/Previews/CharizardArena.png`.

Pikachu now enters stage 1 from the left, trails electricity while running, and
charges and releases branching pixel lightning at the center circle. After the 3.65-second entrance, use
WASD to move in eight directions. The replay button restarts the entrance;
camera dragging is paused during it. After arrival, the camera follows Pikachu's
ground position continuously, including dashes and Iron Tail, with map-edge bounds.
Hold middle mouse to look around temporarily; release to resume following.
E Thunderbolt, Q Quick Attack, C Iron Tail, X Electro Ball,
and Space dash are now available.
Left click fires a small electric ball toward the cursor (5 damage, 5 mana, 1s cooldown).
See `Docs/PIKACHU_ABILITIES.md` for controls, balance, training buttons and permanent crater behavior.
The ability HUD is four compact icons; the lightning count is below X. Open
“Luyện tập” for dummy controls/refills. Hold E to see its 4-unit range and target
circle without changing the camera. Press C to automatically attack a nearby
target within 1.8 units, regardless of mouse position or facing direction.
Direction rows are S, SE, E, NE, N, NW, W, SW (verified against the source art).
Walk cadence follows actual distance; diagonal movement has the same speed.
Shadow markers anchor the animation; idle holds the grounded first pose, keeping
the last facing direction. Movement stops at the court bounds.
`Pokemon > Preview Pikachu Entrance` runs movement/intro regression checks and
renders four images plus a 24 fps sequence under `Docs/Previews`.
`Docs/Previews/encode_pikachu_preview.py` encodes that sequence as a GIF using Pillow.
Pikachu source selected by the user: https://sprites.pmdcollab.org/#/0025?form=0
Walk/Idle/Charge sheets, shadow anchors and XML are imported from that page's
download: https://spriteserver.pmdcollab.org/assets/0025/sprites.zip (2026-10-13).
The original download is retained at `Docs/Pikachu-PMDCollab.zip`.
The site serves the same SpriteCollab collection; see `Docs/PIKACHU_SOURCE.md`.
Original contributor credits are preserved in `Assets/Resources/Pikachu/credits.txt`.

UI uses a self-contained IMGUI prototype in `Assets/Scripts/MainMenu.cs`.
Blender is not needed for this sprite-based menu.

Temporary sprites: PMDCollab/SpriteCollab, `sprite/0006` and `sprite/0009`,
Idle-Anim.png and AnimData.xml, downloaded 2026-10-13.
Source: https://github.com/PMDCollab/SpriteCollab
Pokémon characters belong to their respective owners. Repository availability
does not imply a commercial license. Preserve source attribution when replacing assets.
