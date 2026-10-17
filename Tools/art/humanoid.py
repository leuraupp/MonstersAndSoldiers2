"""Paper-doll humanoid renderer shared by the Paladin, Goblin and Orc.

A character is a Spec (proportions, colours and draw hooks for head/torso/weapon).
Each animation frame is a Pose (offsets, arm and weapon angles, flags). Right is drawn
directly; Left is Right mirrored; Down and Up have their own layouts.

Angles are degrees measured from straight up, positive = rotating "forward"
(toward the facing direction in profile, outward from the body in Down/Up).
"""
from __future__ import annotations

import math
import os
import sys
from dataclasses import dataclass, field, replace
from typing import Callable

sys.path.insert(0, os.path.dirname(__file__))
from pixel import (Canvas, color, ground_shadow, save_sheet, preview, gif,
                   OUT_DIR, PREVIEW_DIR)

S = 32
GROUND = 28
FACINGS = ["Down", "Left", "Right", "Up"]


@dataclass
class Pose:
    ox: float = 0          # forward lunge (+ toward facing)
    oy: float = 0          # vertical offset (+ down)
    arm: float = 170       # weapon arm angle
    wpn: float = 40        # weapon angle
    off_arm: float = 175   # shield / free arm angle
    stride: int = 0        # -1, 0, 1 leg offset
    crouch: int = 0        # legs shortened by n px
    face: str = "normal"   # normal | angry | pain | dead | blink
    flash: bool = False
    trail: tuple | None = None   # (from_angle, to_angle) swing arc
    impact: bool = False
    lying: bool = False
    fade: int = 0          # 0..3 dither steps
    poof: int = 0          # 0..3 smoke puff growing around a fallen body


@dataclass
class Spec:
    name: str
    skin: str
    leg_h: int
    leg_w: int
    torso_w: int
    torso_h: int
    head_w: int
    head_h: int
    arm_len: int
    arm_w: int
    pants: str
    torso_ramp: str
    draw_head: Callable        # (cv, cx, top, facing, pose, spec)
    draw_torso: Callable       # (cv, x0, y0, x1, y1, facing, pose, spec)
    draw_weapon: Callable      # (cv, hx, hy, ux, uy, facing, spec) unit vector u along weapon
    weapon_len: int
    draw_offhand: Callable | None = None   # (cv, hx, hy, facing, spec, front: bool)
    boots: str = "leather"
    sleeve: str | None = None  # ramp for upper arm, None = skin
    hand: str | None = None    # ramp for hands (gauntlets)


# ------------------------------------------------------------------ primitives

def shade_box(cv, x0, y0, x1, y1, ramp, round_top=False, round_bottom=False, light_left=True):
    x0, y0, x1, y1 = int(x0), int(y0), int(x1), int(y1)
    w = max(1, x1 - x0)
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if round_top and y == y0 and x in (x0, x1) and x1 - x0 >= 3:
                continue
            if round_bottom and y == y1 and x in (x0, x1) and x1 - x0 >= 3:
                continue
            t = (x - x0) / w if light_left else 1 - (x - x0) / w
            k = 3 if t < 0.3 else 2 if t < 0.72 else 1
            if y == y1:
                k = max(0, k - 1)
            if y == y0 and k < 3:
                k += 0  # keep
            cv.set(x, y, color(ramp, k))


def thick_line(cv, x0, y0, x1, y1, w, ramp, k=2):
    n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
    for i in range(n):
        t = i / max(1, n - 1)
        x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
        for a in range(w):
            for b in range(w):
                cv.set(x + a - (w - 1) / 2, y + b - (w - 1) / 2,
                       color(ramp, k + (1 if a == 0 and w > 1 else 0)))


def adir(angle, fwd):
    r = math.radians(angle)
    return math.sin(r) * fwd, -math.cos(r)


def arc(cv, cx, cy, r, a0, a1, fwd, c):
    lo, hi = sorted((a0, a1))
    a = lo
    while a <= hi:
        dx, dy = adir(a, fwd)
        cv.set(cx + dx * r, cy + dy * r, c)
        a += 4


