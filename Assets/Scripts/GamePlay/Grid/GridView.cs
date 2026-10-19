// Scripts/Gameplay/GridView.cs
using System;
using System.Collections.Generic;
using DG.Tweening;
using MonstersAndSoldiers.Core;
using TMPro;
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

    [Header("Clues")]
    [SerializeField] private Color clueColor = Color.white;
    [Tooltip("Linha/coluna com a contagem certa")]
    [SerializeField] private Color clueDoneColor = new Color(1f, 1f, 1f, 0.35f);
    [Tooltip("Linha/coluna com soldados demais")]
    [SerializeField] private Color clueOverColor = new Color(1f, 0.35f, 0.35f);
    [SerializeField] private Color clueFlashColor = new Color(0.9f, 0.15f, 0.15f);
    [SerializeField] private int clueSortingOrder = 5;

    public BoardModel Board { get; private set; }

    private GridCell[,] cells;
    private float cellSize;
    private float step;
    private Vector2 startPos;
    private GridPos? highlighted;
    private TextMeshPro[] rowClueLabels;
    private TextMeshPro[] colClueLabels;

    public void InitializeGrid(BoardModel board, Bounds boardBounds, float margin, float spacing, Vector2 boardOrigin) {
        Unbind();
        Board = board;
        int rows = board.Rows;
        int cols = board.Cols;

        float availableWidth = boardBounds.size.x - (2 * margin);
        float availableHeight = boardBounds.size.y - (2 * margin);

        // Com pistas, reserva uma coluna à esquerda e uma linha em cima para os números (GDD 8.3).
        int clueSpace = board.HasClues ? 1 : 0;
        int layoutCols = cols + clueSpace;
        int layoutRows = rows + clueSpace;

        cellSize = Mathf.Min(
            (availableWidth - (layoutCols - 1) * spacing) / layoutCols,
            (availableHeight - (layoutRows - 1) * spacing) / layoutRows
        );
        step = cellSize + spacing;

        startPos = boardOrigin + new Vector2(
            -((cols - 1) * step) / 2 + 0.5f + clueSpace * step / 2,
            -((rows - 1) * step) / 2 - 0.5f - clueSpace * step / 2
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

        if (board.HasClues) CreateClueLabels(delayOffset);
        RefreshAllFeedback();
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
        AudioService.Play(state switch {
            CellState.Soldier => Sfx.PlaceSoldier,
            CellState.Grass => Sfx.PlaceGrass,
            _ => Sfx.ClearCell
        });

        RefreshClue(rowClueLabels, pos.Row, Board.CountSoldiersInRow(pos.Row), Board.RowClues);
        RefreshClue(colClueLabels, pos.Col, Board.CountSoldiersInCol(pos.Col), Board.ColClues);
        RefreshTouching(pos);
        foreach (GridPos n in Board.InsideNeighbors8(pos)) RefreshTouching(n);
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
        AudioService.Play(Sfx.Error);
        var flashed = new HashSet<GridPos>();
        void Flash(GridPos p) {
            if (flashed.Add(p)) SetInvalidCell(p);
        }

        foreach (GridPos p in result.UnsatisfiedMonsters) Flash(p);
        foreach (GridPos p in result.TouchingSoldiers) Flash(p);
        foreach (GridPos p in result.UnpairedSoldiers) Flash(p);

        if (rowClueLabels == null) return;
        foreach (int row in result.WrongRows) FlashClue(rowClueLabels, row, Board.CountSoldiersInRow(row), Board.RowClues);
        foreach (int col in result.WrongCols) FlashClue(colClueLabels, col, Board.CountSoldiersInCol(col), Board.ColClues);
    }

    // ---------- Pistas e contorno ----------

    /// <summary>Pistas das linhas à esquerda (coluna -1) e das colunas em cima (linha Rows).</summary>
    private void CreateClueLabels(float delay) {
        rowClueLabels = new TextMeshPro[Board.Rows];
        colClueLabels = new TextMeshPro[Board.Cols];
        for (int row = 0; row < Board.Rows; row++) {
            rowClueLabels[row] = CreateClueLabel($"Row Clue {row}", new GridPos(row, -1), Board.RowClues[row], delay);
        }
        for (int col = 0; col < Board.Cols; col++) {
            colClueLabels[col] = CreateClueLabel($"Col Clue {col}", new GridPos(Board.Rows, col), Board.ColClues[col], delay);
        }
    }

    private TextMeshPro CreateClueLabel(string labelName, GridPos pos, int value, float delay) {
        var go = new GameObject(labelName);
        go.transform.SetParent(gridParent, false);
        go.transform.position = CellToWorld(pos);

        TextMeshPro label = go.AddComponent<TextMeshPro>();
        label.text = value.ToString();
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.enableAutoSizing = true;
        label.fontSizeMin = 1f;
        label.fontSizeMax = 20f;
        label.rectTransform.sizeDelta = new Vector2(cellSize, cellSize) / Mathf.Max(0.0001f, gridParent.lossyScale.x);
        label.sortingLayerID = cells[0, 0].SortingLayerID;
        label.sortingOrder = clueSortingOrder;

        go.transform.localScale = new Vector3(0, 0, 1);
        go.transform.DOScale(1f, 0.2f).SetDelay(delay).SetLink(go);
        return label;
    }

    private void RefreshAllFeedback() {
        if (rowClueLabels != null) {
            for (int row = 0; row < Board.Rows; row++) RefreshClue(rowClueLabels, row, Board.CountSoldiersInRow(row), Board.RowClues);
            for (int col = 0; col < Board.Cols; col++) RefreshClue(colClueLabels, col, Board.CountSoldiersInCol(col), Board.ColClues);
        }
        for (int row = 0; row < Board.Rows; row++) {
            for (int col = 0; col < Board.Cols; col++) RefreshTouching(new GridPos(row, col));
        }
    }

    /// <summary>Cor da pista: normal, apagada quando bate, vermelha quando passa.</summary>
    private Color ClueColor(int count, int clue) {
        if (count > clue) return clueOverColor;
        return count == clue ? clueDoneColor : clueColor;
    }

    private void RefreshClue(TextMeshPro[] labels, int index, int count, IReadOnlyList<int> clues) {
        if (labels == null) return;
        TextMeshPro label = labels[index];
        DOTween.Kill(label);
        label.color = ClueColor(count, clues[index]);
    }

    private void FlashClue(TextMeshPro[] labels, int index, int count, IReadOnlyList<int> clues) {
        TextMeshPro label = labels[index];
        Color rest = ClueColor(count, clues[index]);
        DOTween.Kill(label);
        DOTween.To(() => label.color, c => label.color = c, clueFlashColor, 0.3f)
            .SetLoops(2, LoopType.Yoyo)
            .SetTarget(label)
            .SetLink(label.gameObject)
            .OnComplete(() => label.color = rest);
    }

    /// <summary>Regra 3 ao vivo: soldado encostado em outro soldado ganha contorno vermelho.</summary>
    private void RefreshTouching(GridPos pos) {
        bool touching = false;
        if (Board.GetState(pos) == CellState.Soldier) {
            foreach (GridPos n in Board.InsideNeighbors8(pos)) {
                if (!Board.IsMonster(n) && Board.GetState(n) == CellState.Soldier) {
                    touching = true;
                    break;
                }
            }
        }
        cells[pos.Row, pos.Col].SetOutline(touching);
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
        AudioService.Play(Sfx.Attack);
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
