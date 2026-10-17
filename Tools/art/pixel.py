"""Mini toolkit for hand-made pixel art in code.

Every sprite is drawn on a Canvas (a grid of palette keys), then outlined, shaded and
exported as a horizontal sprite sheet. Run the per-asset scripts (slime.py, ...) to
regenerate the PNGs; nothing here depends on Unity.
"""
from __future__ import annotations

import math
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_DIR = os.environ.get("ART_OUT") or os.path.join(ROOT, "Assets", "Art")
PREVIEW_DIR = os.path.join(os.path.dirname(__file__), "preview")


def hex2rgba(h: str, a: int = 255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


# One shared palette for the whole game (GDD: "uma paleta única").
# Ramps go dark -> light so shading can index into them.
PALETTE = {
    "outline": "#1b1428",
    "shadow": "#1b1428",
    "white": "#f4f1de",
    "black": "#1b1428",
    "red": ["#5a1a2a", "#9c2a35", "#d8473c", "#f1876b"],
    "slime": ["#1d4a3c", "#2f7d4e", "#57b35a", "#a5e26b", "#e3f7a8"],
    "goblin": ["#2e3b1f", "#4f6b2c", "#7e9a3a", "#b7c95c"],
    "orc": ["#2d2a33", "#4a4b4c", "#6f7665", "#9aa38a"],
    "skin_dark": ["#3a2420", "#6b3d2e", "#9b5e3e", "#c98a5a"],
    "leather": ["#2e1d1a", "#5a3626", "#875236", "#b07a4e"],
    "steel": ["#2b2f45", "#4f5874", "#8a95b0", "#c9d2e3", "#f4f1de"],
    "gold": ["#5a3a1a", "#a8692a", "#e0a43a", "#f6d870"],
    "blue": ["#1c2550", "#2d4a8f", "#4a7fd0", "#86b8f0"],
    "purple": ["#2a1a40", "#4f2f73", "#7d4fa8", "#b88ad8"],
    "stone": ["#26222e", "#46404f", "#6e6878", "#a39cad", "#d6d0dc"],
    "grass": ["#1f3a2a", "#2f5e35", "#4d8a3e", "#7fb24e", "#b7d872"],
    "sky": ["#1a1c3a", "#2b3366", "#46549a", "#6d86c8", "#a8c4ec"],
    "wood": ["#2e1d1a", "#5a3626", "#875236", "#b07a4e", "#d6a771"],
}


def color(key, shade=None, alpha=255):
    v = PALETTE[key]
    if isinstance(v, list):
        v = v[max(0, min(len(v) - 1, shade if shade is not None else len(v) // 2))]
    return hex2rgba(v, alpha)


class Canvas:
    def __init__(self, w: int, h: int):
        self.w, self.h = w, h
        self.px = [[None] * w for _ in range(h)]  # None or RGBA tuple

    def set(self, x, y, c):
        x, y = int(round(x)), int(round(y))
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[y][x] = c

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.px[y][x]
        return None

    def rect(self, x0, y0, x1, y1, c):
        for y in range(int(y0), int(y1) + 1):
            for x in range(int(x0), int(x1) + 1):
                self.set(x, y, c)

    def hline(self, x0, x1, y, c):
        self.rect(min(x0, x1), y, max(x0, x1), y, c)

    def line(self, x0, y0, x1, y1, c):
        n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
        for i in range(n):
            t = i / max(1, n - 1)
            self.set(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, c)

    def fill_shape(self, inside, shade_fn):
        """inside(x, y) -> bool; shade_fn(x, y) -> RGBA. Samples pixel centres."""
        for y in range(self.h):
            for x in range(self.w):
                if inside(x + 0.5, y + 0.5):
                    self.px[y][x] = shade_fn(x + 0.5, y + 0.5)

    def ellipse(self, cx, cy, rx, ry, c):
        self.fill_shape(lambda x, y: ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1,
                        lambda x, y: c)

    def outline(self, c=None, diagonal=False):
        """Adds a 1px outline around every opaque region (outside the shape)."""
        c = c or color("outline")
        add = []
        nbrs = [(1, 0), (-1, 0), (0, 1), (0, -1)]
        if diagonal:
            nbrs += [(1, 1), (-1, 1), (1, -1), (-1, -1)]
        for y in range(self.h):
            for x in range(self.w):
                if self.px[y][x] is None:
                    for dx, dy in nbrs:
                        p = self.get(x + dx, y + dy)
                        if p is not None and p[3] == 255 and p != c:
                            add.append((x, y))
                            break
        for x, y in add:
            self.px[y][x] = c

    def under(self, other: "Canvas", ox=0, oy=0):
        """Draws `other` below the current pixels (only where this canvas is empty)."""
        for y in range(other.h):
            for x in range(other.w):
                p = other.px[y][x]
                if p is not None and self.get(x + ox, y + oy) is None:
                    self.set(x + ox, y + oy, p)

    def over(self, other: "Canvas", ox=0, oy=0):
        for y in range(other.h):
            for x in range(other.w):
                p = other.px[y][x]
                if p is not None:
                    self.set(x + ox, y + oy, p)

    def flipped(self) -> "Canvas":
        c = Canvas(self.w, self.h)
        c.px = [list(reversed(r)) for r in self.px]
        return c

    def to_image(self) -> Image.Image:
        im = Image.new("RGBA", (self.w, self.h), (0, 0, 0, 0))
        for y in range(self.h):
            for x in range(self.w):
                if self.px[y][x] is not None:
                    im.putpixel((x, y), self.px[y][x])
        return im


def ground_shadow(cv: Canvas, cx, cy, rx, ry=None):
    """Soft dark ellipse on the floor; drawn first so the body covers it."""
    ry = ry or max(1, rx * 0.35)
    sh = color("shadow", alpha=110)
    cv.ellipse(cx, cy, rx, ry, sh)


def sphere_light(nx, ny, lx=-0.55, ly=-0.75):
    """Lambert-ish value in [0,1] for a point (nx, ny) in [-1,1]^2 of a dome."""
    nz = math.sqrt(max(0.0, 1 - nx * nx - ny * ny))
    l = math.sqrt(lx * lx + ly * ly + 0.6 * 0.6)
    return max(0.0, (nx * lx + ny * ly + nz * 0.6) / l)


def ramp_pick(ramp_key, v, bands=None):
    ramp = PALETTE[ramp_key]
    n = bands or len(ramp)
    i = min(n - 1, int(v * n))
    return hex2rgba(ramp[i])


# ---------------------------------------------------------------- export helpers

def save_sheet(frames: list[Canvas], path: str) -> Image.Image:
    w, h = frames[0].w, frames[0].h
    sheet = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        sheet.alpha_composite(f.to_image(), (i * w, 0))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    sheet.save(path)
    return sheet


def preview(rows: list[tuple[str, list[Canvas]]], path: str, scale=6,
            bg=(58, 54, 74, 255)):
    """Labelled preview board: one row per animation, frames scaled up."""
    fw, fh = rows[0][1][0].w * scale, rows[0][1][0].h * scale
    label_w = 150
    cols = max(len(fr) for _, fr in rows)
    img = Image.new("RGBA", (label_w + cols * (fw + 4) + 4, len(rows) * (fh + 4) + 4), bg)
    d = ImageDraw.Draw(img)
    try:
        font = ImageFont.truetype("consola.ttf", 16)
    except OSError:
        font = ImageFont.load_default()
    for r, (name, frames) in enumerate(rows):
        y = 4 + r * (fh + 4)
        d.text((8, y + fh // 2 - 8), name, fill=(240, 236, 220, 255), font=font)
        for i, f in enumerate(frames):
            x = label_w + 4 + i * (fw + 4)
            d.rectangle([x, y, x + fw - 1, y + fh - 1], fill=(72, 68, 92, 255))
            img.alpha_composite(f.to_image().resize((fw, fh), Image.NEAREST), (x, y))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    return img


def gif(frames: list[Canvas], path: str, scale=8, ms=120, bg=(72, 68, 92, 255)):
    ims = []
    for f in frames:
        base = Image.new("RGBA", (f.w, f.h), bg)
        base.alpha_composite(f.to_image())
        ims.append(base.resize((f.w * scale, f.h * scale), Image.NEAREST).convert("P"))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    ims[0].save(path, save_all=True, append_images=ims[1:], duration=ms, loop=0, disposal=2)
