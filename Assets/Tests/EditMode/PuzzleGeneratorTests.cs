using System.Collections.Generic;
using System.Linq;
using MonstersAndSoldiers.Core;
using NUnit.Framework;

namespace MonstersAndSoldiers.Tests {
    public class PuzzleGeneratorTests {
        private const int SeedsPerDifficulty = 40;

        private static readonly Difficulty[] AllDifficulties = { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard };

        [Test]
        public void SameSeed_GeneratesSameMap([ValueSource(nameof(AllDifficulties))] Difficulty difficulty) {
            for (int seed = 1; seed <= 10; seed++) {
                GeneratedPuzzle a = PuzzleGenerator.Generate(difficulty, seed);
                GeneratedPuzzle b = PuzzleGenerator.Generate(difficulty, seed);

                CollectionAssert.AreEqual(Sorted(a.Monsters), Sorted(b.Monsters));
                CollectionAssert.AreEqual(a.RowClues, b.RowClues);
                CollectionAssert.AreEqual(a.ColClues, b.ColClues);
                CollectionAssert.AreEqual(a.Solution, b.Solution);
                Assert.AreEqual(seed, a.Seed);
            }
        }

        [Test]
        public void DifferentSeeds_GenerateDifferentMaps() {
            var layouts = new HashSet<string>();
            for (int seed = 1; seed <= 20; seed++) {
                GeneratedPuzzle p = PuzzleGenerator.Generate(Difficulty.Hard, seed);
                layouts.Add(string.Join(";", Sorted(p.Monsters).Select(m => $"{m.Key}{m.Value}")));
            }
            Assert.Greater(layouts.Count, 15);
        }

        [Test]
        public void Presets_MatchGdd() {
            AssertPreset(Difficulty.Easy, 4, 6, MonsterType.Slime);
            AssertPreset(Difficulty.Medium, 6, 8, MonsterType.Slime, MonsterType.Goblin);
            AssertPreset(Difficulty.Hard, 8, 8, MonsterType.Slime, MonsterType.Goblin, MonsterType.Orc);
        }

        [Test]
        public void Clues_MatchSolution_AndSumToMonsterStrength([ValueSource(nameof(AllDifficulties))] Difficulty difficulty) {
            for (int seed = 1; seed <= SeedsPerDifficulty; seed++) {
                GeneratedPuzzle p = PuzzleGenerator.Generate(difficulty, seed);

                for (int r = 0; r < p.Rows; r++) {
                    Assert.AreEqual(p.Solution.Count(s => s.Row == r), p.RowClues[r], $"seed {seed}, linha {r}");
                }
                for (int c = 0; c < p.Cols; c++) {
                    Assert.AreEqual(p.Solution.Count(s => s.Col == c), p.ColClues[c], $"seed {seed}, coluna {c}");
                }

                int strength = p.Monsters.Values.Sum(MonsterRules.RequiredSoldiers);
                Assert.AreEqual(strength, p.RowClues.Sum(), $"seed {seed}");
                Assert.AreEqual(strength, p.ColClues.Sum(), $"seed {seed}");
                Assert.AreEqual(strength, p.Solution.Count, $"seed {seed}");
            }
        }

        [Test]
        public void Solution_PassesRuleValidator([ValueSource(nameof(AllDifficulties))] Difficulty difficulty) {
            for (int seed = 1; seed <= SeedsPerDifficulty; seed++) {
                GeneratedPuzzle p = PuzzleGenerator.Generate(difficulty, seed);
                BoardModel board = p.CreateBoard();
                foreach (GridPos s in p.Solution) Assert.IsTrue(board.SetState(s, CellState.Soldier), $"seed {seed}: {s}");

                Assert.IsTrue(RuleValidator.Validate(board).IsValid, $"seed {seed}");
            }
        }

        [Test]
        public void SoldierFill_IsAboutTwentyPercent([ValueSource(nameof(AllDifficulties))] Difficulty difficulty) {
            PuzzleSettings settings = PuzzleSettings.For(difficulty);
            for (int seed = 1; seed <= SeedsPerDifficulty; seed++) {
                GeneratedPuzzle p = PuzzleGenerator.Generate(difficulty, seed);
                float fill = p.Solution.Count / (float)(p.Rows * p.Cols);
                Assert.That(fill, Is.InRange(0.1f, 0.3f), $"seed {seed}: {p.Solution.Count} soldados");
                Assert.LessOrEqual(p.Solution.Count, settings.TargetSoldiers + 2, $"seed {seed}");
            }
        }

        [Test]
        public void Solution_IsUnique([ValueSource(nameof(AllDifficulties))] Difficulty difficulty) {
            for (int seed = 1; seed <= SeedsPerDifficulty; seed++) {
                GeneratedPuzzle p = PuzzleGenerator.Generate(difficulty, seed);
                Assert.IsTrue(p.IsUnique, $"seed {seed}");
                Assert.LessOrEqual(p.Attempts, PuzzleSettings.For(difficulty).MaxAttempts);

                SolverResult result = PuzzleSolver.Solve(p.CreateBoard(), 2);
                Assert.AreEqual(1, result.SolutionCount, $"seed {seed}");
                CollectionAssert.AreEquivalent(p.Solution, result.FirstSolution, $"seed {seed}");
            }
        }

        [Test]
        public void CreateBoard_ReturnsFreshEmptyBoard() {
            GeneratedPuzzle p = PuzzleGenerator.Generate(Difficulty.Medium, 7);
            BoardModel first = p.CreateBoard();
            first.SetState(p.Solution[0], CellState.Soldier);

            BoardModel second = p.CreateBoard();
            Assert.AreNotSame(first, second);
            Assert.AreEqual(0, second.SoldierCount);
            Assert.IsTrue(second.HasClues);
            Assert.AreEqual(p.Monsters.Count, second.Monsters.Count);
        }

        private static void AssertPreset(Difficulty difficulty, int rows, int cols, params MonsterType[] types) {
            PuzzleSettings settings = PuzzleSettings.For(difficulty);
            Assert.AreEqual(rows, settings.Rows, difficulty.ToString());
            Assert.AreEqual(cols, settings.Cols, difficulty.ToString());
            CollectionAssert.AreEquivalent(types, settings.MonsterTypes, difficulty.ToString());

            // Os mapas só usam os tipos da dificuldade.
            var seen = new HashSet<MonsterType>();
            for (int seed = 1; seed <= SeedsPerDifficulty; seed++) {
                GeneratedPuzzle p = PuzzleGenerator.Generate(difficulty, seed);
                Assert.AreEqual(rows, p.Rows);
                Assert.AreEqual(cols, p.Cols);
                seen.UnionWith(p.Monsters.Values);
            }
            CollectionAssert.AreEquivalent(types, seen, difficulty.ToString());
        }

        private static List<KeyValuePair<GridPos, MonsterType>> Sorted(IReadOnlyDictionary<GridPos, MonsterType> monsters) =>
            monsters.OrderBy(m => m.Key.Row).ThenBy(m => m.Key.Col).ToList();
    }
}
