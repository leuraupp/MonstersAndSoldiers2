using System;
using System.Collections.Generic;

namespace MonstersAndSoldiers.Core {
    /// <summary>
    /// Posição no tabuleiro. Row cresce para cima, Col cresce para a direita.
    /// </summary>
    public readonly struct GridPos : IEquatable<GridPos> {
        public readonly int Row;
        public readonly int Col;

        public GridPos(int row, int col) {
            Row = row;
            Col = col;
        }

        private static readonly GridPos[] Offsets8 = {
            new GridPos(-1, -1), new GridPos(-1, 0), new GridPos(-1, 1),
            new GridPos(0, -1),                      new GridPos(0, 1),
            new GridPos(1, -1),  new GridPos(1, 0),  new GridPos(1, 1),
        };

        /// <summary>As 8 casas ao redor (inclui diagonais), sem checar limites.</summary>
        public IEnumerable<GridPos> Neighbors8() {
            foreach (GridPos o in Offsets8) {
                yield return this + o;
            }
        }

        /// <summary>True se as casas se tocam em uma das 8 direções.</summary>
        public bool IsNeighbor8(GridPos other) {
            int dr = Math.Abs(Row - other.Row);
            int dc = Math.Abs(Col - other.Col);
            return (dr | dc) != 0 && dr <= 1 && dc <= 1;
        }

        public static GridPos operator +(GridPos a, GridPos b) => new GridPos(a.Row + b.Row, a.Col + b.Col);
        public static GridPos operator -(GridPos a, GridPos b) => new GridPos(a.Row - b.Row, a.Col - b.Col);
        public static bool operator ==(GridPos a, GridPos b) => a.Equals(b);
        public static bool operator !=(GridPos a, GridPos b) => !a.Equals(b);

        public bool Equals(GridPos other) => Row == other.Row && Col == other.Col;
        public override bool Equals(object obj) => obj is GridPos other && Equals(other);
        public override int GetHashCode() => (Row * 397) ^ Col;
        public override string ToString() => $"({Row}, {Col})";
    }
}