def dither_fade(cv, step):
    """Fades the sprite with real alpha (Unity handles it fine): 0 = opaque."""
    if step <= 0:
        return cv
    a = {1: 0.6, 2: 0.35, 3: 0.15}[min(3, step)]
    for y in range(cv.h):
        for x in range(cv.w):
            p = cv.px[y][x]
            if p is not None:
                cv.px[y][x] = (p[0], p[1], p[2], int(p[3] * a))
    return cv


def rotate90(cv, clockwise=True):
    out = Canvas(cv.h, cv.w)
    for y in range(cv.h):
        for x in range(cv.w):
            if clockwise:
                out.px[x][cv.h - 1 - y] = cv.px[y][x]
            else:
                out.px[cv.w - 1 - x][y] = cv.px[y][x]
    return out


def whiten(cv):
    for y in range(cv.h):
        for x in range(cv.w):
            p = cv.px[y][x]
            if p is not None and p[3] == 255 and p != color("outline"):
                cv.px[y][x] = color("white")


# ------------------------------------------------------------------ body

def render(spec: Spec, facing: str, pose: Pose) -> Canvas:
    if facing == "Left":
        return render(spec, "Right", pose).flipped()
    if pose.lying:
        return render_lying(spec, facing, pose)

    cv = Canvas(S, S)
    body = Canvas(S, S)
    fwd = 1
    cx = 16 + (pose.ox if facing == "Right" else 0)
    gy = GROUND + (pose.oy if True else 0) + (pose.ox * 0.4 if facing == "Down" else
                                              -pose.ox * 0.4 if facing == "Up" else 0)
    ground_shadow(cv, 16 + (pose.ox if facing == "Right" else 0),
                  GROUND + 0.5 + (pose.ox * 0.4 if facing == "Down" else -pose.ox * 0.4 if facing == "Up" else 0),
                  spec.torso_w * 0.5 + 2, 2.0)

    leg_h = spec.leg_h - pose.crouch
    hip = gy - leg_h
    t_top = hip - spec.torso_h
    tx0 = int(round(cx - spec.torso_w / 2))
    tx1 = tx0 + spec.torso_w - 1
    head_top = t_top - spec.head_h + 1
    sh_y = t_top + 1

    if facing == "Down":
        weapon_side, off_side, wfwd = -1, 1, -1      # weapon in character's right hand = screen left
    elif facing == "Up":
        weapon_side, off_side, wfwd = 1, -1, 1
    else:
        weapon_side, off_side, wfwd = 1, -1, 1

    def shoulder(side):
        if facing == "Right":
            return (cx + (1 if side > 0 else -2), sh_y)
        return (tx0 - 1 if side < 0 else tx1 + 1, sh_y)

    def hand_pos(side, angle):
        sx, sy = shoulder(side)
        f = wfwd if side == weapon_side else -wfwd
        if facing == "Right":
            f = 1
        dx, dy = adir(angle, f)
        return sx + dx * spec.arm_len, sy + dy * spec.arm_len, f

    def draw_arm(c, side, angle):
        sx, sy = shoulder(side)
        hx, hy, _ = hand_pos(side, angle)
        thick_line(c, sx, sy, hx, hy, spec.arm_w, spec.sleeve or spec.skin)
        c.rect(hx - spec.arm_w / 2 + 0.5, hy - 0.5, hx + spec.arm_w / 2 - 0.5, hy + 0.5,
               color(spec.hand or spec.skin, 2))
        return hx, hy

    def draw_weapon(c):
        hx, hy, f = hand_pos(weapon_side, pose.arm)
        ux, uy = adir(pose.wpn, f)
        spec.draw_weapon(c, hx, hy, ux, uy, facing, spec)
        return hx, hy, f

    def legs(c):
        lw = spec.leg_w
        if facing == "Right":
            for i, off in enumerate((-1 - pose.stride, 1 + pose.stride)):
                x0 = int(round(cx - lw / 2 + off * 0.8))
                k = 1 if i == 0 else 2
                c.rect(x0, hip, x0 + lw - 1, gy - 2, color(spec.pants, k))
                c.rect(x0, gy - 1, x0 + lw, gy, color(spec.boots, k))
        else:
            for side in (-1, 1):
                lift = 1 if pose.stride == side else 0
                x0 = int(round(cx - 0.5 + (side * (lw / 2 + 0.5)) - lw / 2)) + (0 if side < 0 else 1)
                x0 = int(cx - lw - 0) if side < 0 else int(cx + 1 - (spec.torso_w % 2 == 0))
                shade_box(c, x0, hip, x0 + lw - 1, gy - 2 - lift, spec.pants)
                c.rect(x0, gy - 1 - lift, x0 + lw - 1, gy - lift, color(spec.boots, 2 if side < 0 else 1))

    # back layer -------------------------------------------------------
    if facing == "Up":
        draw_weapon(body)
        if spec.draw_offhand:
            hx, hy, _ = hand_pos(off_side, pose.off_arm)
            spec.draw_offhand(body, hx, hy, facing, spec, False)
    if facing == "Right":
        draw_arm(body, off_side, pose.off_arm)
        if spec.draw_offhand:
            hx, hy, _ = hand_pos(off_side, pose.off_arm)
            spec.draw_offhand(body, hx, hy, facing, spec, False)

    legs(body)
    spec.draw_torso(body, tx0, t_top, tx1, hip, facing, pose, spec)

    if facing in ("Down", "Up"):
        draw_arm(body, weapon_side, pose.arm)
        draw_arm(body, off_side, pose.off_arm)

    head_cx = cx + (1 if facing == "Right" else 0)
    spec.draw_head(body, head_cx, head_top, facing, pose, spec)

    if facing == "Down" and spec.draw_offhand:
        hx, hy, _ = hand_pos(off_side, pose.off_arm)
        spec.draw_offhand(body, hx, hy, facing, spec, True)
    if facing == "Right":
        draw_arm(body, weapon_side, pose.arm)
    if facing != "Up":
        hx, hy, f = draw_weapon(body)
        # hand on top of the grip
        body.rect(hx - spec.arm_w / 2 + 0.5, hy - 0.5, hx + spec.arm_w / 2 - 0.5, hy + 0.5,
                  color(spec.hand or spec.skin, 2))

    body.outline()
    if pose.flash:
        whiten(body)

    fx_layer = Canvas(S, S)
    if pose.trail:
        hx, hy, f = hand_pos(weapon_side, pose.arm)
        sx, sy = shoulder(weapon_side)
        r = spec.arm_len + spec.weapon_len - 1
        for rr, c in ((r, color("white")), (r - 1, color("white", alpha=255)), (r - 2, color("steel", 3))):
            arc(fx_layer, sx, sy, rr, pose.trail[0], pose.trail[1], f, c)
    if pose.impact:
        hx, hy, f = hand_pos(weapon_side, pose.arm)
        ux, uy = adir(pose.wpn, f)
        ix, iy = hx + ux * spec.weapon_len, hy + uy * spec.weapon_len
        star(fx_layer, ix, iy)

    cv.over(body)
    cv.over(fx_layer)
    return dither_fade(cv, pose.fade)


