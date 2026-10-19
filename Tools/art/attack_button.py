"""Compact Atacar button for the match HUD (smaller than the 72x20 menu buttons).

    python Tools/art/attack_button.py

Output (Assets/Art/UI/Original):
    Button_Atacar.png   3 frames (normal, hover, pressed). Each button is 58x16, centred in a
                        72x20 cell so the sheet keeps the slicing of the menu buttons;
                        sword icon on the left, "ATACAR" on the right.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from pixel import Canvas, color, save_sheet
from font import draw_text, H as FONT_H
from ui import button, UI_DIR, BW, BH

W, H = 58, 16
TEXT_X = 17  # right of the sword icon (drawn by button() at x 6..12)


def frame(state):
    cv = button(W, H, state, face="red", icon="sword")
    press = 1 if state == "pressed" else 0
    lip = 0 if press else 2
    cy = press + (H - 1 - lip - press - FONT_H) // 2 + 1
    draw_text(cv, "ATACAR", TEXT_X, cy + 1, color("red", 0))
    draw_text(cv, "ATACAR", TEXT_X, cy, color("white"))

    cell = Canvas(BW, BH)  # same 72x20 cell as the menu buttons, button centred, rest transparent
    ox, oy = (BW - W) // 2, (BH - H) // 2
    for y in range(H):
        for x in range(W):
            if cv.px[y][x] is not None:
                cell.px[y + oy][x + ox] = cv.px[y][x]
    return cell


def main():
    frames = [frame(s) for s in ("normal", "hover", "pressed")]
    save_sheet(frames, os.path.join(UI_DIR, "Button_Atacar.png"))
    print("Button_Atacar.png ok")


if __name__ == "__main__":
    main()
