"""Regenerates every original sprite of the game.

    python Tools/art/build_all.py

Needs Pillow (pip install pillow). Writes:
    Assets/Art/Characters/<Slime|Paladin|Goblin|Orc>/<Name>_<Idle|Attack|Hit|Death>_<Down|Left|Right|Up>.png
    Assets/Art/UI/Original/*.png
    Tools/art/preview/*  (preview boards and GIFs, not used by the game)

Then, in Unity: Tools > Art > Rebuild Generated Animations (clips, controllers and GridCell prefab).
Import settings (Point, no compression, sliced) come from Assets/Editor/Art/GeneratedArtImporter.cs.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import slime
import characters
import ui
import title
import lineup  # noqa: F401  (builds the lineup preview on import)

if __name__ == "__main__":
    slime.main()
    for spec in characters.ALL.values():
        characters.build(spec)
    ui.main()
    title.build()
