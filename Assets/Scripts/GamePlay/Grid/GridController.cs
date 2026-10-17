// Scripts/Managers/GridController.cs
using System;
using DG.Tweening;
using MonstersAndSoldiers.Core;
using UnityEngine;

/// <summary>
/// Cria o modelo de cada nível, instancia a <see cref="GridView"/> correspondente e cuida da transição entre grids.
/// </summary>
public class GridController : MonoBehaviour {
    public static GridController Instance;

    [Header("References")]
    [SerializeField] private GridView GridViewPrefab;
    [SerializeField] private Transform gridParent;
    [SerializeField] private Transform boardBackground;

    [Header("Settings")]
    [SerializeField] private float margin = 0.5f;
    [SerializeField] private float spacing = 0.2f;
    [SerializeField] private float transitionDistance = 25f;
    [SerializeField] private float transitionDuration = 0.5f;

    private GridView currentGrid;
    private GridView nextGrid;
    private Vector3 finalPosition;

    /// <summary>Nível feito à mão em jogo. Null no modo procedural.</summary>
    public LevelData CurrentLevel { get; private set; }
    public GridView CurrentGrid => currentGrid;
    public BoardModel Board => currentGrid != null ? currentGrid.Board : null;
    public bool IsTransitioning => nextGrid != null;

    /// <summary>Disparado quando um novo tabuleiro fica ativo (início, reinício ou próximo nível).</summary>
    public event Action<BoardModel> BoardChanged;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    public void GenerateGrid(LevelData levelData) => GenerateGrid(levelData.CreateBoard(), levelData);

    /// <summary>Mostra um tabuleiro já criado (modo procedural, ou nível feito à mão via <paramref name="level"/>).</summary>
    public void GenerateGrid(BoardModel board, LevelData level = null) {
        CurrentLevel = level;
        if (currentGrid != null) {
            Destroy(currentGrid.gameObject);
        }
        currentGrid = Instantiate(GridViewPrefab, gridParent);
        finalPosition = currentGrid.transform.position;
        Initialize(currentGrid, board);
        BoardChanged?.Invoke(currentGrid.Board);
    }

    public void TransitionToNextGrid(LevelData nextLevel) => TransitionToNextGrid(nextLevel.CreateBoard(), nextLevel);

    public void TransitionToNextGrid(BoardModel nextBoard, LevelData nextLevel = null) {
        if (IsTransitioning) return;

        nextGrid = Instantiate(GridViewPrefab, gridParent);
        nextGrid.transform.position = new Vector3(transitionDistance, 0, 0);

        GridView oldGrid = currentGrid;
        if (oldGrid != null) {
            oldGrid.transform.DOMove(new Vector3(-transitionDistance, 0, 0), transitionDuration).SetLink(oldGrid.gameObject);
        }

        nextGrid.transform.DOMove(finalPosition, transitionDuration).SetLink(nextGrid.gameObject).OnComplete(() => {
            if (oldGrid != null) Destroy(oldGrid.gameObject);

            Initialize(nextGrid, nextBoard);
            currentGrid = nextGrid;
            nextGrid = null;
            CurrentLevel = nextLevel;
            BoardChanged?.Invoke(currentGrid.Board);
        });
    }

    private void Initialize(GridView grid, BoardModel board) {
        grid.InitializeGrid(
            board,
            grid.GetComponent<BoxCollider2D>().bounds,
            margin,
            spacing,
            boardBackground.position
        );
    }
}
