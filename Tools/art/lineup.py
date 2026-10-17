"""Side-by-side lineup of every character (preview only)."""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
from pixel import preview, gif, PREVIEW_DIR
import slime
from characters import ALL
from humanoid import render, poses

lineup = [("Down", [slime.anims("Down")["Idle"][0]] + [render(s, "Down", poses(s)["Idle"][0]) for s in ALL.values()]),
          ("Right", [slime.anims("Right")["Idle"][0]] + [render(s, "Right", poses(s)["Idle"][0]) for s in ALL.values()]),
          ("Up", [slime.anims("Up")["Idle"][0]] + [render(s, "Up", poses(s)["Idle"][0]) for s in ALL.values()])]
preview(lineup, os.path.join(PREVIEW_DIR, "Lineup.png"), scale=6)
# one gif with the four attacks side by side
from pixel import Canvas
atk = [slime.anims("Right")["Attack"]] + [[render(s, "Right", p) for p in poses(s)["Attack"]] for s in ALL.values()]
frames = []
for i in range(6):
    c = Canvas(32 * 4, 32)
    for k, a in enumerate(atk):
        c.over(a[i], 32 * k, 0)
    frames.append(c)
gif(frames, os.path.join(PREVIEW_DIR, "Lineup_Attack.gif"), scale=5, ms=120)
