# Boss selection

The main menu reads `Assets/Resources/BossCatalog.json`. Each page holds four
cards and automatically offers previous/next buttons when more bosses are added.
All implemented stages are available immediately.

To add a boss, append an entry to `bosses`: name, arena label, scene name,
portrait Resources path, accent HTML color, and portrait sheet frameWidth/frameHeight.
The portrait uses the top-left frame. Register the implemented scene in Build
Settings. A missing/unregistered scene shows a disabled “SẮP RA MẮT” button.
Adding a card does not implement the boss's scene or combat behavior.

Card ordering follows the JSON array. No menu layout changes are required.
