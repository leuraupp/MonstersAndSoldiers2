using System;
using System.Collections.Generic;

namespace MonstersAndSoldiers.Core {
    /// <summary>Dificuldades do modo procedural (GDD 6.2).</summary>
    public enum Difficulty {
        Easy,
        Medium,
        Hard
    }

    /// <summary>Parâmetros de geração de um mapa.</summary>
    public class PuzzleSettings {
        public int Rows { get; }
        public int Cols { get; }

        /// <summary>Tipos sorteados, com peso igual. Repetir um tipo aumenta a chance dele.</summary>
        public IReadOnlyList<MonsterType> MonsterTypes { get; }

        /// <summary>Fração das casas que termina com soldado (GDD 7: cerca de 20%).</summary>
        public float SoldierFill { get; }

        /// <summary>Tentativas até achar um mapa com solução única (GDD 7: 50).</summary>
        public int MaxAttempts { get; }

        public PuzzleSettings(int rows, int cols, IReadOnlyList<MonsterType> monsterTypes,
                              float soldierFill = 0.2f, int maxAttempts = 50) {
            if (rows <= 0 || cols <= 0) throw new ArgumentException("O tabuleiro precisa ter ao menos 1x1.");
            if (monsterTypes == null || monsterTypes.Count == 0) throw new ArgumentException("Informe ao menos um tipo de monstro.");
            Rows = rows;
            Cols = cols;
            MonsterTypes = monsterTypes;
            SoldierFill = soldierFill;
            MaxAttempts = Math.Max(1, maxAttempts);
        }

        /// <summary>Quantos soldados o gerador tenta colocar.</summary>
        public int TargetSoldiers => Math.Max(1, (int)Math.Round(Rows * Cols * SoldierFill));

        /// <summary>Presets da GDD 6.2 (grade col x lin).</summary>
        public static PuzzleSettings For(Difficulty difficulty) {
            return difficulty switch {
                Difficulty.Easy => new PuzzleSettings(4, 6, new[] { MonsterType.Slime }),
                Difficulty.Medium => new PuzzleSettings(6, 8, new[] { MonsterType.Slime, MonsterType.Goblin }),
                Difficulty.Hard => new PuzzleSettings(8, 8, new[] { MonsterType.Slime, MonsterType.Goblin, MonsterType.Orc }),
                _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
            };
        }
    }
}
