"""Board frame behind the cells (GDD 9), in the same style as the UI Panel.

    python Tools/art/board.py

Output (Assets/Art/Board/Original):
    BoardFrame.png   48x48, 9-slice with 8 px borders: wooden frame with gold rivets
                     around a dark interior, so the tinted cells stand out.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from pixel import Canvas, color, save_sheet, preview, OUT_DIR, PREVIEW_DIR

BOARD_DIR = os.path.join(OUT_DIR, "Board", "Original")
SIZE = 48
BORDER = 8  # keep in sync with Phase4Setup.ApplyBoardFrame

BAYER4 = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def frame() -> Canvas:
    cv = Canvas(SIZE, SIZE)
    last = SIZE - 1
    for y in range(SIZE):
        for x in range(SIZE):
            d = min(x, y, last - x, last - y)  # distance to the nearest edge
            if d == 0:
                c = color("outline")
            elif d == 1:
                # wood lit from the top-left, shaded on the bottom-right
                c = color("wood", 4 if (x == 1 or y == 1) else 1)
            elif d in (2, 3, 4):
                grain = 3 if (x + 2 * y) % 7 else 2
                c = color("wood", grain if (x <= 4 or y <= 4) else grain - 1)
            elif d == 5:
                c = color("wood", 0)
            elif d == 6:
                c = color("outline")
            else:
                # dark interior with sparse specks so it is not a flat fill (tiled, not stretched)
                c = color("sky", 1 if (x * 7 + y * 13) % 29 == 0 else 0)
            cv.set(x, y, c)

    for cx, cy in ((2, 2), (last - 3, 2), (2, last - 3), (last - 3, last - 3)):  # rivets
        cv.rect(cx, cy, cx + 1, cy + 1, color("gold", 2))
        cv.set(cx, cy, color("gold", 3))
        cv.set(cx + 1, cy + 1, color("gold", 0))

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
