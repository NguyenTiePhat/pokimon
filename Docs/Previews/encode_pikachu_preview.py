"""Encode Unity-rendered frames and enlarge original source rows for visual QA."""
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[2]
sheet = Image.open(root / "Assets/Resources/Pikachu/Walk-Anim.png").convert("RGBA")
chart = Image.new("RGB", (600, 400), "#24313d")
draw = ImageDraw.Draw(chart)
for row in range(8):
    x, y = (row % 4) * 150, (row // 4) * 200
    draw.text((x + 8, y + 8), f"Source row {row}", fill="white")
    cell = sheet.crop((0, row * 40, 32, (row + 1) * 40)).resize((128, 160), Image.Resampling.NEAREST)
    chart.paste(cell, (x + 11, y + 25), cell)
chart.save(root / "Docs/Previews/Pikachu-SourceRows.png")

paths = sorted((root / "Docs/Previews/PikachuSequence").glob("*.png"))
if paths:
    frames = [Image.open(path).convert("RGB") for path in paths]
    timings = [40 if i % 6 != 5 else 50 for i in range(len(frames))]
    timings[-1] += 600
    frames[0].save(root / "Docs/Previews/Pikachu-Entrance.gif", save_all=True,
                   append_images=frames[1:], duration=timings, loop=0, optimize=True)
    print(f"Encoded {len(frames)} Unity frames.")