def star(cv, x, y, r=2):
    w = color("white")
    for d in range(1, r + 2):
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            cv.set(x + dx * d, y + dy * d, w)
    for dx, dy in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
        cv.set(x + dx * (r), y + dy * (r), color("gold", 3))


def render_lying(spec, facing, pose):
    """Fallen body: the standing (neutral) sprite rotated 90 degrees, lossless."""
    stand_facing = "Right" if facing == "Up" else facing
    standing = render(spec, stand_facing, replace(pose, lying=False, fade=0, flash=False,
                                                  arm=178, wpn=178, off_arm=176, face="dead",
                                                  trail=None, impact=False, ox=0, oy=0))
    # strip the shadow before rotating
    sh = color("shadow", alpha=110)
    for y in range(S):
        for x in range(S):
            if standing.px[y][x] == sh:
                standing.px[y][x] = None
    rot = rotate90(standing, clockwise=(facing != "Left"))
    # find content bbox and set it on the floor, centred
    xs = [x for y in range(S) for x in range(S) if rot.px[y][x] is not None]
    ys = [y for y in range(S) for x in range(S) if rot.px[y][x] is not None]
    cv = Canvas(S, S)
    if not xs:
        return cv
    bw = max(xs) - min(xs) + 1
    ground_shadow(cv, 16, GROUND + 0.5, bw * 0.5 + 1, 2.0)
    ox = 16 - (min(xs) + bw // 2)
    oy = GROUND - max(ys)
    cv.over(rot, ox, oy)
    if pose.flash:
        whiten(cv)
    dither_fade(cv, pose.fade)
    if pose.poof:
        smoke(cv, 16, GROUND - 3, pose.poof)
    return cv


def smoke(cv, cx, cy, step):
    """Puffs of smoke that swell and rise as the body vanishes (step 1..3)."""
    puffs = [(-7, 0), (-3, -2), (1, 0), (5, -2), (8, 1), (-1, -4)]
    r = {1: 2.2, 2: 3.0, 3: 3.4}[step]
    a = {1: 240, 2: 200, 3: 120}[step]
    rise = (step - 1) * 2
    for i, (dx, dy) in enumerate(puffs):
        if step == 1 and i % 2:
            continue
        spread = 1 + 0.25 * (step - 1)
        px, py = cx + dx * spread, cy + dy - rise
        rr = r - (0.6 if i % 3 == 2 else 0)
        for y in range(int(py - rr - 1), int(py + rr + 2)):
            for x in range(int(px - rr - 1), int(px + rr + 2)):
                d = ((x + .5 - px) ** 2 + (y + .5 - py) ** 2) ** 0.5
                if d <= rr:
                    k = 4 if y + .5 < py else 3
                    cv.set(x, y, color("stone", k, alpha=a))


# ------------------------------------------------------------------ animation set

def poses(spec: Spec):
    rest = Pose()
    # idle breathing over 8 frames: body sinks 1px for half the cycle, the weapon sways with it
    # and the character blinks once per loop
    breath = [0, 0, 0, 1, 1, 1, 1, 0]
    sway = [40, 41, 43, 45, 46, 45, 43, 41]
    idle = [replace(rest, crouch=c, wpn=w, arm=170 + (2 if c else 0), off_arm=175 - c,
                    face="blink" if i == 6 else "normal")
            for i, (c, w) in enumerate(zip(breath, sway))]
    attack = [
        replace(rest, ox=-1, arm=-10, wpn=-40, face="angry"),
        replace(rest, ox=-1, crouch=1, arm=-25, wpn=-70, face="angry"),
        replace(rest, ox=3, stride=1, arm=70, wpn=80, face="angry", trail=(-60, 80)),
        replace(rest, ox=4, stride=1, crouch=1, arm=120, wpn=140, face="angry",
                trail=(30, 140), impact=True),
        replace(rest, ox=2, arm=150, wpn=110, face="angry"),
        replace(rest),
    ]
    hit = [replace(rest, flash=True),
           replace(rest, ox=-2, arm=175, wpn=60, face="pain", off_arm=160),
           replace(rest, ox=-1, face="pain")]
    death = [replace(rest, flash=True, face="pain"),
             replace(rest, ox=-2, arm=178, wpn=100, face="pain"),
             replace(rest, ox=-1, crouch=3, oy=0, arm=178, wpn=120, off_arm=178, face="dead"),
             replace(rest, lying=True),
             replace(rest, lying=True, fade=1, poof=1),
             replace(rest, lying=True, fade=2, poof=2),
             replace(rest, lying=True, fade=3, poof=3)]
    return {"Idle": idle, "Attack": attack, "Hit": hit, "Death": death}


def build(spec: Spec, write_assets=True):
    out = os.path.join(OUT_DIR, "Characters", spec.name)
    rows = []
    for name, ps in poses(spec).items():
        for facing in FACINGS:
            frames = [render(spec, facing, p) for p in ps]
            if write_assets:
                save_sheet(frames, os.path.join(out, f"{spec.name}_{name}_{facing}.png"))
            rows.append((f"{name} {facing}", frames))
            if facing in ("Down", "Right"):
                gif(frames, os.path.join(PREVIEW_DIR, f"{spec.name}_{name}_{facing}.gif"),
                    ms=160 if name == "Idle" else 110)
    preview(rows, os.path.join(PREVIEW_DIR, f"{spec.name}_preview.png"), scale=5)
    return rows
