using System;
using System.Collections.Generic;

namespace MonstersAndSoldiers.Core {
    /// <summary>
    /// Estado do tabuleiro, sem nenhuma dependência da Unity.
    /// A visualização apenas observa <see cref="CellChanged"/> e chama os métodos de ação.
    /// </summary>
    public class BoardModel {
        public int Rows { get; }
        public int Cols { get; }

        /// <summary>Pistas por linha (índice = Row). Null quando o nível não usa pistas.</summary>
        public IReadOnlyList<int> RowClues { get; }

        /// <summary>Pistas por coluna (índice = Col). Null quando o nível não usa pistas.</summary>
        public IReadOnlyList<int> ColClues { get; }

        public IReadOnlyDictionary<GridPos, MonsterType> Monsters => monsters;

        /// <summary>Total de soldados da solução = soma das forças dos monstros.</summary>
        public int RequiredSoldierTotal { get; }

        public int SoldierCount { get; private set; }

        /// <summary>Disparado sempre que o estado de uma casa muda.</summary>
        public event Action<GridPos, CellState> CellChanged;

        private readonly CellState[,] states;
        private readonly Dictionary<GridPos, MonsterType> monsters;

        public BoardModel(int rows, int cols, IDictionary<GridPos, MonsterType> monsters,
                          IReadOnlyList<int> rowClues = null, IReadOnlyList<int> colClues = null) {
            if (rows <= 0 || cols <= 0) throw new ArgumentException("O tabuleiro precisa ter ao menos 1x1.");
            if (rowClues != null && rowClues.Count != rows) throw new ArgumentException("rowClues precisa ter uma pista por linha.");
            if (colClues != null && colClues.Count != cols) throw new ArgumentException("colClues precisa ter uma pista por coluna.");

            Rows = rows;
            Cols = cols;
            RowClues = rowClues;
            ColClues = colClues;
            states = new CellState[rows, cols];
            this.monsters = new Dictionary<GridPos, MonsterType>();

            foreach (KeyValuePair<GridPos, MonsterType> m in monsters) {
                if (!IsInside(m.Key)) throw new ArgumentException($"Monstro fora do tabuleiro em {m.Key}.");
                this.monsters[m.Key] = m.Value;
                RequiredSoldierTotal += MonsterRules.RequiredSoldiers(m.Value);
            }
        }

        public bool HasClues => RowClues != null && ColClues != null;

        public bool IsInside(GridPos p) => p.Row >= 0 && p.Row < Rows && p.Col >= 0 && p.Col < Cols;

        public bool IsMonster(GridPos p) => monsters.ContainsKey(p);

        public bool TryGetMonster(GridPos p, out MonsterType type) => monsters.TryGetValue(p, out type);

        public CellState GetState(GridPos p) => states[p.Row, p.Col];

        public IEnumerable<GridPos> Soldiers {
            get {
                for (int r = 0; r < Rows; r++) {
                    for (int c = 0; c < Cols; c++) {
                        if (states[r, c] == CellState.Soldier) yield return new GridPos(r, c);
                    }
                }
            }
        }

        public IEnumerable<GridPos> InsideNeighbors8(GridPos p) {
            foreach (GridPos n in p.Neighbors8()) {
                if (IsInside(n)) yield return n;
            }
        }

        /// <summary>Define o estado de uma casa. Retorna false se a casa é um monstro ou está fora.</summary>
        public bool SetState(GridPos p, CellState state) {
            if (!IsInside(p) || IsMonster(p)) return false;

            CellState old = states[p.Row, p.Col];
            if (old == state) return true;

            if (old == CellState.Soldier) SoldierCount--;
            if (state == CellState.Soldier) SoldierCount++;

            states[p.Row, p.Col] = state;
            CellChanged?.Invoke(p, state);
            return true;
        }

        /// <summary>
        /// Estado resultante da ação primária (clique esquerdo / A): alterna Soldado; em Grama vira Soldado.
        /// </summary>
        public CellState NextStateForPrimary(GridPos p) =>
            GetState(p) == CellState.Soldier ? CellState.Empty : CellState.Soldier;

        /// <summary>
        /// Estado resultante da ação secundária (clique direito / B): alterna Grama; em Soldado vira Grama.
        /// </summary>
        public CellState NextStateForSecondary(GridPos p) =>
            GetState(p) == CellState.Grass ? CellState.Empty : CellState.Grass;

        /// <summary>Volta todas as casas para Vazia (reiniciar o mesmo mapa).</summary>
        public void Clear() {
            for (int r = 0; r < Rows; r++) {
                for (int c = 0; c < Cols; c++) {
                    SetState(new GridPos(r, c), CellState.Empty);
                }
            }
        }

        public int CountSoldiersInRow(int row) {
            int count = 0;
            for (int c = 0; c < Cols; c++) {
                if (states[row, c] == CellState.Soldier) count++;
            }
            return count;
        }

        public int CountSoldiersInCol(int col) {
            int count = 0;
            for (int r = 0; r < Rows; r++) {
                if (states[r, col] == CellState.Soldier) count++;
            }
            return count;
        }
    }
}
