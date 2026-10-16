using System;
using System.Collections.Generic;
using DG.Tweening;
using MonstersAndSoldiers.Core;
using UnityEngine;

/// <summary>
/// Visual de uma casa. Não guarda regra nenhuma: só mostra o que o <see cref="GridView"/> mandar.
/// </summary>
public class GridCell : MonoBehaviour {
    private const string SoldierSpriteName = "Soldier";

    [Header("Visuals")]
    [Tooltip("Filhos com o nome do ocupante: Slime, Goblin, Orc, Soldier")]
    [SerializeField] private List<GameObject> sprites;
    [SerializeField] private SpriteRenderer BGColor;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color soldierColor = new Color(0.55f, 0.72f, 0.9f);
    [SerializeField] private Color grassColor = new Color(0.55f, 0.78f, 0.45f);
    [SerializeField] private Color errorColor = new Color(152f / 255f, 54f / 255f, 54f / 255f);
    [SerializeField, Range(0f, 1f)] private float highlightStrength = 0.45f;

    [Header("Size Settings")]
    [SerializeField] private float baseSize = 1f; // Tamanho original do sprite

    public float BaseSize => baseSize;
    public GridPos Position { get; private set; }

    private Color baseColor;
    private bool isHighlighted;
    private AnimationSwitcher occupant;
    private Tween colorTween;

    public void Initialize(GridPos position, Color emptyColor) {
        Position = position;
        baseColor = emptyColor;
        name = $"Cell {position}";
        ShowOccupant(null);
        RefreshColor();
    }

    /// <summary>Configura a casa como monstro (fixo durante todo o nível).</summary>
    public void SetMonster(MonsterType type, Color color) {
        baseColor = color;
        name = $"{type} {Position}";
        ShowOccupant(type.ToString());
        RefreshColor();
    }

    /// <summary>Mostra o estado de uma casa sem monstro.</summary>
    public void SetState(CellState state, Color emptyColor) {
        baseColor = state switch {
            CellState.Soldier => soldierColor,
            CellState.Grass => grassColor,
            _ => emptyColor
        };
        ShowOccupant(state == CellState.Soldier ? SoldierSpriteName : null);
        RefreshColor();
    }

    public void SetHighlight(bool highlighted) {
        if (isHighlighted == highlighted) return;
        isHighlighted = highlighted;
        RefreshColor();
    }

    public void Face(FaceDirection direction) {
        if (occupant != null) occupant.Face(direction);
    }

    /// <summary>Toca o ataque; retorna false (sem chamar o callback) se a casa não tem personagem.</summary>
    public bool PlayAttack(bool success, Action onComplete) {
        if (occupant == null) return false;
        occupant.PlayAttack(success, onComplete);
        return true;
    }

    public void FlashRed() {
        colorTween?.Kill();
        colorTween = BGColor.DOColor(errorColor, 0.3f)
            .SetLoops(2, LoopType.Yoyo)
            .OnKill(ApplyColor);
    }

    private void RefreshColor() {
        colorTween?.Kill();
        colorTween = null;
        ApplyColor();
    }

    private void ApplyColor() {
        if (BGColor == null) return;
        BGColor.color = isHighlighted ? Color.Lerp(baseColor, highlightColor, highlightStrength) : baseColor;
    }

    private void ShowOccupant(string spriteName) {
        occupant = null;
        foreach (GameObject sprite in sprites) {
            bool active = sprite.name == spriteName;
            sprite.SetActive(active);
            if (!active) continue;

            occupant = sprite.GetComponent<AnimationSwitcher>();
            if (occupant == null) occupant = sprite.AddComponent<AnimationSwitcher>();
            occupant.Prefix = spriteName;
        }
    }

    private void OnDestroy() {
        colorTween?.Kill();
    }
}
