"""Board frame behind the cells (GDD 9): a patch of meadow, like the title screen ground.

    python Tools/art/board.py

Output (Assets/Art/Board/Original):
    BoardFrame.png   48x48, 9-slice with 10 px borders: packed-dirt edge topped with grass
                     tufts around a dark-grass interior. Unity draws it Tiled, so the
                     interior specks repeat instead of stretching.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from pixel import Canvas, color, save_sheet, preview, OUT_DIR, PREVIEW_DIR

BOARD_DIR = os.path.join(OUT_DIR, "Board", "Original")
SIZE = 48
BORDER = 10  # keep in sync with Phase4Setup.BoardFrameBorder


def frame() -> Canvas:
    cv = Canvas(SIZE, SIZE)
    last = SIZE - 1
    for y in range(SIZE):
        for x in range(SIZE):
            d = min(x, y, last - x, last - y)  # distance to the nearest edge
            if d == 0:
                c = color("outline")
            elif d <= 2:
                c = color("grass", 4 if (x * 5 + y * 3) % 7 == 0 else 3)   # grass tufts on the rim
            elif d <= BORDER - 3:
                c = color("wood", 1 if (x * 3 + y * 7) % 11 == 0 else 2)   # packed dirt
            elif d == BORDER - 2:
                c = color("wood", 0)
            elif d == BORDER - 1:
                c = color("outline")
            else:
                c = color("grass", 2 if (x * 7 + y * 13) % 23 == 0 else 1)  # dark grass, sparse specks
            cv.set(x, y, c)

    for x, y in ((0, 0), (last, 0), (0, last), (last, last)):  # softer silhouette
        cv.px[y][x] = None
    return cv


def main():
    f = frame()
    save_sheet([f], os.path.join(BOARD_DIR, "BoardFrame.png"))
    preview([("BoardFrame", [f])], os.path.join(PREVIEW_DIR, "BoardFrame_preview.png"), scale=6)
    print("BoardFrame.png ok")


if __name__ == "__main__":
    main()
