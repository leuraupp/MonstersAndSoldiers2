"""Paladin, Goblin and Orc specs on top of humanoid.py.

    python Tools/art/characters.py            # all three
    python Tools/art/characters.py Paladin    # just one
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from pixel import color
from humanoid import Spec, shade_box, thick_line, build


# ------------------------------------------------------------------ shared bits

def blade(cv, hx, hy, ux, uy, length, ramp="steel", width=2, start=2):
    px, py = -uy, ux  # perpendicular
    for i in range(start, length + 1):
        x, y = hx + ux * i, hy + uy * i
        cv.set(x, y, color(ramp, 3))
        if width > 1 and i < length:
            cv.set(x + px * 0.9, y + py * 0.9, color(ramp, 2 if i % 2 else 1))
    cv.set(hx + ux * length, hy + uy * length, color(ramp, 4 if ramp == "steel" else 3))


def eyes_front(cv, cx, y, pose, iris, gap=2, wide=1):
    blk = color("black")
    if pose.face == "dead":
        for x in (cx - gap - 1, cx + gap):
            cv.set(x - 1, y - 1, blk); cv.set(x + 1, y + 1, blk)
            cv.set(x + 1, y - 1, blk); cv.set(x - 1, y + 1, blk); cv.set(x, y, blk)
        return
    if pose.face in ("pain", "blink"):
        for x in (cx - gap - 1, cx + gap):
            cv.set(x - 1, y, blk); cv.set(x, y, blk)
        return
    for i, x in enumerate((cx - gap - wide, cx + gap)):
        cv.rect(x, y - 1, x + wide - 1, y, color(iris))
        cv.set(x + (wide - 1 if i == 0 else 0), y, blk)
        if pose.face == "angry":
            bx = x + wide - 1 if i == 0 else x
            cv.set(bx, y - 2, blk)
            cv.set(bx + (1 if i == 0 else -1), y - 2, blk)


def eye_side(cv, x, y, pose, iris):
    blk = color("black")
    if pose.face == "dead":
        cv.set(x - 1, y - 1, blk); cv.set(x + 1, y + 1, blk)
        cv.set(x + 1, y - 1, blk); cv.set(x - 1, y + 1, blk); cv.set(x, y, blk)
        return
    if pose.face in ("pain", "blink"):
        cv.set(x - 1, y, blk); cv.set(x, y, blk)
        return
    cv.set(x, y - 1, color(iris)); cv.set(x, y, blk)
    if pose.face == "angry":
        cv.set(x - 1, y - 2, blk); cv.set(x, y - 2, blk)


# ------------------------------------------------------------------ Paladin

def paladin_head(cv, cx, top, facing, pose, spec):
    w, h = spec.head_w, spec.head_h
    x0 = int(cx - w // 2)
    x1 = x0 + w - 1
    shade_box(cv, x0, top, x1, top + h - 1, "steel", round_top=True)
    # gold brow band
    cv.hline(x0, x1, top + 2, color("gold", 2))
    cv.set(x0, top + 2, color("gold", 1))
    slit_y = top + 4
    blk = color("black")
    glow = {"normal": color("blue", 3), "angry": color("red", 3)}.get(pose.face)
    if facing == "Down":
        cv.hline(x0 + 1, x1 - 1, slit_y, blk)
        cv.set(cx, slit_y, color("steel", 2))            # nose guard
        cv.set(cx, slit_y + 1, color("steel", 2))
        if glow:
            cv.set(cx - 2, slit_y, glow); cv.set(cx + 2, slit_y, glow)
        for x in range(x0 + 2, x1 - 1, 2):               # breathing holes
            cv.set(x, top + 6, color("steel", 1))
        plume_x = cx - 1
        cv.rect(plume_x, top - 3, plume_x + 2, top - 1, color("blue", 2))
        cv.set(plume_x, top - 3, color("blue", 3)); cv.set(plume_x + 2, top - 1, color("blue", 1))
    elif facing == "Right":
        cv.hline(cx, x1, slit_y, blk)
        if glow:
            cv.set(x1 - 1, slit_y, glow)
        cv.set(x1, top + 6, color("steel", 1))
        # plume sweeping back
        cv.rect(cx - 3, top - 2, cx, top - 1, color("blue", 2))
        cv.rect(x0 - 1, top - 1, cx - 3, top + 1, color("blue", 1))
        cv.set(cx - 1, top - 2, color("blue", 3))
    else:  # Up
        cv.rect(cx - 1, top - 3, cx + 1, top - 1, color("blue", 2))
        cv.rect(cx - 1, top, cx + 1, top + 3, color("blue", 1))   # plume falls down the back
        cv.set(cx, top - 3, color("blue", 3))


def paladin_torso(cv, x0, y0, x1, y1, facing, pose, spec):
    shade_box(cv, x0, y0, x1, y1, "steel")
    cx = (x0 + x1) // 2
    if facing == "Down":
        shade_box(cv, x0 + 2, y0 + 1, x1 - 2, y1 + 1, "blue")
        cv.set(cx, y0 + 2, color("gold", 3)); cv.hline(cx - 1, cx + 1, y0 + 3, color("gold", 2))
        cv.set(cx, y0 + 4, color("gold", 2))
        cv.hline(x0, x1, y1 - 1, color("leather", 2))
        cv.set(cx, y1 - 1, color("gold", 3))
        # pauldrons
        for x in (x0 - 1, x1 + 1):
            cv.rect(min(x, x + (1 if x < cx else -1)), y0, max(x, x + (1 if x < cx else -1)), y0 + 1,
                    color("steel", 3 if x < cx else 2))
    elif facing == "Right":
        shade_box(cv, cx, y0 + 1, x1, y1 + 1, "blue")
        cv.hline(x0, x1, y1 - 1, color("leather", 2))
        cv.rect(x0 + 1, y0, x0 + 3, y0 + 2, color("steel", 3))       # pauldron
    else:
        shade_box(cv, x0, y0, x1, y1 + 2, "blue", light_left=False)   # cape
        cv.hline(x0, x1, y0, color("gold", 2))


def paladin_sword(cv, hx, hy, ux, uy, facing, spec):
    px, py = -uy, ux
    # pommel + grip behind the hand
    cv.set(hx - ux * 2, hy - uy * 2, color("gold", 3))
    cv.set(hx - ux, hy - uy, color("leather", 2))
    # crossguard
    gx, gy = hx + ux * 1.4, hy + uy * 1.4
    for k in (-2, -1, 0, 1, 2):
        cv.set(gx + px * k, gy + py * k, color("gold", 3 if k < 0 else 2))
    blade(cv, hx, hy, ux, uy, spec.weapon_len, "steel", 2, start=2)


def paladin_shield(cv, hx, hy, facing, spec, front):
    hx, hy = int(round(hx)), int(round(hy))
    if facing == "Down" and front:
        x0, y0 = hx - 3, hy - 4
        shade_box(cv, x0, y0, x0 + 6, y0 + 6, "blue", round_bottom=True)
        cv.rect(x0 + 1, y0 + 7, x0 + 5, y0 + 7, color("blue", 1))
        cv.set(x0 + 3, y0 + 8, color("blue", 1))
        for x in range(x0, x0 + 7):
            cv.set(x, y0, color("gold", 3))
        cv.rect(x0 + 3, y0 + 2, x0 + 3, y0 + 6, color("gold", 3))
        cv.rect(x0 + 1, y0 + 3, x0 + 5, y0 + 3, color("gold", 2))
    elif facing == "Right":
        # far arm: shield edge peeking out behind the body
        cv.rect(hx - 2, hy - 5, hx, hy + 2, color("blue", 1))
        cv.rect(hx - 2, hy - 5, hx - 2, hy + 2, color("gold", 2))
    elif facing == "Up":
        x0, y0 = hx - 3, hy - 4
        shade_box(cv, x0, y0, x0 + 6, y0 + 7, "wood", round_bottom=True)
        cv.rect(x0 + 1, y0 + 3, x0 + 5, y0 + 3, color("steel", 2))


PALADIN = Spec(
    name="Paladin", skin="steel", leg_h=5, leg_w=3, torso_w=10, torso_h=7,
    head_w=9, head_h=8, arm_len=5, arm_w=2, pants="steel", torso_ramp="steel",
    draw_head=paladin_head, draw_torso=paladin_torso, draw_weapon=paladin_sword,
    weapon_len=10, draw_offhand=paladin_shield, boots="leather", sleeve="steel", hand="steel")


# ------------------------------------------------------------------ Goblin

def goblin_head(cv, cx, top, facing, pose, spec):
    w, h = spec.head_w, spec.head_h
    x0 = int(cx - w // 2)
    x1 = x0 + w - 1
    sk = spec.skin
    ear = lambda x, y: cv.set(x, y, color(sk, 2))
    if facing in ("Down", "Up"):
        for i in range(4):  # long ears pointing out and slightly up
            for s, xb in ((-1, x0), (1, x1)):
                cv.set(xb + s * (i + 1), top + 3 - i // 2, color(sk, 2 if s < 0 else 1))
                if i < 3:
                    cv.set(xb + s * (i + 1), top + 4 - i // 2, color(sk, 1))
    shade_box(cv, x0, top, x1, top + h - 1, sk, round_top=True, round_bottom=True)
    if facing == "Down":
        eyes_front(cv, cx, top + 4, pose, "gold" if True else None, gap=1, wide=2)
        cv.set(cx, top + 5, color(sk, 1))                     # nose
        my = top + 6
        if pose.face in ("angry", "pain"):
            cv.hline(cx - 2, cx + 1, my, color("black"))
            cv.set(cx - 2, my - 0, color("white")); cv.set(cx + 1, my, color("white"))
        else:
            cv.hline(cx - 1, cx + 1, my, color("black"))
            cv.set(cx + 1, my, color("white"))
    elif facing == "Right":
        # ear swept back
        for i in range(5):
            cv.set(x0 - i, top + 2 + i // 2, color(sk, 1 if i % 2 else 2))
            if i < 4:
                cv.set(x0 - i, top + 3 + i // 2, color(sk, 1))
        cv.set(x1 + 1, top + 4, color(sk, 2))                 # pointy nose
        cv.set(x1 + 1, top + 5, color(sk, 1))
        eye_side(cv, x1 - 2, top + 4, pose, "gold")
        cv.hline(x1 - 2, x1, top + 6, color("black"))
        cv.set(x1 - 1, top + 6, color("white"))
    else:
        # hood/hair tuft on the back of the head
        cv.rect(cx - 2, top, cx + 2, top + 2, color("leather", 1))


def goblin_torso(cv, x0, y0, x1, y1, facing, pose, spec):
    shade_box(cv, x0, y0, x1, y1, "leather", round_top=True)
    cx = (x0 + x1) // 2
    cv.hline(x0, x1, y1 - 1, color("leather", 0))
    if facing == "Down":
        cv.set(cx - 1, y0 + 2, color("leather", 3)); cv.set(cx + 1, y0 + 3, color("leather", 3))
        cv.line(x0 + 1, y0, x1 - 1, y1 - 2, color("wood", 1))  # strap
        cv.set(cx, y1 - 1, color("gold", 2))
    elif facing == "Right":
        cv.set(x1 - 1, y0 + 2, color("leather", 3))
    else:
        cv.rect(cx - 2, y0 + 1, cx + 1, y1 - 2, color("wood", 1))  # small sack
        cv.set(cx - 1, y0 + 1, color("wood", 3))


def goblin_dagger(cv, hx, hy, ux, uy, facing, spec):
    px, py = -uy, ux
    cv.set(hx - ux, hy - uy, color("wood", 2))
    for k in (-1, 0, 1):
        cv.set(hx + ux + px * k, hy + uy + py * k, color("wood", 1))
    blade(cv, hx, hy, ux, uy, spec.weapon_len, "steel", 1, start=2)


GOBLIN = Spec(
    name="Goblin", skin="goblin", leg_h=4, leg_w=2, torso_w=8, torso_h=6,
    head_w=10, head_h=8, arm_len=4, arm_w=2, pants="leather", torso_ramp="leather",
    draw_head=goblin_head, draw_torso=goblin_torso, draw_weapon=goblin_dagger,
    weapon_len=6, boots="leather")


# ------------------------------------------------------------------ Orc

def orc_head(cv, cx, top, facing, pose, spec):
    w, h = spec.head_w, spec.head_h
    x0 = int(cx - w // 2)
    x1 = x0 + w - 1
    sk = spec.skin
    shade_box(cv, x0, top + 1, x1, top + h - 1, sk, round_bottom=True)
    cv.hline(x0 + 1, x1 - 1, top, color(sk, 2))
    cv.hline(x0 + 1, x1 - 1, top + h - 1, color(sk, 0))
    if facing == "Down":
        cv.rect(cx - 1, top - 2, cx + 1, top, color("black"))          # topknot
        cv.set(cx, top - 3, color("black"))
        cv.hline(x0, x1, top + 3, color(sk, 1))                         # heavy brow
        eyes_front(cv, cx, top + 4, pose, "red", gap=2, wide=1)
        jaw = top + h - 2
        cv.hline(x0 + 1, x1 - 1, jaw, color(sk, 1))
        cv.set(cx - 2, jaw, color("white")); cv.set(cx - 2, jaw - 1, color("white"))
        cv.set(cx + 2, jaw, color("white")); cv.set(cx + 2, jaw - 1, color("white"))
        if pose.face in ("angry", "pain"):
            cv.hline(cx - 1, cx + 1, jaw, color("black"))
    elif facing == "Right":
        cv.rect(x0, top - 1, x0 + 2, top + 1, color("black"))
        cv.set(x0 - 1, top + 2, color("black")); cv.set(x0 - 1, top + 3, color("black"))
        cv.hline(cx, x1 + 1, top + 3, color(sk, 1))
        eye_side(cv, x1 - 1, top + 4, pose, "red")
        jaw = top + h - 2
        cv.rect(cx, jaw, x1 + 1, jaw + 1, color(sk, 1))
        cv.set(x1, jaw - 1, color("white")); cv.set(x1, jaw, color("white"))
        cv.set(x0 + 1, top + 4, color(sk, 1))   # ear
    else:
        cv.rect(cx - 1, top - 2, cx + 1, top + 2, color("black"))
        cv.set(cx, top - 3, color("black"))


def orc_torso(cv, x0, y0, x1, y1, facing, pose, spec):
    """Sleeveless leather vest open on the chest, iron pauldron, wide belt."""
    shade_box(cv, x0, y0, x1, y1, "leather", round_top=True,
              light_left=facing != "Up")
    cx = (x0 + x1) // 2
    if facing == "Down":
        shade_box(cv, cx - 2, y0, cx + 2, y1 - 3, spec.skin)
        cv.set(cx, y0 + 3, color(spec.skin, 1)); cv.set(cx, y0 + 5, color(spec.skin, 1))
        cv.rect(x0 - 1, y0 - 1, x0 + 3, y0 + 1, color("stone", 3))
        cv.hline(x0 - 1, x0 + 3, y0 + 1, color("stone", 2))
        cv.set(x0 + 1, y0 - 1, color("stone", 4))
    elif facing == "Right":
        shade_box(cv, cx + 1, y0 + 1, x1, y1 - 3, spec.skin)
        cv.rect(x0 + 2, y0 - 1, x0 + 6, y0 + 2, color("stone", 3))
        cv.hline(x0 + 2, x0 + 6, y0 + 2, color("stone", 2))
    else:
        cv.rect(x1 - 3, y0 - 1, x1 + 1, y0 + 1, color("stone", 2))
        cv.line(x0 + 2, y0 + 2, x1 - 2, y0 + 2, color("leather", 0))
    cv.hline(x0, x1, y1 - 2, color("leather", 0))
    cv.hline(x0, x1, y1 - 1, color("leather", 1))
    if facing != "Up":
        bx = cx if facing == "Down" else x1 - 2
        cv.rect(bx - 1, y1 - 2, bx, y1 - 1, color("gold", 2))


def orc_club(cv, hx, hy, ux, uy, facing, spec):
    px, py = -uy, ux
    cv.set(hx - ux, hy - uy, color("wood", 2))
    L = spec.weapon_len
    for i in range(1, L + 1):
        r = 0.6 + 1.6 * max(0, (i - L * 0.45) / (L * 0.55))
        for k in range(-int(r), int(r) + 1):
            cv.set(hx + ux * i + px * k, hy + uy * i + py * k,
                   color("wood", 3 if k < 0 else 2 if k == 0 else 1))
    # iron spikes on the head of the club
    for i, k in ((L - 1, 3), (L - 4, -3), (L - 2, -3)):
        cv.set(hx + ux * i + px * k, hy + uy * i + py * k, color("stone", 4))


ORC = Spec(
    name="Orc", skin="orc", leg_h=5, leg_w=4, torso_w=14, torso_h=9,
    head_w=10, head_h=8, arm_len=6, arm_w=3, pants="purple", torso_ramp="orc",
    draw_head=orc_head, draw_torso=orc_torso, draw_weapon=orc_club,
    weapon_len=10, boots="leather")


ALL = {"Paladin": PALADIN, "Goblin": GOBLIN, "Orc": ORC}

if __name__ == "__main__":
    names = sys.argv[1:] or list(ALL)
    for n in names:
        build(ALL[n])
