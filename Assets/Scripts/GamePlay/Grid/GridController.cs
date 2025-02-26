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
    private List<Vector2Int> soldierPositions = new List<Vector2Int>();

    //get set currentLevel
    public LevelData CurrentLevel {
        get => currentLevel;
    }

    public List<Vector2Int> SoldierPositions {
        get => soldierPositions;
    }

    public int RemainingSoldiers {
        get => currentLevel.maxSoldiers - soldierPositions.Count;
    }

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

    public int GetRequiredSoldiers(MonsterType monsterType) {
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

    public void TryPlaceSoldier(Vector2Int gridPosition) {
        soldierPositions.Add(gridPosition);
    }

    public void TryRemoveSoldier(Vector2Int gridPosition) {
        soldierPositions.Remove(gridPosition);
    }

    public void HighlightInvalidSoldiers(HashSet<Vector2Int> invalidSoldiers) {
        foreach (var pos in invalidSoldiers) {
            gridView.SetInvalidCell(pos);
        }
    }
    public void HighlightUnsatisfiedMonsters(HashSet<Vector2Int> unsatisfiedMonsters) {
        foreach (var pos in unsatisfiedMonsters) {
            gridView.SetInvalidCell(pos);
        }
    }
    public void ShowSoldierLimitExceededMessage() {
        Debug.Log("Limite de soldados excedido!");
    }
}