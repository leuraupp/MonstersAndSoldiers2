// Scripts/Managers/GridController.cs
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour {
    public static GridController Instance;

    [Header("References")]
    [SerializeField] private GridView gridView;
    [SerializeField] private GameObject board;

    [Header("Settings")]
    [SerializeField] private float margin = 0.5f;
    [SerializeField] private float spacing = 0.2f;

    private GridData currentGrid;
    private Bounds boardBounds;
    private LevelData currentLevel;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    public void GenerateGrid(LevelData levelData) {
        currentLevel = levelData;
        CalculateBoardBounds();
        gridView.InitializeGrid(levelData.rows, levelData.columns, levelData.monsterPlacements, boardBounds, margin, spacing);
    }

    private void CalculateBoardBounds() {
        BoxCollider2D collider = board.GetComponent<BoxCollider2D>();
        boardBounds = collider.bounds;
    }

    private int GetRequiredSoldiers(MonsterType monsterType) {
        return monsterType switch {
            MonsterType.Slime => 1,
            MonsterType.Goblin => 2,
            MonsterType.Orc => 3,
            _ => 0
        };
    }

    private bool IsValidPosition(int row, int col) {
        return row >= 0 && row < currentGrid.rows &&
               col >= 0 && col < currentGrid.columns;
    }

    public bool TryPlaceSoldier(Vector2Int gridPosition) {
        // Implementaremos na próxima etapa
        return true;
    }
}