"""Layered title screen: parallax layers, castle flags, twinkling stars, clouds, fireflies,
menu buttons and the layout file Unity reads to place everything.

    python Tools/art/title.py

Outputs (Assets/Art/UI/Original):
    Title_Sky.png, Title_Far.png, Title_Mid.png, Title_Near.png   (480+2*PAD) x 270 each
    Title_Flag.png      4 frames  8x5   (waving flag, hangs from the pole tip)
    Title_Twinkle.png   8 frames  7x7   (star that flares then rests)
    Title_Cloud_0.png, Title_Cloud_1.png
    Title_Firefly.png   5x5
    MenuButton_<Label>.png  3 frames: normal, hover, pressed (96x24 each)
    TitleLayout.json    positions in pixels of the 480x270 screen (x right, y down)
"""
import json
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(__file__))
from pixel import Canvas, color, PALETTE, save_sheet, OUT_DIR, PREVIEW_DIR, gif
from font import draw_text, text_width, H as FONT_H
from ui import dither_pick, pine, logo, UI_DIR, BAYER4
from PIL import Image

W, H = 480, 270
PAD = 8                # extra pixels on each side so layers can slide without showing edges
LW = W + 2 * PAD
HORIZON = 190


def ridge(base, amp, freqs, phase, width=LW):
    return [base - sum(a * math.sin((x - PAD) * f + phase * (i + 1)) for i, (a, f) in
                       enumerate(zip(amp, freqs))) for x in range(width)]


# ------------------------------------------------------------------ layers

def sky_layer(rnd):
    cv = Canvas(LW, H)
    for y in range(H):
        t = y / HORIZON
        for x in range(LW):
            if t < 0.78:
                c = dither_pick("sky", t / 0.78 * 3.2, x, y)
            else:
                c = dither_pick("purple", 1.2 + (t - 0.78) / 0.22 * 2.0, x, y)
            cv.px[y][x] = c
    mx, my, mr = 388 + PAD, 52, 16
    # soft two-step halo (dithered so it stays pixel art)
    for y in range(my - mr - 14, my + mr + 15):
        for x in range(mx - mr - 14, mx + mr + 15):
            d = math.hypot(x - mx, y - my)
            if mr < d <= mr + 5:
                cv.set(x, y, color("sky", 3))
            elif mr + 5 < d <= mr + 12 and (x + y) % 2 == 0:
                cv.set(x, y, color("sky", 3) if d < mr + 9 else color("sky", 2))
    for y in range(my - mr, my + mr + 1):
        for x in range(mx - mr, mx + mr + 1):
            d = math.hypot(x - mx, y - my)
            if d <= mr:
                lit = (x - mx) * -0.6 + (y - my) * -0.4
                v = 4 if lit > -6 else 3
                if d > mr - 1.2 and lit < -2:
                    v = 2
                cv.set(x, y, color("stone", v))
    for cx_, cy_, r in ((382, 46, 3), (394, 60, 2), (396, 44, 1.5), (380, 60, 1.5)):
        for y in range(int(cy_ - r), int(cy_ + r) + 1):
            for x in range(int(cx_ - r), int(cx_ + r) + 1):
                if math.hypot(x - cx_, y - cy_) <= r:
                    cv.set(x + PAD, y, color("stone", 3))
    # static stars (the twinkling ones are separate sprites)
    stars = []
    for _ in range(170):
        x, y = rnd.randrange(LW), rnd.randrange(int(HORIZON * 0.7))
        if math.hypot(x - mx, y - my) < mr + 16:
            continue
        cv.set(x, y, color("white") if rnd.random() < 0.3 else color("sky", 4))
        stars.append((x - PAD, y))
    return cv, (mx - PAD, my, mr)


