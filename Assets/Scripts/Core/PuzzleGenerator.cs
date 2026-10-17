using System;
using System.Collections.Generic;

namespace MonstersAndSoldiers.Core {
    /// <summary>Mapa gerado: monstros, pistas e a solução usada para criá-lo.</summary>
    public class GeneratedPuzzle {
        public int Seed { get; }
        public int Rows { get; }
        public int Cols { get; }
        public IReadOnlyDictionary<GridPos, MonsterType> Monsters { get; }
        public IReadOnlyList<int> RowClues { get; }
        public IReadOnlyList<int> ColClues { get; }

        /// <summary>Soldados da solução (o jogador não vê).</summary>
        public IReadOnlyList<GridPos> Solution { get; }

        /// <summary>Quantas tentativas o gerador usou até este mapa.</summary>
        public int Attempts { get; }

        /// <summary>False só se nenhuma das tentativas teve solução única (o mapa ainda é resolvível).</summary>
        public bool IsUnique { get; }

        public GeneratedPuzzle(int seed, int rows, int cols, IReadOnlyDictionary<GridPos, MonsterType> monsters,
                               IReadOnlyList<int> rowClues, IReadOnlyList<int> colClues,
                               IReadOnlyList<GridPos> solution, int attempts, bool isUnique) {
            Seed = seed;
            Rows = rows;
            Cols = cols;
            Monsters = monsters;
            RowClues = rowClues;
            ColClues = colClues;
            Solution = solution;
            Attempts = attempts;
            IsUnique = isUnique;
        }

        /// <summary>Tabuleiro vazio deste mapa. Cada chamada cria um novo (Reiniciar = mesmo mapa).</summary>
        public BoardModel CreateBoard() {
            var monsters = new Dictionary<GridPos, MonsterType>();
            foreach (KeyValuePair<GridPos, MonsterType> m in Monsters) monsters[m.Key] = m.Value;
            return new BoardModel(Rows, Cols, monsters, RowClues, ColClues);
        }
    }

    /// <summary>
    /// Gera mapas com solução única a partir de uma seed (GDD 7). A mesma seed sempre gera o mesmo mapa.
    /// </summary>
    public static class PuzzleGenerator {
        /// <summary>Quantas vezes tenta reescolher os soldados de um monstro antes de desistir da casa.</summary>
        private const int PickRetries = 8;

        public static GeneratedPuzzle Generate(Difficulty difficulty, int seed) {
            return Generate(PuzzleSettings.For(difficulty), seed);
        }

        public static GeneratedPuzzle Generate(PuzzleSettings settings, int seed) {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var rng = new Random(seed);
            GeneratedPuzzle fallback = null;

            for (int attempt = 1; attempt <= settings.MaxAttempts; attempt++) {
                if (!TryPlace(settings, rng, out Dictionary<GridPos, MonsterType> monsters, out List<GridPos> soldiers)) continue;

                int[] rowClues = new int[settings.Rows];
                int[] colClues = new int[settings.Cols];
                foreach (GridPos s in soldiers) {
                    rowClues[s.Row]++;
                    colClues[s.Col]++;
                }

                bool unique = PuzzleSolver.Solve(settings.Rows, settings.Cols, monsters, rowClues, colClues, 2).IsUnique;
                var puzzle = new GeneratedPuzzle(seed, settings.Rows, settings.Cols, monsters, rowClues, colClues,
                                                 soldiers, attempt, unique);
                if (unique) return puzzle;
                fallback = puzzle;
            }

            return fallback ?? throw new InvalidOperationException(
                $"Não foi possível gerar um mapa {settings.Cols}x{settings.Rows} (seed {seed}).");
        }

        /// <summary>
        /// Passo 1 da GDD 7: sorteia monstro e casa livre, e escolhe ao redor dele N casas que não encostam
        /// em nenhum soldado já posto nem entre si. Para quando chega na quantidade alvo de soldados.
        /// </summary>
        private static bool TryPlace(PuzzleSettings settings, Random rng,
                                     out Dictionary<GridPos, MonsterType> monsters, out List<GridPos> soldiers) {
            int rows = settings.Rows;
            int cols = settings.Cols;
            monsters = new Dictionary<GridPos, MonsterType>();
            soldiers = new List<GridPos>();

            var occupied = new bool[rows, cols];
            var soldierAt = new bool[rows, cols];
            int target = settings.TargetSoldiers;
            int maxFailures = rows * cols * 4;
            int failures = 0;

            var free = new List<GridPos>();
            var around = new List<GridPos>();
            var picked = new List<GridPos>();

            while (soldiers.Count < target && failures < maxFailures) {
                MonsterType type = PickType(settings.MonsterTypes, target - soldiers.Count, rng);
                int need = MonsterRules.RequiredSoldiers(type);

                free.Clear();
                for (int r = 0; r < rows; r++) {
                    for (int c = 0; c < cols; c++) {
                        if (!occupied[r, c]) free.Add(new GridPos(r, c));
                    }
                }
                if (free.Count == 0) break;
                GridPos monster = free[rng.Next(free.Count)];

                around.Clear();
                foreach (GridPos n in monster.Neighbors8()) {
                    if (n.Row < 0 || n.Row >= rows || n.Col < 0 || n.Col >= cols) continue;
                    if (occupied[n.Row, n.Col] || TouchesSoldier(n, soldierAt, rows, cols)) continue;
                    around.Add(n);
                }

                if (around.Count < need || !TryPickApart(around, need, rng, picked)) {
                    failures++;
                    continue;
                }

                monsters[monster] = type;
                occupied[monster.Row, monster.Col] = true;
                foreach (GridPos s in picked) {
                    soldiers.Add(s);
                    occupied[s.Row, s.Col] = true;
                    soldierAt[s.Row, s.Col] = true;
                }
            }

            return soldiers.Count > 0;
        }

        /// <summary>Sorteia um tipo; evita passar do alvo quando existe um tipo que ainda cabe.</summary>
        private static MonsterType PickType(IReadOnlyList<MonsterType> types, int remaining, Random rng) {
            MonsterType type = types[rng.Next(types.Count)];
            if (MonsterRules.RequiredSoldiers(type) <= remaining) return type;

            MonsterType smallest = type;
            foreach (MonsterType t in types) {
                if (MonsterRules.RequiredSoldiers(t) < MonsterRules.RequiredSoldiers(smallest)) smallest = t;
            }
            return smallest;
        }

        /// <summary>Escolhe <paramref name="need"/> casas de <paramref name="pool"/> que não se encostam.</summary>
        private static bool TryPickApart(List<GridPos> pool, int need, Random rng, List<GridPos> picked) {
            for (int retry = 0; retry < PickRetries; retry++) {
                Shuffle(pool, rng);
                picked.Clear();
                foreach (GridPos p in pool) {
                    bool touches = false;
                    foreach (GridPos q in picked) {
                        if (p.IsNeighbor8(q)) {
                            touches = true;
                            break;
                        }
                    }
                    if (touches) continue;
                    picked.Add(p);
                    if (picked.Count == need) return true;
                }
            }
            return false;
        }

        private static bool TouchesSoldier(GridPos p, bool[,] soldierAt, int rows, int cols) {
            foreach (GridPos n in p.Neighbors8()) {
                if (n.Row >= 0 && n.Row < rows && n.Col >= 0 && n.Col < cols && soldierAt[n.Row, n.Col]) return true;
            }
            return false;
        }

        private static void Shuffle<T>(List<T> list, Random rng) {
            for (int i = list.Count - 1; i > 0; i--) {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
