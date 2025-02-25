// Scripts/Gameplay/GridData.cs
using UnityEngine;

[System.Serializable]
public class GridData {
    public int rows;
    public int columns;
    public CellData[,] cells;

    public GridData(int rows, int columns) {
        this.rows = rows;
        this.columns = columns;
        cells = new CellData[rows, columns];

        // Inicializa todas as células como vazias
        for (int i = 0; i < rows; i++) {
            for (int j = 0; j < columns; j++) {
                cells[i, j] = new CellData {
                    cellType = CellType.Empty,
                    gridPosition = new Vector2Int(i, j),
                    requiredSoldiers = 0,
                    currentSoldiers = 0
                };
            }
        }
    }
}

[System.Serializable]
public class CellData {
    public CellType cellType;
    public Vector2Int gridPosition;
    public int requiredSoldiers;
    public int currentSoldiers;
}

public enum CellType { Empty, Slime, Goblin, Orc, Soldier }