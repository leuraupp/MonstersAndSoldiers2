"""Slime: 32x32 frames, four facings (Down, Left, Right, Up).

    python Tools/art/slime.py            # writes Assets/Art/Characters/Slime/*.png
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from pixel import (Canvas, color, ground_shadow, sphere_light, ramp_pick,
                   save_sheet, preview, gif, OUT_DIR, PREVIEW_DIR)

S = 32
GROUND = 27
DIRS = {"Down": (0, 1), "Left": (-1, 0), "Right": (1, 0), "Up": (0, -1)}


def slime(cx, base, w, h, facing="Down", eyes="open", mouth=False, flash=False,
          alpha_drops=None, shadow=True):
    cv = Canvas(S, S)
    if shadow:
        ground_shadow(cv, cx, GROUND + 0.5, w * 0.5 + 1, 2.2)
    body = Canvas(S, S)
    top = base - h
    hw = w / 2

    def inside(x, y):
        if y < top or y > base:
            return False
        t = (base - y) / h  # 0 at floor, 1 at top
        half = hw * math.sqrt(max(0.0, 1 - t ** 2.4))
        return abs(x - cx) <= half

    def shade(x, y):
        if flash:
            return color("white")
        nx = (x - cx) / hw
        ny = (y - (top + h * 0.6)) / (h * 0.65)
        v = sphere_light(nx * 0.95, max(-1, min(1, ny)))
        if base - y < 1.6:  # contact line with the floor stays dark
            v = min(v, 0.25)
        return color("slime", 1 if v < 0.38 else 2 if v < 0.78 else 3)

    body.fill_shape(inside, shade)
    if not flash:
        # glossy highlight, upper-left
        hx, hy = cx - hw * 0.45, top + h * 0.28
        body.set(hx, hy, color("slime", 4))
        body.set(hx + 1, hy, color("slime", 4))
        body.set(hx, hy + 1, color("slime", 4))
        if w > 18:
            body.set(hx + 3, hy - 0.6, color("slime", 3))
    face(body, cx, base, w, h, facing, eyes, mouth, flash)
    body.outline()
    cv.over(body)
    return cv


def face(cv, cx, base, w, h, facing, eyes, mouth, flash):
    if facing == "Up" or flash:
        return
    dx = {"Down": 0, "Left": -1, "Right": 1}[facing]
    blk, wht = color("black"), color("white")
    ey = int(round(base - h * 0.62))
    if facing == "Down":
        xs = [int(round(cx - 4)), int(round(cx + 2))]
    else:  # profile: near eye big, far eye squeezed against the edge
        near = int(round(cx + dx * w * 0.2 - (1 if dx < 0 else 0)))
        xs = [near, near + dx * 4]
    tall = 3 if h >= 11 else 2
    for i, x in enumerate(xs):
        far = facing != "Down" and i == 1
        ww = 1 if far else 2
        if eyes in ("open", "angry"):
            cv.rect(x, ey, x + ww - 1, ey + tall - 1, blk)
            if not far:
                cv.set(x, ey, wht)
            if eyes == "angry":  # brow cuts the top inner corner
                inner = x + ww - 1 if (i == 0 and facing == "Down") else x
                if facing != "Down":
                    inner = x if dx > 0 else x + ww - 1
                cv.set(inner, ey, color("slime", 1))
                cv.set(inner + (1 if inner == x else -1), ey - 1, blk)
                cv.set(inner, ey - 1, blk)
        elif eyes == "closed":
            cv.rect(x, ey + 1, x + ww - 1, ey + 1, blk)
        elif eyes == "x":
            if far:
                cv.set(x, ey + 1, blk); continue
            cv.set(x - 1, ey, blk); cv.set(x + 1, ey, blk); cv.set(x, ey + 1, blk)
            cv.set(x - 1, ey + 2, blk); cv.set(x + 1, ey + 2, blk)
    if mouth:
        mx = int(round(cx - 1)) if facing == "Down" else xs[0] + dx * 2
        my = ey + tall + 1
        cv.rect(mx, my, mx + 2, my + 1, color("red", 0))
        cv.rect(mx, my + 1, mx + 2, my + 1, color("red", 2))
    elif eyes == "open" and facing == "Down" and h >= 11:
        cv.set(cx - 1, ey + tall, color("slime", 1)); cv.set(cx, ey + tall, color("slime", 1))


def sparks(cv, x, y, r, c=None):
    c = c or color("white")
    for a in range(0, 360, 45):
        k = 1 if a % 90 == 0 else 0.7
        cv.set(x + math.cos(math.radians(a)) * r * k, y + math.sin(math.radians(a)) * r * k, c)
        cv.set(x + math.cos(math.radians(a)) * (r + 1) * k, y + math.sin(math.radians(a)) * (r + 1) * k, c)


def droplet(cv, x, y, s=1):
    cv.rect(x, y, x + s, y + s, color("slime", 2))
    cv.set(x, y, color("slime", 3))


def anims(facing):
    fx, fy = DIRS[facing]
    cx = 16
    A = {}

    A["Idle"] = [slime(cx, GROUND, w, h, facing)
                 for w, h in [(20, 13), (21, 12), (22, 11), (21, 12)]]

    # lunge toward the facing direction, then splat on the target
    def at(off, lift, w, h, **kw):
        return slime(cx + fx * off, GROUND + fy * off * 0.5 - lift, w, h, facing, **kw)
    f = [at(0, 0, 24, 9, eyes="angry"),
         at(1, 3, 16, 16, eyes="angry"),
         at(5, 2, 20, 13, eyes="angry", mouth=True),
         at(8, 0, 26, 8, eyes="angry", mouth=True),
         at(4, 0, 20, 13, eyes="open"),
         at(0, 0, 20, 13)]
    sx, sy = cx + fx * 13, GROUND - 7 - (4 if fy else 0)
    sparks(f[3], sx, sy, 3)
    sparks(f[4], sx, sy, 4, color("slime", 4))
    A["Attack"] = f

    A["Hit"] = [slime(cx, GROUND, 20, 13, facing, flash=True),
                slime(cx - fx * 2, GROUND - fy, 23, 10, facing, eyes="x"),
                slime(cx - fx * 1, GROUND, 21, 12, facing, eyes="closed")]

    d = [slime(cx, GROUND, 20, 13, facing, flash=True),
         slime(cx, GROUND, 23, 10, facing, eyes="x"),
         slime(cx, GROUND, 26, 7, facing, eyes="x"),
         slime(cx, GROUND, 28, 4, facing, eyes="closed", shadow=False),
         slime(cx, GROUND, 24, 2.5, facing, eyes="closed", shadow=False),
         Canvas(S, S)]
    for i, (dxp, dyp) in enumerate([(-9, -4), (8, -6), (-4, -9), (5, -3)]):
        droplet(d[3], cx + dxp, GROUND - 4 + dyp // 2)
        droplet(d[4], cx + dxp * 1.3, GROUND - 1 + dyp // 3)
    for x in (-6, -1, 4, 9):
        d[5].set(cx + x, GROUND, color("slime", 1))
        d[5].set(cx + x - 1, GROUND, color("slime", 2))
    A["Death"] = d
    return A


def main():
    order = ["Idle", "Attack", "Hit", "Death"]
    out = os.path.join(OUT_DIR, "Characters", "Slime")
    rows = []
    all_anims = {f: anims(f) for f in DIRS}
    for name in order:
        for facing in DIRS:
            frames = all_anims[facing][name]
            save_sheet(frames, os.path.join(out, f"Slime_{name}_{facing}.png"))
            rows.append((f"{name} {facing}", frames))
            if facing in ("Down", "Right"):
                gif(frames, os.path.join(PREVIEW_DIR, f"Slime_{name}_{facing}.gif"),
                    ms=150 if name == "Idle" else 110)
    preview(rows, os.path.join(PREVIEW_DIR, "Slime_preview.png"), scale=5)


if __name__ == "__main__":
    main()
