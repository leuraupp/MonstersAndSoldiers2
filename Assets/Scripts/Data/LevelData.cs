// Scripts/Data/LevelData.cs
using System.Collections.Generic;
using MonstersAndSoldiers.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "Battle Grid/Level Data")]
public class LevelData : ScriptableObject {
    [Header("Basic Info")]
    public string levelName;
    public int levelID;

    [Header("Grid Configuration")]
    [Tooltip("Number of rows in the grid")]
    public int rows = 8;

    [Tooltip("Number of columns in the grid")]
    public int columns = 8;

    [Header("Monsters")]
    public List<MonsterPlacement> monsterPlacements = new List<MonsterPlacement>();

    [Header("Clues (vazio = nível sem pistas)")]
    [Tooltip("Soldados por linha, da linha 0 (embaixo) para cima")]
    public int[] rowClues = new int[0];

    [Tooltip("Soldados por coluna, da coluna 0 (esquerda) para a direita")]
    public int[] columnClues = new int[0];

    [Header("Tutorial (GDD 6.1)")]
    [Tooltip("Balão com a regra nova, mostrado quando o nível começa. Vazio = sem balão.")]
    [TextArea(2, 5)] public string tutorialHint;

    [Tooltip("Uma solução conhecida (x = linha, y = coluna). Usada pelos testes; vazio nos níveis antigos.")]
    public List<Vector2Int> solution = new List<Vector2Int>();

    [Header("Visuals")]
    public Sprite backgroundSprite;
    public Color backgroundColor = Color.white;

    public bool HasClues => rowClues != null && columnClues != null &&
                            rowClues.Length == rows && columnClues.Length == columns;

    /// <summary>Cria o modelo do tabuleiro a partir deste nível.</summary>
    public BoardModel CreateBoard() {
        var monsters = new Dictionary<GridPos, MonsterType>();
        foreach (MonsterPlacement m in monsterPlacements) {
            monsters[new GridPos(m.gridPosition.x, m.gridPosition.y)] = m.monsterType;
        }

        return HasClues
            ? new BoardModel(rows, columns, monsters, rowClues, columnClues)
            : new BoardModel(rows, columns, monsters);
    }
}
