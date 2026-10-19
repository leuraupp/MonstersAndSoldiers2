"""Board cell tile (GDD 9: casas vazia, grama, destaque, erro).

    python Tools/art/cell.py

Output (Assets/Art/Board/Original):
    Cell.png   34x34, one frame. Light and neutral on purpose: GridCell tints it
               (empty, soldier, grass, highlight, error), so the colours come from the tint.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from pixel import Canvas, color, save_sheet, preview, OUT_DIR, PREVIEW_DIR

BOARD_DIR = os.path.join(OUT_DIR, "Board", "Original")
SIZE = 34  # 34 px at PPU 32 ~ the 1.08 units of the old tile, so the board layout does not move

BAYER4 = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def tile() -> Canvas:
    cv = Canvas(SIZE, SIZE)
    last = SIZE - 1
    face = color("stone", 4)       # #d6d0dc
    light = color("white")         # #f4f1de
    mid = color("stone", 3)        # #a39cad
    dark = color("stone", 2)       # #6e6878
    edge = color("stone", 1)       # #46404f

    for y in range(SIZE):
        for x in range(SIZE):
            # Rounded corners: leave the 4 corner pixels empty.
            if (x in (0, last)) and (y in (0, last)):
                continue
            if x == 0 or y == 0 or x == last or y == last:
                cv.set(x, y, edge)
            elif x == 1 or y == 1:
                cv.set(x, y, light)            # bevel: light from the top-left
            elif x == last - 1 or y == last - 1:
                cv.set(x, y, mid)              # bevel: shade on the bottom-right
            elif x == last - 2 or y == last - 2:
                # soft step between face and shade, dithered so it reads as stone
                cv.set(x, y, mid if BAYER4[y % 4][x % 4] > 7 else face)
            else:
                cv.set(x, y, face)

    # A few chips and specks, fixed positions so every cell matches.
    for (x, y) in [(7, 9), (8, 9), (22, 6), (26, 21), (11, 25), (12, 25), (18, 15)]:
        cv.set(x, y, mid)
    for (x, y) in [(8, 10), (27, 22), (12, 26)]:
        cv.set(x, y, dark)
    return cv


def main():
    cell = tile()
    save_sheet([cell], os.path.join(BOARD_DIR, "Cell.png"))
    preview([("Cell", [cell])], os.path.join(PREVIEW_DIR, "Cell_preview.png"), scale=8)
    print("Cell.png ok")


if __name__ == "__main__":
    main()
