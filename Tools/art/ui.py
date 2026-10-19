"""Title screen background, logo, buttons and panel.

    python Tools/art/ui.py

Outputs (Assets/Art/UI/Original):
    Title_Background.png   480x270 (x4 = 1920x1080)
    Logo.png               "MONSTERS & SOLDIERS"
    Button_<Label>.png     3 frames: normal, hover, pressed (72x20 each)
    Button_Blank.png       4 frames: normal, hover, pressed, disabled (9-slice)
    Button_Pause_Icon.png  3 frames, 20x20
    Panel.png              48x48 (9-slice)
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(__file__))
from pixel import Canvas, color, hex2rgba, PALETTE, save_sheet, OUT_DIR, PREVIEW_DIR
from font import draw_text, text_width, H as FONT_H
from PIL import Image

UI_DIR = os.path.join(OUT_DIR, "UI", "Original")

BAYER4 = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def dither_pick(ramp, v, x, y):
    """v in [0, len-1]; ordered dithering between neighbouring ramp entries."""
    n = len(PALETTE[ramp])
    v = max(0.0, min(n - 1.0, v))
    lo = int(v)
    frac = v - lo
    k = lo + (1 if frac * 16 > BAYER4[y % 4][x % 4] + 0.5 else 0)
    return color(ramp, min(n - 1, k))


# ------------------------------------------------------------------ logo

def logo():
    big, small = 3, 2
    l1, l2, l3 = "MONSTERS", "&", "SOLDIERS"
    w = max(text_width(l1, 1, big), text_width(l3, 1, big)) + 12
    h = FONT_H * big * 2 + FONT_H * small + 4 + 14
    cv = Canvas(w, h)

    def grad(ramp, top_k, bot_k, rows):
        def f(gx, gy):
            t = gy / (FONT_H - 1)
            k = round(top_k + (bot_k - top_k) * t)
            if gy == 0:
                k = top_k
            return color(ramp, k)
        return f

    y1 = 6
    x1 = (w - text_width(l1, 1, big)) // 2
    draw_text(cv, l1, x1, y1, grad("slime", 4, 1, FONT_H), scale=big)
    y2 = y1 + FONT_H * big + 2
    x2 = (w - text_width(l2, 1, small)) // 2
    draw_text(cv, l2, x2, y2, grad("gold", 3, 2, FONT_H), scale=small)
    y3 = y2 + FONT_H * small + 2
    x3 = (w - text_width(l3, 1, big)) // 2
    draw_text(cv, l3, x3, y3, grad("gold", 3, 1, FONT_H), scale=big)

    # specular pixel on the top-left of every glyph stroke
    for y in range(h):
        for x in range(w):
            p = cv.px[y][x]
            if p and cv.get(x - 1, y) is None and cv.get(x, y - 1) is None:
                cv.px[y][x] = color("white")

    cv.outline(color("outline"), diagonal=True)
    cv.outline(color("purple", 1), diagonal=False)
    # drop shadow
    shadow = Canvas(w, h)
    for y in range(h):
        for x in range(w):
            if cv.px[y][x] is not None:
                shadow.set(x + 1, y + 2, color("outline"))
    cv.under(shadow)
    return cv


# ------------------------------------------------------------------ background

def title_background(seed=7):
    rnd = random.Random(seed)
    W, H = 480, 270
    cv = Canvas(W, H)
    horizon = 190
    # sky: deep blue at top to dusky purple-pink near the horizon
    for y in range(H):
        t = y / horizon
        for x in range(W):
            if t < 0.78:
                c = dither_pick("sky", t / 0.78 * 3.2, x, y)
            else:
                c = dither_pick("purple", 1.2 + (t - 0.78) / 0.22 * 2.0, x, y)
            cv.px[y][x] = c
    # stars
    for _ in range(140):
        x, y = rnd.randrange(W), rnd.randrange(int(horizon * 0.65))
        cv.set(x, y, color("white") if rnd.random() < 0.35 else color("sky", 4))
    for _ in range(6):  # a few twinkly crosses
        x, y = rnd.randrange(20, W - 20), rnd.randrange(10, 80)
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
            cv.set(x + dx, y + dy, color("white"))
    # moon with halo
    mx, my, mr = 388, 52, 16
    for y in range(my - mr - 8, my + mr + 9):
        for x in range(mx - mr - 8, mx + mr + 9):
            d = math.hypot(x - mx, y - my)
            if d <= mr:
                v = 4 if (x - mx) * -0.6 + (y - my) * -0.4 > -6 else 3
                cv.set(x, y, color("stone", v))
            elif d <= mr + 6 and (x + y) % 2 == 0:
                cv.set(x, y, color("sky", 3))
    for cx_, cy_, r in ((382, 46, 3), (394, 60, 2), (396, 44, 1.5), (380, 60, 1.5)):
        for y in range(int(cy_ - r), int(cy_ + r) + 1):
            for x in range(int(cx_ - r), int(cx_ + r) + 1):
                if math.hypot(x - cx_, y - cy_) <= r:
                    cv.set(x, y, color("stone", 3))

    def ridge(base, amp, freqs, phase):
        return [base - sum(a * math.sin(x * f + phase * (i + 1)) for i, (a, f) in
                           enumerate(zip(amp, freqs))) for x in range(W)]

    # far mountains
    far = ridge(170, (14, 7, 3), (0.012, 0.031, 0.09), 1.3)
    for x in range(W):
        for y in range(int(far[x]), H):
            cv.px[y][x] = color("purple", 1) if y - far[x] > 2 else color("purple", 2)
    # castle on the left hill
    castle(cv, 70, 168)
    # mid hills with pines
    mid = ridge(196, (10, 5, 2), (0.017, 0.043, 0.11), 4.1)
    for x in range(W):
        for y in range(int(mid[x]), H):
            cv.px[y][x] = color("grass", 0) if y - mid[x] > 1 else color("grass", 1)
    for _ in range(46):
        x = rnd.randrange(W)
        pine(cv, x, int(mid[x]) + 2, rnd.randint(9, 17))
    # foreground meadow
    near = ridge(236, (4, 2), (0.02, 0.07), 2.2)
    for x in range(W):
        for y in range(int(near[x]), H):
            d = y - near[x]
            cv.px[y][x] = dither_pick("grass", 3.0 - min(2.4, d / 14), x, y)
    for _ in range(260):  # grass tufts and flowers
        x = rnd.randrange(W)
        y = rnd.randrange(int(near[x]) + 2, H)
        cv.set(x, y, color("grass", 4))
        cv.set(x, y - 1, color("grass", 3))
    for _ in range(40):
        x = rnd.randrange(W)
        y = rnd.randrange(int(near[x]) + 4, H)
        cv.set(x, y, color("gold", 3) if rnd.random() < 0.5 else color("white"))
    return cv, near


def castle(cv, x0, base):
    st = lambda k: color("stone", k)
    def tower(x, w, h, roof=True):
        top = base - h
        for y in range(top, base + 1):
            for xx in range(x, x + w):
                cv.set(xx, y, st(1 if xx > x + w * 0.6 else 2))
        for xx in range(x, x + w, 2):  # crenels
            cv.set(xx, top - 1, st(2))
        if roof:
            for i in range(w // 2 + 3):
                cv.hline(x - 1 + i, x + w - i, top - 2 - i, color("blue", 1 if i % 3 else 0))
            cv.set(x + w // 2, top - w // 2 - 5, color("gold", 3))
            cv.rect(x + w // 2, top - w // 2 - 9, x + w // 2, top - w // 2 - 5, st(3))
            cv.rect(x + w // 2 + 1, top - w // 2 - 9, x + w // 2 + 3, top - w // 2 - 8, color("red", 2))
        for wy in range(top + 4, base - 3, 7):  # lit windows
            cv.set(x + w // 2, wy, color("gold", 3))
            cv.set(x + w // 2, wy + 1, color("gold", 2))
    tower(x0, 26, 26, roof=False)
    tower(x0 - 8, 10, 40)
    tower(x0 + 24, 10, 46)
    tower(x0 + 9, 8, 34)
    # gate
    cv.rect(x0 + 10, base - 8, x0 + 15, base, color("outline"))
    cv.hline(x0 + 11, x0 + 14, base - 9, color("outline"))


def pine(cv, x, base, h):
    for i in range(h):
        half = int(i * 0.45) + (1 if i % 4 == 3 else 0)
        y = base - h + i
        for xx in range(x - half, x + half + 1):
            cv.set(xx, y, color("grass", 1) if xx < x else color("outline"))
    cv.set(x, base, color("wood", 0))


def title_screen_preview(bg, ground, logo_cv):
    """Composite used only for previewing: bg + characters + logo."""
    from humanoid import render, poses
    from characters import ALL
    import slime
    comp = Canvas(bg.w, bg.h)
    comp.over(bg)
    actors = [(ALL["Paladin"], "Right", 92), (slime, "Left", 300),
              (ALL["Goblin"], "Left", 340), (ALL["Orc"], "Left", 392)]
    for spec, facing, x in actors:
        spr = (spec.anims(facing)["Idle"][0] if spec is slime
               else render(spec, facing, poses(spec)["Idle"][0]))
        big = Canvas(64, 64)
        for y in range(32):
            for xx in range(32):
                p = spr.px[y][xx]
                if p is not None:
                    big.rect(xx * 2, y * 2, xx * 2 + 1, y * 2 + 1, p)
        comp.over(big, x - 32, int(ground[x]) + 6 - 58)
    comp.over(logo_cv, (bg.w - logo_cv.w) // 2, 34)
    return comp


# ------------------------------------------------------------------ buttons

BW, BH = 72, 20

FACES = {
    "blue": ("blue", "gold"),
    "red": ("red", "gold"),
    "grey": ("stone", "stone"),
}


def button(w, h, state, face="blue", label=None, icon=None):
    """state: normal | hover | pressed | disabled."""
    cv = Canvas(w, h)
    ramp, trim = FACES["grey" if state == "disabled" else face]
    press = 1 if state == "pressed" else 0
    lip = 0 if press else 2             # visible bottom bevel thickness
    y0, y1 = press, h - 1
    x0, x1 = 0, w - 1
    light = {"normal": 2, "hover": 2, "pressed": 1, "disabled": 2}[state]
    top_rows = 4 if state == "hover" else 2   # hover = taller highlight band
    # body
    for y in range(y0 + 1, y1):
        for x in range(x0 + 1, x1):
            if y >= y1 - lip:
                c = color(ramp, max(0, light - 2))
            elif y <= y0 + top_rows:
                c = color(ramp, min(len(PALETTE[ramp]) - 1, light + 1))
            else:
                c = color(ramp, light)
            cv.set(x, y, c)
    # trim (inner border)
    tk_hi = 3 if state != "pressed" else 2
    tk_lo = 3 if state == "hover" else 1
    for x in range(x0 + 1, x1):
        cv.set(x, y0 + 1, color(trim, tk_hi))
        cv.set(x, y1 - lip - 1, color(trim, tk_lo))
    for y in range(y0 + 1, y1 - lip):
        cv.set(x0 + 1, y, color(trim, tk_hi))
        cv.set(x1 - 1, y, color(trim, tk_lo))
    # outline with rounded corners
    for x in range(x0 + 1, x1):
        cv.set(x, y0, color("outline")); cv.set(x, y1, color("outline"))
    for y in range(y0 + 1, y1):
        cv.set(x0, y, color("outline")); cv.set(x1, y, color("outline"))
    if state == "hover":  # little sparkle in the corner
        cv.set(x0 + 3, y0 + 3, color("white"))
        cv.set(x1 - 3, y0 + 3, color("white"))

    text_col = color("white") if state != "disabled" else color("stone", 3)
    shadow = color(ramp, 0) if state != "disabled" else color("stone", 1)
    cy = y0 + (y1 - lip - y0 - FONT_H) // 2 + 1
    if label:
        tx = (w - text_width(label)) // 2
        draw_text(cv, label, tx, cy + 1, shadow)
        draw_text(cv, label, tx, cy, text_col)
    if icon == "pause":
        for bx in (w // 2 - 3, w // 2 + 1):
            cv.rect(bx, cy + 1, bx + 1, cy + FONT_H, shadow)
            cv.rect(bx, cy, bx + 1, cy + FONT_H - 1, text_col)
    if icon == "sword":
        sx = 6
        for i in range(7):
            cv.set(sx + i, cy + 6 - i + 1, shadow)
            cv.set(sx + i, cy + 6 - i, color("steel", 4) if i > 1 else color("gold", 3))
        cv.set(sx + 1, cy + 4, color("gold", 3)); cv.set(sx + 3, cy + 6, color("gold", 3))
    return cv


def panel(size=48):
    cv = Canvas(size, size)
    for y in range(size):
        for x in range(size):
            cv.px[y][x] = color("sky", 1) if y > 6 else color("sky", 2)
    for i in range(size):
        for (x, y) in ((i, 0), (i, size - 1), (0, i), (size - 1, i)):
            cv.set(x, y, color("outline"))
        for (x, y, k) in ((i, 1, 3), (i, size - 2, 1), (1, i, 3), (size - 2, i, 1)):
            if 0 < x < size - 1 and 0 < y < size - 1:
                cv.set(x, y, color("gold", k))
        for (x, y) in ((i, 2), (2, i)):
            if 1 < x < size - 2 and 1 < y < size - 2:
                cv.set(x, y, color("gold", 1))
    for cx, cy in ((3, 3), (size - 5, 3), (3, size - 5), (size - 5, size - 5)):  # rivets
        cv.rect(cx, cy, cx + 1, cy + 1, color("gold", 2))
        cv.set(cx, cy, color("gold", 3))
        cv.set(cx + 1, cy + 1, color("gold", 0))
    # transparent corners for a softer silhouette
    for x, y in ((0, 0), (size - 1, 0), (0, size - 1), (size - 1, size - 1)):
        cv.px[y][x] = None
    return cv


LABELS = {  # file suffix -> (text, face, icon)
    "Jogar": ("JOGAR", "blue", None),
    "Opcoes": ("OPÇÕES", "blue", None),
    "Creditos": ("CRÉDITOS", "blue", None),
    "Sair": ("SAIR", "blue", None),
    # "Atacar" agora vem de attack_button.py (botão compacto da partida).
    "Pausa": ("PAUSA", "blue", None),
}

# 9-slice borders (left, bottom, right, top) in pixels, as Unity expects them.
BORDERS = {
    "Button_Blank": (4, 5, 4, 4),
    "Panel": (8, 8, 8, 8),
}


def main():
    os.makedirs(UI_DIR, exist_ok=True)
    lg = logo()
    lg.to_image().save(os.path.join(UI_DIR, "Logo.png"))
    bg, ground = title_background()
    bg.to_image().save(os.path.join(UI_DIR, "Title_Background.png"))
    states = ["normal", "hover", "pressed"]
    sheets = {}
    for key, (text, face, icon) in LABELS.items():
        frames = [button(BW, BH, s, face, text, icon) for s in states]
        sheets[key] = frames
        save_sheet(frames, os.path.join(UI_DIR, f"Button_{key}.png"))
    blank = [button(BW, BH, s) for s in states + ["disabled"]]
    save_sheet(blank, os.path.join(UI_DIR, "Button_Blank.png"))
    pause = [button(20, 20, s, icon="pause") for s in states]
    save_sheet(pause, os.path.join(UI_DIR, "Button_Pause_Icon.png"))
    panel().to_image().save(os.path.join(UI_DIR, "Panel.png"))

    # previews ---------------------------------------------------------
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    comp = title_screen_preview(bg, ground, lg)
    comp.to_image().resize((bg.w * 3, bg.h * 3), Image.NEAREST).save(
        os.path.join(PREVIEW_DIR, "Title_preview.png"))
    rows = list(sheets.values()) + [blank, pause]
    sc = 5
    board = Image.new("RGBA", ((BW * 4 + 20) * sc, (BH + 6) * len(rows) * sc + 40 * sc), (40, 36, 56, 255))
    for r, frames in enumerate(rows):
        for i, f in enumerate(frames):
            im = f.to_image()
            board.alpha_composite(im.resize((im.width * sc, im.height * sc), Image.NEAREST),
                                  ((4 + i * (BW + 4)) * sc, (4 + r * (BH + 6)) * sc))
    p = panel().to_image()
    board.alpha_composite(p.resize((p.width * sc // 2 * 2 // 2, p.height * sc // 2 * 2 // 2), Image.NEAREST),
                          (4 * sc, (4 + len(rows) * (BH + 6)) * sc))
    board.save(os.path.join(PREVIEW_DIR, "Buttons_preview.png"))


if __name__ == "__main__":
    main()
