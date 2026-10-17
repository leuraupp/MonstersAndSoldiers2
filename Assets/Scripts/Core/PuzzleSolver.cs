using System;
using System.Collections.Generic;

namespace MonstersAndSoldiers.Core {
    /// <summary>Resultado do solver: quantas soluções achou (até o limite pedido) e a primeira delas.</summary>
    public class SolverResult {
        public int SolutionCount { get; }

        /// <summary>Soldados da primeira solução encontrada. Vazio se não há solução.</summary>
        public IReadOnlyList<GridPos> FirstSolution { get; }

        public bool IsUnique => SolutionCount == 1;

        public SolverResult(int solutionCount, IReadOnlyList<GridPos> firstSolution) {
            SolutionCount = solutionCount;
            FirstSolution = firstSolution ?? Array.Empty<GridPos>();
        }
    }

    /// <summary>
    /// Conta as soluções de um mapa por backtracking (GDD 7), casa a casa, linha a linha.
    /// Poda por pistas de linha/coluna, pela regra dos soldados não se encostarem e, ao fim de cada
    /// linha, pelo pareamento soldado-monstro (o mesmo emparelhamento bipartido do <see cref="RuleValidator"/>).
    /// </summary>
    public static class PuzzleSolver {
        public static SolverResult Solve(BoardModel board, int limit = 2) {
            if (board == null) throw new ArgumentNullException(nameof(board));
            return Solve(board.Rows, board.Cols, board.Monsters, board.RowClues, board.ColClues, limit);
        }

        public static SolverResult Solve(int rows, int cols, IReadOnlyDictionary<GridPos, MonsterType> monsters,
                                         IReadOnlyList<int> rowClues, IReadOnlyList<int> colClues, int limit = 2) {
            if (rowClues == null || colClues == null) throw new ArgumentException("O solver precisa das pistas de linha e coluna.");
            if (rowClues.Count != rows || colClues.Count != cols) throw new ArgumentException("Número de pistas não bate com o tabuleiro.");
            if (limit < 1) throw new ArgumentOutOfRangeException(nameof(limit));

            var search = new Search(rows, cols, monsters, rowClues, colClues, limit);
            search.Run();
            return new SolverResult(search.Count, search.First);
        }

        public static int CountSolutions(BoardModel board, int limit = 2) => Solve(board, limit).SolutionCount;

        private class Search {
            private readonly int rows;
            private readonly int cols;
            private readonly IReadOnlyList<int> rowClues;
            private readonly IReadOnlyList<int> colClues;
            private readonly int limit;

            private readonly bool[,] candidate;   // casa vazia vizinha de ao menos um monstro
            private readonly int[,][] cellSlotArray; // [r, c] -> vagas de monstros vizinhos
            private readonly int[,] rowCandFrom;  // candidatas na linha r a partir da coluna c
            private readonly int[,] colCandFrom;  // candidatas na coluna c a partir da linha r

            private readonly List<GridPos> slots = new List<GridPos>();
            private readonly List<int>[] slotsClosedAfterRow; // vagas cujos vizinhos estão todos decididos ao fim da linha

            private readonly bool[,] soldier;
            private readonly int[] rowCount;
            private readonly int[] colCount;
            private readonly List<GridPos> placed = new List<GridPos>();
            private readonly List<int> closedSlots = new List<int>();

            private readonly bool impossible;

            public int Count { get; private set; }
            public List<GridPos> First { get; private set; }

