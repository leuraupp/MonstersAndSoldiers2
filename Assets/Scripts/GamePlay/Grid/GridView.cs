// Scripts/Gameplay/GridView.cs
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class GridView : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private GridCell cellPrefab;

    [Header("Colors")]
    [SerializeField] private Color emptyColor = Color.gray;
    [SerializeField] private Color slimeColor = Color.green;
    [SerializeField] private Color goblinColor = Color.yellow;
    [SerializeField] private Color orcColor = Color.red;
    [SerializeField] private Color soldierColor = Color.blue;

    private GridCell[,] cells;
    private float cellSize;
    private float spacing;

    public void InitializeGrid(int rows, int cols, List<MonsterPlacement> monsters, Bounds boardBounds, float margin, float spacing, Vector2 boardOrigin) {
        Debug.Log($"[InitializeGrid] Inicializando grid com {rows} linhas, {cols} colunas, bounds: {boardBounds}, margin: {margin}, spacing: {spacing}, boardOrigin: {boardOrigin}");
        this.spacing = spacing;

        float availableWidth = boardBounds.size.x - (2 * margin);
        float availableHeight = boardBounds.size.y - (2 * margin);

        cellSize = Mathf.Min(
            (availableWidth - (cols - 1) * spacing) / cols,
            (availableHeight - (rows - 1) * spacing) / rows
        );
        Debug.Log($"[InitializeGrid] cellSize calculado: {cellSize}");

        cells = new GridCell[rows, cols];
        Vector2 startPos = boardOrigin + new Vector2(
            -((cols - 1) * (cellSize + spacing)) / 2 + 0.5f,
            -((rows - 1) * (cellSize + spacing)) / 2 - 0.5f
        );
        Debug.Log($"[InitializeGrid] startPos: {startPos}");

        float delayOffset = 0.01f;
        for (int row = 0; row < rows; row++) {
            for (int col = 0; col < cols; col++) {
                Vector3 position = new Vector3(
                    startPos.x + col * (cellSize + spacing),
                    startPos.y + row * (cellSize + spacing),
                    0
                );
                Debug.Log($"[InitializeGrid] Instanciando célula em {position} (row {row}, col {col})");
                GridCell cell = Instantiate(cellPrefab, gridParent);
                cell.transform.position = position;
                float scaleFactor = cellSize / cell.BaseSize;
                cell.transform.localScale = new Vector3(0, 0, 1);
                cell.transform.DOScale(scaleFactor, 0.2f).SetDelay(delayOffset);
                cell.Initialize(emptyColor);
                cell.SetGridPosition(new Vector2Int(row, col));
                cell.SetType(CellType.Empty);
                foreach (MonsterPlacement monster in monsters) {
                    if (monster.gridPosition == new Vector2Int(row, col)) {
                        Debug.Log($"[InitializeGrid] Colocando monstro {monster.monsterType} na célula ({row}, {col})");
                        cell.SetColor(MonsterTypeToColor(monster.monsterType));
                        cell.SetType(MonsterTypeToCellType(monster.monsterType));
                    }
                }
                cells[row, col] = cell;
                delayOffset += 0.01f;
            }
        }
        Debug.Log("[InitializeGrid] Grid criada com sucesso!");
    }

    private CellType MonsterTypeToCellType(MonsterType monsterType) {
        return monsterType switch {
            MonsterType.Slime => CellType.Slime,
            MonsterType.Goblin => CellType.Goblin,
            MonsterType.Orc => CellType.Orc,
            _ => CellType.Empty
        };
    }

    private Color MonsterTypeToColor(MonsterType monsterType) {
        return monsterType switch {
            MonsterType.Slime => slimeColor,
            MonsterType.Goblin => goblinColor,
            MonsterType.Orc => orcColor,
            _ => emptyColor
        };
    }

    public void SetCellColor(Vector2Int gridPosition, Color color) {
        cells[gridPosition.x, gridPosition.y].SetColor(color);
    }

    public void SetInvalidCell(Vector2Int gridPosition) {
        cells[gridPosition.x, gridPosition.y].FlashRed();
    }

    public GridCell GetCellAtPosition(Vector2Int gridPosition) {
        return cells[gridPosition.x, gridPosition.y];
    }

    public List<GridCell> GetNonEmptyCells() {
        List<GridCell> nonEmptyCells = new List<GridCell>();
        foreach (GridCell cell in cells) {
            if (cell.CurrentType != CellType.Empty) {
                nonEmptyCells.Add(cell);
            }
        }
        return nonEmptyCells;
    }

    public void ShowAttackAnimation() {
        List<GridCell> cells = GetNonEmptyCells();
        Debug.Log("Showing attack animation");
        Debug.Log(cells.Count);

        foreach (GridCell cell in cells) {
            Debug.Log("cell.CurrentType.ToString(): " + cell.CurrentType.ToString());
            Debug.Log("cell.CurrentFaceDirection.ToString(): " + cell.CurrentFaceDirection.ToString());
            cell.AnimationSwitcher.AnimationPlay("Attack", cell.CurrentType.ToString(), cell.CurrentFaceDirection.ToString());
        }
    }
}