using System.Collections.Generic;
using MonstersAndSoldiers.Core;
using NUnit.Framework;

namespace MonstersAndSoldiers.Tests {
    public class PuzzleSolverTests {
        private static GridPos P(int row, int col) => new GridPos(row, col);

        [Test]
        public void GddExample_HasUniqueSolution() {
            // Exemplo 3.2 do GDD v2 (o mesmo do RuleValidatorTests).
            var monsters = new Dictionary<GridPos, MonsterType> {
                { P(1, 3), MonsterType.Orc },
                { P(1, 0), MonsterType.Goblin },
                { P(0, 4), MonsterType.Slime },
            };
            var board = new BoardModel(4, 6, monsters, new[] { 3, 0, 3, 0 }, new[] { 2, 0, 2, 0, 1, 1 });

            SolverResult result = PuzzleSolver.Solve(board);

            Assert.IsTrue(result.IsUnique);
            CollectionAssert.AreEquivalent(
                new[] { P(2, 4), P(0, 0), P(2, 0), P(0, 2), P(0, 5), P(2, 2) },
                result.FirstSolution);
        }

        [Test]
        public void SymmetricBoard_HasTwoSolutions() {
            // Dois slimes espelhados: (0,0)+(2,2) e (0,2)+(2,0) dão as mesmas pistas.
            var monsters = new Dictionary<GridPos, MonsterType> {
                { P(1, 0), MonsterType.Slime },
                { P(1, 2), MonsterType.Slime },
            };
            var board = new BoardModel(3, 3, monsters, new[] { 1, 0, 1 }, new[] { 1, 0, 1 });

            Assert.AreEqual(2, PuzzleSolver.CountSolutions(board, 2));
            Assert.AreEqual(2, PuzzleSolver.CountSolutions(board, 10));
            Assert.AreEqual(1, PuzzleSolver.CountSolutions(board, 1), "O limite interrompe a busca.");
            Assert.IsFalse(PuzzleSolver.Solve(board).IsUnique);
        }

        [Test]
        public void Goblin_WithOnlyTouchingNeighbors_HasNoSolution() {
            // No canto de um 2x2, as 3 casas vizinhas do Goblin se encostam: não cabem 2 soldados.
            var board = new BoardModel(2, 2, new Dictionary<GridPos, MonsterType> { { P(0, 0), MonsterType.Goblin } },
                                       new[] { 1, 1 }, new[] { 1, 1 });

            SolverResult result = PuzzleSolver.Solve(board);

            Assert.AreEqual(0, result.SolutionCount);
            Assert.IsEmpty(result.FirstSolution);
        }

        [Test]
        public void CluesNotMatchingMonsterStrength_HaveNoSolution() {
            var board = new BoardModel(3, 3, new Dictionary<GridPos, MonsterType> { { P(1, 1), MonsterType.Slime } },
                                       new[] { 1, 0, 1 }, new[] { 1, 0, 1 });

            Assert.AreEqual(0, PuzzleSolver.CountSolutions(board));
        }

        [Test]
        public void SharedNeighbors_RespectOneSoldierPerMonster() {
            // Orc (3) e Slime (1) dividem vizinhos: a solução precisa de 4 soldados, um para cada vaga.
            var monsters = new Dictionary<GridPos, MonsterType> {
                { P(1, 1), MonsterType.Orc },
                { P(1, 3), MonsterType.Slime },
            };
            var board = new BoardModel(3, 5, monsters, new[] { 2, 0, 2 }, new[] { 1, 0, 2, 0, 1 });

            SolverResult result = PuzzleSolver.Solve(board, 10);

            Assert.Greater(result.SolutionCount, 0);
            BoardModel check = new BoardModel(3, 5, monsters, new[] { 2, 0, 2 }, new[] { 1, 0, 2, 0, 1 });
            foreach (GridPos s in result.FirstSolution) check.SetState(s, CellState.Soldier);
            Assert.IsTrue(RuleValidator.Validate(check).IsValid);
        }

        [Test]
        public void BoardWithoutClues_IsRejected() {
            var board = new BoardModel(2, 2, new Dictionary<GridPos, MonsterType> { { P(0, 0), MonsterType.Slime } });
            Assert.Throws<System.ArgumentException>(() => PuzzleSolver.Solve(board));
        }
    }
}
