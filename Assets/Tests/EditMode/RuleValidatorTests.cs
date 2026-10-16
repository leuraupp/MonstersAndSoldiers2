using System.Collections.Generic;
using MonstersAndSoldiers.Core;
using NUnit.Framework;

namespace MonstersAndSoldiers.Tests {
    public class RuleValidatorTests {
        private static GridPos P(int row, int col) => new GridPos(row, col);

        // Exemplo 3.2 do GDD v2 (linha 0 do GDD = Row 0 aqui).
        private static BoardModel GddExample() {
            var monsters = new Dictionary<GridPos, MonsterType> {
                { P(1, 3), MonsterType.Orc },
                { P(1, 0), MonsterType.Goblin },
                { P(0, 4), MonsterType.Slime },
            };
            return new BoardModel(4, 6, monsters, new[] { 3, 0, 3, 0 }, new[] { 2, 0, 2, 0, 1, 1 });
        }

        private static readonly GridPos[] GddSolution = {
            P(2, 4), P(0, 0), P(2, 0), P(0, 2), P(0, 5), P(2, 2)
        };

        [Test]
        public void GddExample_Solution_IsValid() {
            BoardModel board = GddExample();
            foreach (GridPos p in GddSolution) board.SetState(p, CellState.Soldier);

            ValidationResult result = RuleValidator.Validate(board);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(6, result.SoldierToMonster.Count);
            Assert.AreEqual(P(0, 4), result.SoldierToMonster[P(0, 5)]);
        }

        [Test]
        public void EmptyBoard_FlagsEveryMonster() {
            ValidationResult result = RuleValidator.Validate(GddExample());

            Assert.IsFalse(result.IsValid);
            CollectionAssert.AreEquivalent(new[] { P(1, 3), P(1, 0), P(0, 4) }, result.UnsatisfiedMonsters);
        }

        [Test]
        public void TouchingSoldiers_Diagonal_AreInvalid() {
            var board = new BoardModel(3, 3, new Dictionary<GridPos, MonsterType> { { P(1, 1), MonsterType.Goblin } });
            board.SetState(P(0, 0), CellState.Soldier);
            board.SetState(P(1, 0), CellState.Soldier);

            ValidationResult result = RuleValidator.Validate(board);

            CollectionAssert.AreEquivalent(new[] { P(0, 0), P(1, 0) }, result.TouchingSoldiers);
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void SharedSoldier_CountsForOnlyOneMonster() {
            // Dois slimes dividem um único soldado: um deles fica sem par (regra 2).
            var monsters = new Dictionary<GridPos, MonsterType> {
                { P(0, 0), MonsterType.Slime },
                { P(0, 2), MonsterType.Slime },
            };
            var board = new BoardModel(2, 3, monsters);
            board.SetState(P(1, 1), CellState.Soldier);

            ValidationResult result = RuleValidator.Validate(board);

            Assert.AreEqual(1, result.UnsatisfiedMonsters.Count);
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void Orc_WithFourCornersAvailable_NeedsExactlyThree() {
            var board = new BoardModel(3, 3, new Dictionary<GridPos, MonsterType> { { P(1, 1), MonsterType.Orc } });
            board.SetState(P(0, 0), CellState.Soldier);
            board.SetState(P(0, 2), CellState.Soldier);
            board.SetState(P(2, 0), CellState.Soldier);
            Assert.IsTrue(RuleValidator.Validate(board).IsValid);

            board.SetState(P(2, 2), CellState.Soldier);
            ValidationResult extra = RuleValidator.Validate(board);
            Assert.AreEqual(1, extra.UnpairedSoldiers.Count);
            Assert.IsFalse(extra.IsValid);
        }

        [Test]
        public void SoldierFarFromMonsters_IsUnpaired() {
            var board = new BoardModel(1, 4, new Dictionary<GridPos, MonsterType> { { P(0, 0), MonsterType.Slime } });
            board.SetState(P(0, 1), CellState.Soldier);
            board.SetState(P(0, 3), CellState.Soldier);

            ValidationResult result = RuleValidator.Validate(board);

            CollectionAssert.AreEquivalent(new[] { P(0, 3) }, result.UnpairedSoldiers);
        }

        [Test]
        public void WrongClues_AreReportedPerLine() {
            BoardModel board = GddExample();
            foreach (GridPos p in GddSolution) board.SetState(p, CellState.Soldier);
            board.SetState(P(0, 5), CellState.Empty);
            board.SetState(P(1, 5), CellState.Soldier);

            ValidationResult result = RuleValidator.Validate(board);

            CollectionAssert.AreEquivalent(new[] { 0, 1 }, result.WrongRows);
            Assert.IsEmpty(result.WrongCols);
        }
    }

    public class BoardModelTests {
        private static BoardModel Board() =>
            new BoardModel(2, 2, new Dictionary<GridPos, MonsterType> { { new GridPos(0, 0), MonsterType.Slime } });

        [Test]
        public void PrimaryAndSecondary_FollowGddCycle() {
            BoardModel board = Board();
            var p = new GridPos(1, 1);

            Assert.AreEqual(CellState.Soldier, board.NextStateForPrimary(p));
            Assert.AreEqual(CellState.Grass, board.NextStateForSecondary(p));

            board.SetState(p, CellState.Soldier);
            Assert.AreEqual(CellState.Empty, board.NextStateForPrimary(p));
            Assert.AreEqual(CellState.Grass, board.NextStateForSecondary(p));

            board.SetState(p, CellState.Grass);
            Assert.AreEqual(CellState.Soldier, board.NextStateForPrimary(p));
            Assert.AreEqual(CellState.Empty, board.NextStateForSecondary(p));
        }

        [Test]
        public void MonsterCells_CannotChange() {
            BoardModel board = Board();
            Assert.IsFalse(board.SetState(new GridPos(0, 0), CellState.Soldier));
            Assert.AreEqual(0, board.SoldierCount);
        }

        [Test]
        public void SoldierCount_AndEvents_TrackChanges() {
            BoardModel board = Board();
            int events = 0;
            board.CellChanged += (_, __) => events++;

            board.SetState(new GridPos(1, 1), CellState.Soldier);
            board.SetState(new GridPos(1, 1), CellState.Soldier); // sem mudança, sem evento
            board.SetState(new GridPos(0, 1), CellState.Soldier);
            Assert.AreEqual(2, board.SoldierCount);

            board.Clear();
            Assert.AreEqual(0, board.SoldierCount);
            Assert.AreEqual(4, events);
        }
    }
}
