using System.Collections.Generic;

namespace MonstersAndSoldiers.Core {
    /// <summary>Resultado da validação, com os erros separados por regra do GDD.</summary>
    public class ValidationResult {
        /// <summary>Regra 1: monstros que não receberam todos os soldados de que precisam.</summary>
        public readonly HashSet<GridPos> UnsatisfiedMonsters = new HashSet<GridPos>();

        /// <summary>Regras 2 e 5: soldados que não ficaram pareados com nenhum monstro.</summary>
        public readonly HashSet<GridPos> UnpairedSoldiers = new HashSet<GridPos>();

        /// <summary>Regra 3: soldados encostados em outro soldado.</summary>
        public readonly HashSet<GridPos> TouchingSoldiers = new HashSet<GridPos>();

        /// <summary>Regra 4: linhas e colunas cuja contagem não bate com a pista.</summary>
        public readonly HashSet<int> WrongRows = new HashSet<int>();
        public readonly HashSet<int> WrongCols = new HashSet<int>();

        /// <summary>Pareamento encontrado: soldado → monstro.</summary>
        public readonly Dictionary<GridPos, GridPos> SoldierToMonster = new Dictionary<GridPos, GridPos>();

        public bool IsValid =>
            UnsatisfiedMonsters.Count == 0 &&
            UnpairedSoldiers.Count == 0 &&
            TouchingSoldiers.Count == 0 &&
            WrongRows.Count == 0 &&
            WrongCols.Count == 0;
    }

    /// <summary>Verifica as 5 regras do puzzle (GDD v2, seção 3).</summary>
    public static class RuleValidator {
        public static ValidationResult Validate(BoardModel board) {
            var result = new ValidationResult();
            var soldiers = new List<GridPos>(board.Soldiers);

            // Regra 3: soldados não se encostam (8 direções).
            var soldierSet = new HashSet<GridPos>(soldiers);
            foreach (GridPos s in soldiers) {
                foreach (GridPos n in s.Neighbors8()) {
                    if (soldierSet.Contains(n)) {
                        result.TouchingSoldiers.Add(s);
                        result.TouchingSoldiers.Add(n);
                    }
                }
            }

            // Regra 4: pistas de linha e coluna (só quando o nível tem pistas).
            if (board.HasClues) {
                for (int r = 0; r < board.Rows; r++) {
                    if (board.CountSoldiersInRow(r) != board.RowClues[r]) result.WrongRows.Add(r);
                }
                for (int c = 0; c < board.Cols; c++) {
                    if (board.CountSoldiersInCol(c) != board.ColClues[c]) result.WrongCols.Add(c);
                }
            }

            // Regras 1, 2 e 5: pareamento soldado-monstro.
            Pair(board, soldiers, result);
            return result;
        }

        /// <summary>
        /// Emparelhamento bipartido máximo (caminhos aumentantes). Cada monstro vira N "vagas",
        /// uma por soldado exigido; cada soldado ocupa no máximo uma vaga de um monstro vizinho.
        /// </summary>
        private static void Pair(BoardModel board, List<GridPos> soldiers, ValidationResult result) {
            var slots = new List<GridPos>();
            foreach (KeyValuePair<GridPos, MonsterType> m in board.Monsters) {
                int required = MonsterRules.RequiredSoldiers(m.Value);
                for (int i = 0; i < required; i++) slots.Add(m.Key);
            }

            var adjacency = new List<int>[soldiers.Count];
            for (int s = 0; s < soldiers.Count; s++) {
                adjacency[s] = new List<int>();
                for (int i = 0; i < slots.Count; i++) {
                    if (soldiers[s].IsNeighbor8(slots[i])) adjacency[s].Add(i);
                }
            }

            int[] slotOwner = new int[slots.Count];
            for (int i = 0; i < slotOwner.Length; i++) slotOwner[i] = -1;

            for (int s = 0; s < soldiers.Count; s++) {
                TryAugment(s, adjacency, slotOwner, new bool[slots.Count]);
            }

            var paired = new HashSet<int>();
            for (int i = 0; i < slots.Count; i++) {
                if (slotOwner[i] < 0) {
                    result.UnsatisfiedMonsters.Add(slots[i]);
                } else {
                    paired.Add(slotOwner[i]);
                    result.SoldierToMonster[soldiers[slotOwner[i]]] = slots[i];
                }
            }

            for (int s = 0; s < soldiers.Count; s++) {
                if (!paired.Contains(s)) result.UnpairedSoldiers.Add(soldiers[s]);
            }
        }

        private static bool TryAugment(int soldier, List<int>[] adjacency, int[] slotOwner, bool[] visited) {
            foreach (int slot in adjacency[soldier]) {
                if (visited[slot]) continue;
                visited[slot] = true;
                if (slotOwner[slot] < 0 || TryAugment(slotOwner[slot], adjacency, slotOwner, visited)) {
                    slotOwner[slot] = soldier;
                    return true;
                }
            }
            return false;
        }
    }
}
