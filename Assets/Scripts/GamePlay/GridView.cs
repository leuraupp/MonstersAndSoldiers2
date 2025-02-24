// Scripts/Gameplay/GridView.cs
using UnityEngine;
using DG.Tweening;

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

    public void InitializeGrid(int rows, int cols, Bounds boardBounds, float margin, float spacing) {
        this.spacing = spacing;

        // Calcula tamanho das células
        float availableWidth = boardBounds.size.x - (2 * margin);
        float availableHeight = boardBounds.size.y - (2 * margin);

        cellSize = Mathf.Min(
            (availableWidth - (cols - 1) * spacing) / cols,
            (availableHeight - (rows - 1) * spacing) / rows
        );

        // Cria grid
        cells = new GridCell[rows, cols];
        Vector2 startPos = new Vector2(
            boardBounds.min.x + margin + cellSize / 2,
            boardBounds.min.y + margin + cellSize / 2
        );

        float delayOffset = 0.01f;
        for (int row = 0; row < rows; row++) {
            for (int col = 0; col < cols; col++) {
                Vector3 position = new Vector3(
                    startPos.x + col * (cellSize + spacing),
                    startPos.y + row * (cellSize + spacing),
                    0
                );

                GridCell cell = Instantiate(cellPrefab, gridParent);
                cell.transform.position = position;

                // Novo: Ajuste de escala baseado no tamanho calculado
                float scaleFactor = cellSize / cell.BaseSize;
                cell.transform.localScale = new Vector3(0, 0, 1);
                cell.transform.DOScale(scaleFactor, 0.2f).SetDelay(delayOffset);

                cell.Initialize(emptyColor);
                cells[row, col] = cell;
            }
        }
    }

    public void UpdateCellVisual(int row, int col, CellType type) {
        if (!IsValidPosition(row, col))
            return;

        Color color = type switch {
            CellType.Slime => slimeColor,
            CellType.Goblin => goblinColor,
            CellType.Orc => orcColor,
            CellType.Soldier => soldierColor,
            _ => emptyColor
        };

        cells[row, col].SetColor(color);
    }

    public bool TrySetCellType(Vector2Int gridPosition, CellType newType) {
        if (!IsValidPosition(gridPosition.y, gridPosition.x))
            return false;
        if (cells[gridPosition.y, gridPosition.x].CurrentType != CellType.Empty)
            return false;

        UpdateCellVisual(gridPosition.y, gridPosition.x, newType);
        return true;
    }

    private bool IsValidPosition(int row, int col) {
        return row >= 0 && row < cells.GetLength(0) &&
               col >= 0 && col < cells.GetLength(1);
    }
}