def castle(cv, x0, base):
    """Castle standing on its own hill; returns the flag anchor points (pole tips)."""
    st = lambda k: color("stone", k)
    flags = []

    def tower(x, w, h, roof=True):
        top = base - h
        for y in range(top, base + 1):
            for xx in range(x, x + w):
                k = 1 if xx > x + w * 0.6 else 2
                if xx == x:
                    k = 3   # moonlit edge (moon is up-right, but the rim reads better on the left)
                cv.set(xx, y, st(k))
        for y in range(top + 3, base, 5):   # stone courses
            for xx in range(x + 1, x + w - 1, 3):
                cv.set(xx + (y // 5) % 2, y, st(1))
        for xx in range(x, x + w, 2):       # crenels
            cv.set(xx, top - 1, st(2))
        if roof:
            for i in range(w // 2 + 3):
                cv.hline(x - 1 + i, x + w - i, top - 2 - i, color("blue", 1 if i % 3 else 0))
            tip = top - w // 2 - 5
            cv.set(x + w // 2, tip, color("gold", 3))
            cv.rect(x + w // 2, tip - 4, x + w // 2, tip, st(3))
            flags.append((x + w // 2 + 1, tip - 4))
        for wy in range(top + 4, base - 3, 7):  # lit windows
            cv.set(x + w // 2, wy, color("gold", 3))
            cv.set(x + w // 2, wy + 1, color("gold", 2))

    tower(x0, 26, 26, roof=False)
    tower(x0 - 8, 10, 40)
    tower(x0 + 24, 10, 46)
    tower(x0 + 9, 8, 34)
    cv.rect(x0 + 10, base - 8, x0 + 15, base, color("outline"))   # gate
    cv.hline(x0 + 11, x0 + 14, base - 9, color("outline"))
    cv.set(x0 + 12, base - 6, color("gold", 1))                     # torch glow in the gate
    cv.set(x0 + 13, base - 6, color("gold", 2))
    return flags


def far_layer():
    cv = Canvas(LW, H)
    far = ridge(170, (14, 7, 3), (0.012, 0.031, 0.09), 1.3)
    for x in range(LW):
        for y in range(int(far[x]), H):
            cv.px[y][x] = color("purple", 1) if y - far[x] > 2 else color("purple", 2)
    # the castle's hill, so it no longer floats on the horizon
    hx, hy, rx, ry = 72 + PAD, 176, 64, 14
    for y in range(hy - ry, H):
        for x in range(hx - rx, hx + rx + 1):
            if y >= hy or ((x - hx) / rx) ** 2 + ((y - hy) / ry) ** 2 <= 1:
                top = hy - ry * math.sqrt(max(0, 1 - ((x - hx) / rx) ** 2))
                d = y - top
                if d < 1:
                    c = color("purple", 3)
                elif d < 3 or (d < 6 and (x + y) % 2 == 0):
                    c = color("purple", 2)
                else:
                    c = color("purple", 1)
                cv.set(x, y, c)
    # path winding up to the gate
    for i in range(30):
        t = i / 29
        px = hx - 2 + math.sin(t * 5.5) * 9 * (1 - t) + 4
        py = 168 + t * 22
        cv.set(px, py, color("purple", 3))
        cv.set(px + 1, py, color("purple", 2))
    flags = castle(cv, 70 + PAD, 168)
    # low mist hugging the valley in front of the mountains (ordered dither, denser in the middle)
    for y in range(174, 202):
        a = max(0.0, 1 - abs(y - 188) / 14) * 0.55
        for x in range(LW):
            if a * 16 > BAYER4[y % 4][(x + y // 4) % 4] + 0.5:
                cv.set(x, y, color("purple", 3, alpha=110))
    return cv, [(x - PAD, y) for x, y in flags]


def mid_layer(rnd):
    cv = Canvas(LW, H)
    mid = ridge(196, (10, 5, 2), (0.017, 0.043, 0.11), 4.1)
    for x in range(LW):
        for y in range(int(mid[x]), H):
            cv.px[y][x] = color("grass", 0) if y - mid[x] > 1 else color("grass", 1)
    for _ in range(54):
        x = rnd.randrange(LW)
        pine(cv, x, int(mid[x]) + 2, rnd.randint(9, 17))
    return cv


def near_layer(rnd):
    cv = Canvas(LW, H)
    near = ridge(236, (4, 2), (0.02, 0.07), 2.2)
    for x in range(LW):
        for y in range(int(near[x]), H):
            d = y - near[x]
            cv.px[y][x] = dither_pick("grass", 3.0 - min(2.4, d / 14), x, y)
        cv.set(x, int(near[x]), color("grass", 4))       # moonlit rim on the meadow edge
    for _ in range(300):  # grass tufts
        x = rnd.randrange(LW)
        y = rnd.randrange(int(near[x]) + 2, H)
        cv.set(x, y, color("grass", 4))
        cv.set(x, y - 1, color("grass", 3))
    for _ in range(46):   # flowers
        x = rnd.randrange(LW)
        y = rnd.randrange(int(near[x]) + 4, H)
        cv.set(x, y, color("gold", 3) if rnd.random() < 0.5 else color("white"))
    for _ in range(9):    # a few rocks
        x = rnd.randrange(LW)
        y = rnd.randrange(int(near[x]) + 10, H - 2)
        cv.rect(x, y, x + 2, y + 1, color("stone", 2))
        cv.hline(x, x + 1, y, color("stone", 3))
        cv.hline(x - 1, x + 3, y + 2, color("grass", 0))
    # tall grass blades along the very bottom, framing the scene
    for x in range(0, LW, 2):
        h = 4 + int(3 * (1 + math.sin(x * 0.37)) + rnd.randint(0, 3))
        lean = 1 if (x // 6) % 2 else -1
        for i in range(h):
            cv.set(x + (lean if i > h * 0.6 else 0), H - 1 - i, color("grass", 0 if i < h / 2 else 1))
    return cv, [n for n in near[PAD:PAD + W]]


# ------------------------------------------------------------------ small animated sprites

def flag_frames():
    """Pennant 7 px long; the free end ripples while the hoist stays on the pole."""
    frames = []
    for f in range(4):
        cv = Canvas(8, 5)
        for x in range(7):
            amp = x / 6
            wave = round(math.sin(x * 1.1 - f * math.pi / 2) * 1.2 * amp)
            rows = 3 if x < 4 else (2 if x < 6 else 1)
            top = 1 + wave + (1 if rows == 1 else 0)
            for y in range(rows):
                cv.set(x, top + y, color("red", 3 if y == 0 else 2))
            cv.set(x, top + rows - 1, color("red", 1) if rows > 1 else color("red", 2))
        frames.append(cv)
    return frames


def twinkle_frames():
    sizes = [0, 1, 2, 3, 2, 1, 0, 0]
    frames = []
    for s in sizes:
        cv = Canvas(7, 7)
        cv.set(3, 3, color("white"))
        for i in range(1, s + 1):
            c = color("white") if i == 1 else color("sky", 4, alpha=200 if i == 2 else 140)
            for dx, dy in ((i, 0), (-i, 0), (0, i), (0, -i)):
                cv.set(3 + dx, 3 + dy, c)
        if s == 3:
            for dx, dy in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
                cv.set(3 + dx, 3 + dy, color("sky", 4, alpha=120))
        frames.append(cv)
    return frames


def cloud(w, h, seed):
    rnd = random.Random(seed)
    cv = Canvas(w, h)
    puffs = [(w * 0.5, h * 0.62, w * 0.42, h * 0.38)]
    for _ in range(4):
        px = rnd.uniform(w * 0.18, w * 0.82)
        puffs.append((px, h * rnd.uniform(0.35, 0.6), w * rnd.uniform(0.12, 0.2), h * rnd.uniform(0.3, 0.45)))
    for y in range(h):
        for x in range(w):
            inside = [p for p in puffs if ((x + .5 - p[0]) / p[2]) ** 2 + ((y + .5 - p[1]) / p[3]) ** 2 <= 1]
            if not inside:
                continue
            top = min(p[1] - p[3] * math.sqrt(max(0, 1 - ((x + .5 - p[0]) / p[2]) ** 2)) for p in inside)
            depth = y - top
            if depth < 1.5:
                c = color("sky", 4, alpha=230)
            elif depth < 3:
                c = color("sky", 3, alpha=220)
            else:
                c = color("sky", 2, alpha=210)
            if y >= h - 2 and (x + y) % 2:
                continue                       # dithered, ragged underside
            cv.set(x, y, c)
    return cv


def firefly():
    cv = Canvas(5, 5)
    cv.set(2, 2, color("white"))
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        cv.set(2 + dx, 2 + dy, color("gold", 3, alpha=200))
    for dx, dy in ((2, 0), (-2, 0), (0, 2), (0, -2), (1, 1), (-1, 1), (1, -1), (-1, -1)):
        cv.set(2 + dx, 2 + dy, color("gold", 2, alpha=90))
    return cv


# ------------------------------------------------------------------ menu buttons

MBW, MBH = 96, 24
MENU_LABELS = {"Jogar": "JOGAR", "Opcoes": "OPÇÕES", "Creditos": "CRÉDITOS", "Sair": "SAIR"}


def menu_button(state, label):
    """Wooden plaque with a gold frame. state: normal | hover | pressed."""
    w, h = MBW, MBH
    cv = Canvas(w, h)
    press = 1 if state == "pressed" else 0
    lip = 0 if press else 2
    y0, y1 = press, h - 1
    hover = state == "hover"
    rnd = random.Random(label)
    grain = {}
    for gy in (5, h - 8):          # long streaks above and below the label, never across it
        x = rnd.randrange(5, 12)
        while x < w - 8:
            ln = rnd.randint(8, 18)
            for i in range(min(ln, w - 6 - x)):
                grain[(x + i, gy + press)] = True
            x += ln + rnd.randint(6, 14)

    # gold frame + wooden panel
    for y in range(y0, y1 + 1):
        for x in range(w):
            # chamfered corners (2 px)
            cx = min(x, w - 1 - x)
            cy = min(y - y0, y1 - y)
            if cx + cy < 2:
                continue
            if y > y1 - lip:
                c = color("wood", 0)                           # bottom lip (shadow side)
            elif cx == 0 or cy == 0 or cx + cy == 2:
                c = color("outline")
            elif cx == 1 or y - y0 == 1 or y1 - lip - y == 1 or cx + cy == 3:
                k = 3 if (y - y0 <= 2 or cx == 1 and x < w // 2) else 1
                c = color("gold", min(3, k + (1 if hover else 0)) if k < 3 else 3)
                if hover and y - y0 == 1:
                    c = color("white")
            elif cx == 2 or y - y0 == 2 or y1 - lip - y == 2:
                c = color("gold", 2 if y - y0 == 2 else 1)
            else:
                band = y - y0 - 3
                k = 3 if band < 2 else 2
                if y1 - lip - y <= 3:
                    k = 1
                if hover:
                    k = min(4, k + 1)
                c = color("wood", k)
                if grain.get((x, y)) and 2 < band and y1 - lip - y > 3:
                    c = color("wood", max(0, k - 1))            # wood grain
            cv.set(x, y, c)
        if y == y1 and lip:
            pass
    # outline under the lip
    for x in range(2, w - 2):
        cv.set(x, y1, color("outline"))
    # rivets
    for rx, ry in ((4, y0 + 4), (w - 5, y0 + 4), (4, y1 - lip - 4), (w - 5, y1 - lip - 4)):
        cv.set(rx, ry, color("gold", 3))
        cv.set(rx + 1, ry + 1, color("gold", 1))
    inner_top = y0 + 3
    inner_bot = y1 - lip - 3
    cy = inner_top + (inner_bot - inner_top + 1 - FONT_H) // 2
    tw = text_width(label)
    tx = (w - tw) // 2
    text = color("white")
    draw_text(cv, label, tx, cy + 1, color("wood", 0))
    draw_text(cv, label, tx, cy, text)
    if hover:  # gold arrows pointing at the label
        mid = cy + FONT_H // 2
        for i in range(3):
            cv.rect(tx - 9 + i, mid - (2 - i), tx - 9 + i, mid + (2 - i), color("gold", 3))
            cv.rect(tx + tw + 8 - i, mid - (2 - i), tx + tw + 8 - i, mid + (2 - i), color("gold", 3))
        cv.set(tx - 9, mid - 2, color("white"))
        cv.set(tx + tw + 8, mid - 2, color("white"))
    return cv


# ------------------------------------------------------------------ build

ACTORS = [  # name, sheet, x (centre, screen px), start frame
    ("Paladin", "Characters/Paladin/Paladin_Idle_Right", 150, 0),
    ("Paladin2", "Characters/Paladin/Paladin_Idle_Right", 186, 2),
    ("Slime", "Characters/Slime/Slime_Idle_Left", 290, 1),
    ("Goblin", "Characters/Goblin/Goblin_Idle_Left", 326, 3),
    ("Orc", "Characters/Orc/Orc_Idle_Left", 364, 0),
]
TWINKLES = [(52, 22), (118, 64), (176, 18), (232, 88), (300, 30), (452, 92), (430, 18), (20, 96), (268, 120)]
CLOUDS = [("Title_Cloud_0", 330, 58, 3.0), ("Title_Cloud_1", 120, 36, 4.5), ("Title_Cloud_1", 470, 110, 2.2)]


def build():
    rnd = random.Random(7)
    os.makedirs(UI_DIR, exist_ok=True)
    sky, moon = sky_layer(rnd)
    far, flags = far_layer()
    mid = mid_layer(rnd)
    near, ground = near_layer(rnd)
    layers = {"Title_Sky": sky, "Title_Far": far, "Title_Mid": mid, "Title_Near": near}
    for name, cv in layers.items():
        cv.to_image().save(os.path.join(UI_DIR, name + ".png"))

    save_sheet(flag_frames(), os.path.join(UI_DIR, "Title_Flag.png"))
    save_sheet(twinkle_frames(), os.path.join(UI_DIR, "Title_Twinkle.png"))
    clouds = {"Title_Cloud_0": cloud(64, 14, 3), "Title_Cloud_1": cloud(42, 10, 11)}
    for name, cv in clouds.items():
        cv.to_image().save(os.path.join(UI_DIR, name + ".png"))
    firefly().to_image().save(os.path.join(UI_DIR, "Title_Firefly.png"))
    buttons = {}
    for key, text in MENU_LABELS.items():
        buttons[key] = [menu_button(s, text) for s in ("normal", "hover", "pressed")]
        save_sheet(buttons[key], os.path.join(UI_DIR, f"MenuButton_{key}.png"))

    layout = {
        "width": W, "height": H, "pad": PAD,
        "flags": [{"x": x, "y": y} for x, y in flags],
        "twinkles": [{"x": x, "y": y} for x, y in TWINKLES],
        "clouds": [{"sprite": s, "x": x, "y": y, "speed": v} for s, x, y, v in CLOUDS],
        "actors": [{"name": n, "sheet": s, "x": x, "feetY": int(ground[x]) + 8, "startFrame": f}
                   for n, s, x, f in ACTORS],
        "fireflyArea": {"x": 0, "y": 196, "w": W, "h": 60},
    }
    with open(os.path.join(UI_DIR, "TitleLayout.json"), "w", encoding="utf-8") as fh:
        json.dump(layout, fh, indent=2)

    preview(layers, flags, clouds, ground, buttons)


def preview(layers, flags, clouds, ground, buttons):
    """Still of the composed menu, roughly what Unity shows (for review only)."""
    from humanoid import render, poses
    from characters import ALL
    import slime
    img = Image.new("RGBA", (W, H))
    for name in ("Title_Sky",):
        img.alpha_composite(layers[name].to_image().crop((PAD, 0, PAD + W, H)))
    for s, x, y, _ in CLOUDS:
        img.alpha_composite(clouds[s].to_image(), (x - clouds[s].w // 2, y))
    tw = twinkle_frames()[3].to_image()
    for x, y in TWINKLES:
        img.alpha_composite(tw, (x - 3, y - 3))
    for name in ("Title_Far", "Title_Mid"):
        img.alpha_composite(layers[name].to_image().crop((PAD, 0, PAD + W, H)))
    fl = flag_frames()[1].to_image()
    for x, y in flags:
        img.alpha_composite(fl, (x, y - 1))
    img.alpha_composite(layers["Title_Near"].to_image().crop((PAD, 0, PAD + W, H)))
    ff = firefly().to_image()
    r = random.Random(2)
    for _ in range(14):
        img.alpha_composite(ff, (r.randrange(10, W - 10), r.randrange(196, 250)))
    for name, sheet, x, start in ACTORS:
        if name == "Slime":
            spr = slime.anims("Left")["Idle"][start % 4]
        else:
            spec = ALL[name.rstrip("2")]
            facing = "Right" if name.startswith("Paladin") else "Left"
            spr = render(spec, facing, poses(spec)["Idle"][start % len(poses(spec)["Idle"])])
        im = spr.to_image().resize((64, 64), Image.NEAREST)
        img.alpha_composite(im, (x - 32, int(ground[x]) + 8 - 56))
    lg = logo().to_image()
    img.alpha_composite(lg, ((W - lg.width) // 2, 25))
    b0 = buttons["Jogar"][1].to_image()
    b1 = buttons["Sair"][0].to_image()
    img.alpha_composite(b0, ((W - MBW) // 2, 135 - 2 + 5))
    img.alpha_composite(b1, ((W - MBW) // 2, 135 + 28 + 5))
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    img.resize((W * 3, H * 3), Image.NEAREST).save(os.path.join(PREVIEW_DIR, "Title_preview.png"))
    board = Image.new("RGBA", (MBW * 3 + 16, (MBH + 4) * len(buttons) + 4), (40, 36, 56, 255))
    for r_, frames in enumerate(buttons.values()):
        for i, f in enumerate(frames):
            board.alpha_composite(f.to_image(), (4 + i * (MBW + 4), 4 + r_ * (MBH + 4)))
    board.resize((board.width * 4, board.height * 4), Image.NEAREST).save(
        os.path.join(PREVIEW_DIR, "MenuButtons_preview.png"))
    gif(flag_frames(), os.path.join(PREVIEW_DIR, "Title_Flag.gif"), scale=12, ms=140)
    gif(twinkle_frames(), os.path.join(PREVIEW_DIR, "Title_Twinkle.gif"), scale=12, ms=120)


if __name__ == "__main__":
    build()