            public Search(int rows, int cols, IReadOnlyDictionary<GridPos, MonsterType> monsters,
                          IReadOnlyList<int> rowClues, IReadOnlyList<int> colClues, int limit) {
                this.rows = rows;
                this.cols = cols;
                this.rowClues = rowClues;
                this.colClues = colClues;
                this.limit = limit;

                soldier = new bool[rows, cols];
                rowCount = new int[rows];
                colCount = new int[cols];
                candidate = new bool[rows, cols];
                slotsClosedAfterRow = new List<int>[rows];
                for (int r = 0; r < rows; r++) slotsClosedAfterRow[r] = new List<int>();

                // Ordem estável das vagas (o dicionário pode vir em qualquer ordem).
                var monsterList = new List<KeyValuePair<GridPos, MonsterType>>(monsters);
                monsterList.Sort((a, b) => a.Key.Row != b.Key.Row ? a.Key.Row.CompareTo(b.Key.Row) : a.Key.Col.CompareTo(b.Key.Col));

                var isMonster = new bool[rows, cols];
                foreach (KeyValuePair<GridPos, MonsterType> m in monsterList) {
                    if (m.Key.Row < 0 || m.Key.Row >= rows || m.Key.Col < 0 || m.Key.Col >= cols) {
                        throw new ArgumentException($"Monstro fora do tabuleiro em {m.Key}.");
                    }
                    isMonster[m.Key.Row, m.Key.Col] = true;
                    int closeRow = Math.Min(m.Key.Row + 1, rows - 1);
                    for (int i = 0; i < MonsterRules.RequiredSoldiers(m.Value); i++) {
                        slotsClosedAfterRow[closeRow].Add(slots.Count);
                        slots.Add(m.Key);
                    }
                }

                var slotLists = new List<int>[rows, cols];
                for (int s = 0; s < slots.Count; s++) {
                    foreach (GridPos n in slots[s].Neighbors8()) {
                        if (n.Row < 0 || n.Row >= rows || n.Col < 0 || n.Col >= cols || isMonster[n.Row, n.Col]) continue;
                        candidate[n.Row, n.Col] = true;
                        (slotLists[n.Row, n.Col] ??= new List<int>()).Add(s);
                    }
                }

                cellSlotArray = new int[rows, cols][];
                for (int r = 0; r < rows; r++) {
                    for (int c = 0; c < cols; c++) {
                        cellSlotArray[r, c] = slotLists[r, c] != null ? slotLists[r, c].ToArray() : Array.Empty<int>();
                    }
                }

                rowCandFrom = new int[rows, cols + 2];
                colCandFrom = new int[rows + 2, cols];
                for (int r = 0; r < rows; r++) {
                    for (int c = cols - 1; c >= 0; c--) rowCandFrom[r, c] = rowCandFrom[r, c + 1] + (candidate[r, c] ? 1 : 0);
                }
                for (int c = 0; c < cols; c++) {
                    for (int r = rows - 1; r >= 0; r--) colCandFrom[r, c] = colCandFrom[r + 1, c] + (candidate[r, c] ? 1 : 0);
                }

                // Total de soldados = soma das forças = soma das pistas (GDD 3).
                int rowSum = 0, colSum = 0;
                for (int r = 0; r < rows; r++) rowSum += rowClues[r];
                for (int c = 0; c < cols; c++) colSum += colClues[c];
                impossible = rowSum != slots.Count || colSum != slots.Count;
            }

            public void Run() {
                if (impossible) return;
                Step(0);
            }

            private void Step(int index) {
                if (Count >= limit) return;
                if (index == rows * cols) {
                    Count++;
                    if (First == null) First = new List<GridPos>(placed);
                    return;
                }

                int r = index / cols;
                int c = index % cols;

                if (CanPlace(r, c)) {
                    soldier[r, c] = true;
                    rowCount[r]++;
                    colCount[c]++;
                    placed.Add(new GridPos(r, c));

                    if (LinesStillReachable(r, c, c + 2, r + 2)) Advance(index, r, c);

                    placed.RemoveAt(placed.Count - 1);
                    colCount[c]--;
                    rowCount[r]--;
                    soldier[r, c] = false;
                }

                if (Count >= limit) return;
                if (LinesStillReachable(r, c, c + 1, r + 1)) Advance(index, r, c);
            }

