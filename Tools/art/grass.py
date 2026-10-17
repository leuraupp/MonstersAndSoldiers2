"""Grass tuft shown on a board cell marked with the right mouse button.

    python Tools/art/grass.py

Outputs (Assets/Art/Board/Original), 32x32 frames like the characters:
    Grass_Grow.png   5 frames: sprouts, overshoots and settles (played once when placed)
    Grass_Sway.png   8 frames: blades and flowers rocking in the wind (loops)
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from pixel import Canvas, color, ground_shadow, save_sheet, gif, preview, OUT_DIR, PREVIEW_DIR

S = 32
GROUND = 26
BOARD_DIR = os.path.join(OUT_DIR, "Board", "Original")

# Clumps of blades: (base x, base y, [(dx, height, lean), ...]); lean = tip offset in px.
CLUMPS = [
    (10, GROUND, [(-3, 6, -3), (-1, 9, -2), (0, 11, 0), (1, 8, 2), (3, 5, 3)]),
    (17, GROUND + 1, [(-3, 8, -2), (-1, 12, -1), (0, 14, 0), (1, 13, 1), (2, 9, 2), (4, 6, 3)]),
    (24, GROUND, [(-3, 5, -3), (-1, 8, -1), (0, 10, 1), (2, 7, 3)]),
]
# Flowers ride on a stem: (base x, base y, height, lean, kind)
FLOWERS = [(13, GROUND, 10, -1, "daisy"), (22, GROUND, 8, 2, "bell")]


def blade(cv, bx, by, height, lean, sway, ramp_top):
    """One blade, 1 px wide, bending more toward the tip; base dark, tip light."""
    if height <= 0:
        return None
    tip = None
    for i in range(int(round(height)) + 1):
        t = i / max(1.0, height)
        x = bx + lean * t * t + sway * t * t
        y = by - i
        k = 2 if t < 0.3 else 3 if t < 0.7 else ramp_top
        cv.set(x, y, color("grass", k))
        tip = (x, y)
    return tip


def flower(cv, x, y, kind):
    x, y = int(round(x)), int(round(y))
    if kind == "daisy":
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            cv.set(x + dx, y + dy, color("white"))
        cv.set(x, y, color("gold", 3))
    else:  # little red bell hanging from the stem
        cv.rect(x - 1, y, x + 1, y + 1, color("red", 2))
        cv.set(x - 1, y, color("red", 3))
        cv.set(x, y + 2, color("red", 1))


def tuft(scale=1.0, sway=0.0, flowers=True):
    cv = Canvas(S, S)
    if scale > 0.2:
        ground_shadow(cv, 17, GROUND + 1.5, 10 * min(1.0, scale), 1.8)
    body = Canvas(S, S)
    # dark mound the blades grow from
    w = 9 * min(1.0, scale + 0.2)
    for x in range(int(17 - w), int(17 + w) + 1):
        d = abs(x - 17) / max(1, w)
        h = 2 if d < 0.6 else 1
        for y in range(GROUND + 1 - h, GROUND + 2):
            body.set(x, y, color("grass", 1 if y == GROUND + 1 else 2))
    # back blades first, so the tall centre clump sits in front
    for ci, (bx, by, blades) in enumerate(CLUMPS):
        phase = sway * (0.8 + 0.2 * ci)
        for bi, (dx, h, lean) in enumerate(blades):
            top = 4
            blade(body, bx + dx, by, h * scale, lean * min(1.0, scale), phase * (0.6 + 0.1 * bi), top)
    if flowers and scale >= 0.7:
        for fx, fy, h, lean, kind in FLOWERS:
            tip = blade(body, fx, fy, h * min(1.0, scale), lean, sway * 1.1, 2)
            if tip:
                flower(body, tip[0], tip[1] - 1, kind)
    body.outline()
    cv.over(body)
    return cv


def frames():
    grow = [tuft(s, 0, flowers=s >= 0.7) for s in (0.3, 0.6, 0.9, 1.15)] + [tuft(1.0, 0)]
    sway = [tuft(1.0, 1.4 * math.sin(i / 8 * 2 * math.pi)) for i in range(8)]
    return grow, sway


def main():
    grow, sway = frames()
    os.makedirs(BOARD_DIR, exist_ok=True)
    save_sheet(grow, os.path.join(BOARD_DIR, "Grass_Grow.png"))
    save_sheet(sway, os.path.join(BOARD_DIR, "Grass_Sway.png"))
    preview([("Grow", grow), ("Sway", sway)], os.path.join(PREVIEW_DIR, "Grass_preview.png"), scale=6)
    gif(grow + sway * 2, os.path.join(PREVIEW_DIR, "Grass.gif"), ms=120)


if __name__ == "__main__":
    main()
