namespace MonstersAndSoldiers.Core {
    public enum MonsterType {
        Slime,
        Goblin,
        Orc
    }

    /// <summary>Estado de uma casa sem monstro (GDD 4.1).</summary>
    public enum CellState {
        Empty,
        Soldier,
        Grass
    }

    public enum FaceDirection {
        Up,
        Down,
        Left,
        Right
    }

    public static class MonsterRules {
        /// <summary>Quantos soldados cada monstro exige ao seu redor (GDD 3, regra 1).</summary>
        public static int RequiredSoldiers(MonsterType type) {
            return type switch {
                MonsterType.Slime => 1,
                MonsterType.Goblin => 2,
                MonsterType.Orc => 3,
                _ => 0
            };
        }

        /// <summary>
        /// Direção para olhar de <paramref name="from"/> até <paramref name="to"/>.
        /// Em diagonais, prioriza o eixo horizontal.
        /// </summary>
        public static FaceDirection DirectionTo(GridPos from, GridPos to) {
            GridPos d = to - from;
            if (d.Col > 0) return FaceDirection.Right;
            if (d.Col < 0) return FaceDirection.Left;
            return d.Row > 0 ? FaceDirection.Up : FaceDirection.Down;
        }
    }
}