            private void Advance(int index, int r, int c) {
                if (c < cols - 1) {
                    Step(index + 1);
                    return;
                }

                // Fim da linha: as vagas dos monstros desta linha e da anterior já têm todos os vizinhos decididos.
                int before = closedSlots.Count;
                closedSlots.AddRange(slotsClosedAfterRow[r]);
                if (rowCount[r] == rowClues[r] && PairingStillPossible()) Step(index + 1);
                closedSlots.RemoveRange(before, closedSlots.Count - before);
            }

            private bool CanPlace(int r, int c) {
                if (!candidate[r, c]) return false;
                if (rowCount[r] >= rowClues[r] || colCount[c] >= colClues[c]) return false;
                if (c > 0 && soldier[r, c - 1]) return false;
                if (r > 0) {
                    if (soldier[r - 1, c]) return false;
                    if (c > 0 && soldier[r - 1, c - 1]) return false;
                    if (c < cols - 1 && soldier[r - 1, c + 1]) return false;
                }
                return true;
            }

            /// <summary>
            /// A linha r e a coluna c ainda conseguem chegar na pista? Sem encostar, cabem no máximo
            /// metade (arredondada para cima) das casas restantes, e só entre as candidatas.
            /// </summary>
            private bool LinesStillReachable(int r, int c, int nextCol, int nextRow) {
                int colsLeft = Math.Max(0, cols - nextCol);
                int rowMax = Math.Min((colsLeft + 1) / 2, rowCandFrom[r, Math.Min(nextCol, cols)]);
                if (rowCount[r] + rowMax < rowClues[r]) return false;

                int rowsLeft = Math.Max(0, rows - nextRow);
                int colMax = Math.Min((rowsLeft + 1) / 2, colCandFrom[Math.Min(nextRow, rows), c]);
                return colCount[c] + colMax >= colClues[c];
            }

            /// <summary>
            /// Condição necessária: todo soldado posto cabe em uma vaga, e toda vaga fechada cabe em um soldado posto.
            /// Se as duas valem, existe um emparelhamento que cobre ambas (Mendelsohn-Dulmage).
            /// No fim do tabuleiro todas as vagas estão fechadas e há tantos soldados quanto vagas: emparelhamento perfeito.
            /// </summary>
            private bool PairingStillPossible() {
                // Soldados -> vagas.
                int[] slotOwner = NewFilled(slots.Count);
                for (int s = 0; s < placed.Count; s++) {
                    if (!AugmentSoldier(s, slotOwner, new bool[slots.Count])) return false;
                }

                if (closedSlots.Count == 0) return true;

                // Vagas fechadas -> soldados.
                int[] soldierOwner = NewFilled(placed.Count);
                foreach (int slot in closedSlots) {
                    if (!AugmentSlot(slot, soldierOwner, new bool[placed.Count])) return false;
                }
                return true;
            }

            private bool AugmentSoldier(int s, int[] slotOwner, bool[] visited) {
                GridPos p = placed[s];
                foreach (int slot in cellSlotArray[p.Row, p.Col]) {
                    if (visited[slot]) continue;
                    visited[slot] = true;
                    if (slotOwner[slot] < 0 || AugmentSoldier(slotOwner[slot], slotOwner, visited)) {
                        slotOwner[slot] = s;
                        return true;
                    }
                }
                return false;
            }

            private bool AugmentSlot(int slot, int[] soldierOwner, bool[] visited) {
                for (int s = 0; s < placed.Count; s++) {
                    if (visited[s] || !placed[s].IsNeighbor8(slots[slot])) continue;
                    visited[s] = true;
                    if (soldierOwner[s] < 0 || AugmentSlot(soldierOwner[s], soldierOwner, visited)) {
                        soldierOwner[s] = slot;
                        return true;
                    }
                }
                return false;
            }

            private static int[] NewFilled(int length) {
                var array = new int[length];
                for (int i = 0; i < length; i++) array[i] = -1;
                return array;
            }
        }
    }
}
