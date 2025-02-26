// Scripts/Managers/GridController.cs
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GridController : MonoBehaviour {
    public static GridController Instance;

    [Header("References")]
    [SerializeField] private GridView GridViewPrefab;
    [SerializeField] private Transform gridParent;

    [Header("Settings")]
    [SerializeField] private float margin = 0.5f;
    [SerializeField] private float spacing = 0.2f;
    [SerializeField] private float transitionDistance = 25f;

    private GridView currentGrid;
    private GridView nextGrid;
    private LevelData currentLevel;
    private List<Vector2Int> soldierPositions = new List<Vector2Int>();
    private Vector3 finalPosition;

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
        if (currentGrid != null) {
            Destroy(currentGrid.gameObject);
        }
        currentGrid = Instantiate(GridViewPrefab, gridParent);
        currentGrid.InitializeGrid(levelData.rows, levelData.columns, levelData.monsterPlacements, currentGrid.GetComponent<BoxCollider2D>().bounds, margin, spacing);
        finalPosition = currentGrid.transform.position;
    }

    public void TransitionToNextGrid(LevelData nextLevel) {
        if (nextGrid != null) {
            Destroy(nextGrid.gameObject);
        }
        nextGrid = Instantiate(GridViewPrefab, gridParent);
        nextGrid.transform.position = new Vector3(transitionDistance, 0, 0);
        currentGrid.transform.DOMove(new Vector3(-transitionDistance, 0, 0), 0.5f);
        nextGrid.transform.DOMove(finalPosition, 0.5f).OnComplete(() => {
            Destroy(currentGrid.gameObject);
            nextGrid.InitializeGrid(nextLevel.rows, nextLevel.columns, nextLevel.monsterPlacements, nextGrid.GetComponent<BoxCollider2D>().bounds, margin, spacing);
            currentGrid = nextGrid;
            nextGrid = null;
            currentLevel = nextLevel;
        });
    }

    public int GetRequiredSoldiers(MonsterType monsterType) {
        return monsterType switch {
            MonsterType.Slime => 1,
            MonsterType.Goblin => 2,
            MonsterType.Orc => 3,
            _ => 0
        };
    }

    public void TryPlaceSoldier(Vector2Int gridPosition) {
        soldierPositions.Add(gridPosition);
    }

    public void TryRemoveSoldier(Vector2Int gridPosition) {
        soldierPositions.Remove(gridPosition);
    }

    public void HighlightInvalidSoldiers(HashSet<Vector2Int> invalidSoldiers) {
        foreach (var pos in invalidSoldiers) {
            currentGrid.SetInvalidCell(pos);
        }
    }
    public void HighlightUnsatisfiedMonsters(HashSet<Vector2Int> unsatisfiedMonsters) {
        foreach (var pos in unsatisfiedMonsters) {
            currentGrid.SetInvalidCell(pos);
        }
    }
    public void ShowSoldierLimitExceededMessage() {
        Debug.Log("Limite de soldados excedido!");
    }
}