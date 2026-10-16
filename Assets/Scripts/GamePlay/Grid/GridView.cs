// Scripts/Gameplay/GridView.cs
using System;
using System.Collections.Generic;
using DG.Tweening;
using MonstersAndSoldiers.Core;
using UnityEngine;

/// <summary>
/// Desenha um <see cref="BoardModel"/> e reage às mudanças dele. Também converte posição do mundo em casa.
/// </summary>
public class GridView : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private GridCell cellPrefab;

    [Header("Colors")]
    [SerializeField] private Color emptyColor = Color.gray;
    [SerializeField] private Color slimeColor = Color.green;
    [SerializeField] private Color goblinColor = Color.yellow;
    [SerializeField] private Color orcColor = Color.red;

    public BoardModel Board { get; private set; }

    private GridCell[,] cells;
    private float cellSize;
    private float step;
    private Vector2 startPos;
    private GridPos? highlighted;

    public void InitializeGrid(BoardModel board, Bounds boardBounds, float margin, float spacing, Vector2 boardOrigin) {
        Unbind();
        Board = board;
        int rows = board.Rows;
        int cols = board.Cols;

        float availableWidth = boardBounds.size.x - (2 * margin);
        float availableHeight = boardBounds.size.y - (2 * margin);

        cellSize = Mathf.Min(
            (availableWidth - (cols - 1) * spacing) / cols,
            (availableHeight - (rows - 1) * spacing) / rows
        );
        step = cellSize + spacing;

        startPos = boardOrigin + new Vector2(
            -((cols - 1) * step) / 2 + 0.5f,
            -((rows - 1) * step) / 2 - 0.5f
        );

        cells = new GridCell[rows, cols];
        float delayOffset = 0.01f;
        for (int row = 0; row < rows; row++) {
            for (int col = 0; col < cols; col++) {
                var pos = new GridPos(row, col);
                GridCell cell = Instantiate(cellPrefab, gridParent);
                cell.transform.position = CellToWorld(pos);
                float scaleFactor = cellSize / cell.BaseSize;
                cell.transform.localScale = new Vector3(0, 0, 1);
                cell.transform.DOScale(scaleFactor, 0.2f).SetDelay(delayOffset).SetLink(cell.gameObject);

                cell.Initialize(pos, emptyColor);
                if (board.TryGetMonster(pos, out MonsterType monster)) {
                    cell.SetMonster(monster, MonsterTypeToColor(monster));
                } else {
                    cell.SetState(board.GetState(pos), emptyColor);
                }

                cells[row, col] = cell;
                delayOffset += 0.01f;
            }
        }

        board.CellChanged += OnCellChanged;
    }

    private void OnDestroy() {
        Unbind();
    }

    private void Unbind() {
        if (Board != null) Board.CellChanged -= OnCellChanged;
    }

    private void OnCellChanged(GridPos pos, CellState state) {
        GridCell cell = cells[pos.Row, pos.Col];
        cell.SetState(state, emptyColor);
        if (state == CellState.Soldier) FaceNeighbors(pos);
    }

    // ---------- Posição ----------

    public Vector3 CellToWorld(GridPos pos) {
        return new Vector3(startPos.x + pos.Col * step, startPos.y + pos.Row * step, 0);
    }

    /// <summary>Casa sob um ponto do mundo. False se o ponto cai fora do tabuleiro ou no espaço entre casas.</summary>
    public bool TryWorldToCell(Vector2 world, out GridPos pos) {
        pos = default;
        if (Board == null) return false;

        float fc = (world.x - startPos.x) / step;
        float fr = (world.y - startPos.y) / step;
        int col = Mathf.RoundToInt(fc);
        int row = Mathf.RoundToInt(fr);

        float half = cellSize / 2f / step;
        if (Mathf.Abs(fc - col) > half || Mathf.Abs(fr - row) > half) return false;

        pos = new GridPos(row, col);
        return Board.IsInside(pos);
    }

    // ---------- Feedback ----------

    public void SetHighlight(GridPos? pos) {
        if (highlighted == pos) return;
        if (highlighted.HasValue) cells[highlighted.Value.Row, highlighted.Value.Col].SetHighlight(false);
        highlighted = pos;
        if (pos.HasValue) cells[pos.Value.Row, pos.Value.Col].SetHighlight(true);
    }

    public void SetInvalidCell(GridPos pos) {
        cells[pos.Row, pos.Col].FlashRed();
    }

    public void ShowValidationErrors(ValidationResult result) {
        var flashed = new HashSet<GridPos>();
        void Flash(GridPos p) {
            if (flashed.Add(p)) SetInvalidCell(p);
        }

        foreach (GridPos p in result.UnsatisfiedMonsters) Flash(p);
        foreach (GridPos p in result.TouchingSoldiers) Flash(p);
        foreach (GridPos p in result.UnpairedSoldiers) Flash(p);
        // TODO Fase 2: destacar as pistas (WrongRows / WrongCols) quando elas forem exibidas.
    }

    // ---------- Animações ----------

    /// <summary>Soldado recém-colocado olha para um monstro vizinho, e esses monstros olham para ele.</summary>
    private void FaceNeighbors(GridPos soldier) {
        bool facedSoldier = false;
        foreach (GridPos n in Board.InsideNeighbors8(soldier)) {
            if (!Board.IsMonster(n)) continue;
            if (!facedSoldier) {
                cells[soldier.Row, soldier.Col].Face(MonsterRules.DirectionTo(soldier, n));
                facedSoldier = true;
            }
            cells[n.Row, n.Col].Face(MonsterRules.DirectionTo(n, soldier));
        }
    }

    /// <summary>
    /// Todos os personagens atacam. Soldados pareados olham para o seu monstro antes.
    /// <paramref name="onComplete"/> é chamado uma vez, quando a última animação terminar.
    /// </summary>
    public void PlayAttack(ValidationResult result, Action onComplete) {
        foreach (KeyValuePair<GridPos, GridPos> pair in result.SoldierToMonster) {
            cells[pair.Key.Row, pair.Key.Col].Face(MonsterRules.DirectionTo(pair.Key, pair.Value));
            cells[pair.Value.Row, pair.Value.Col].Face(MonsterRules.DirectionTo(pair.Value, pair.Key));
        }

        int pending = 1; // segura o callback até todas as animações terem começado
        void Done() {
            if (--pending == 0) onComplete?.Invoke();
        }

        foreach (GridCell cell in cells) {
            pending++;
            if (!cell.PlayAttack(result.IsValid, Done)) pending--;
        }
        Done();
    }

    private Color MonsterTypeToColor(MonsterType monsterType) {
        return monsterType switch {
            MonsterType.Slime => slimeColor,
            MonsterType.Goblin => goblinColor,
            MonsterType.Orc => orcColor,
            _ => emptyColor
        };
    }
}
