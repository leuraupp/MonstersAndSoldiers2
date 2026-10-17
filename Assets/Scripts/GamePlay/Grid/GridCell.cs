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
    [Tooltip("Tufo de grama mostrado quando o jogador marca a casa com o botão direito")]
    [SerializeField] private GameObject grass;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color soldierColor = new Color(0.55f, 0.72f, 0.9f);
    [SerializeField] private Color grassColor = Color.white;
    [SerializeField] private Color errorColor = new Color(152f / 255f, 54f / 255f, 54f / 255f);
    [SerializeField] private Color outlineColor = new Color(0.9f, 0.15f, 0.15f);
    [SerializeField, Range(0f, 1f)] private float highlightStrength = 0.45f;

    [Header("Size Settings")]
    [SerializeField] private float baseSize = 1f; // Tamanho original do sprite

    public float BaseSize => baseSize;
    public GridPos Position { get; private set; }

    private Color baseColor;
    private bool isHighlighted;
    private AnimationSwitcher occupant;
    private Tween colorTween;
    private SpriteRenderer outline;

    public void Initialize(GridPos position, Color emptyColor) {
        Position = position;
        baseColor = emptyColor;
        name = $"Cell {position}";
        ShowOccupant(null);
        ShowGrass(false);
        RefreshColor();
    }

    /// <summary>Configura a casa como monstro (fixo durante todo o nível).</summary>
    public void SetMonster(MonsterType type, Color color) {
        baseColor = color;
        name = $"{type} {Position}";
        ShowOccupant(type.ToString());
        ShowGrass(false);
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
        ShowGrass(state == CellState.Grass);
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

    /// <summary>
    /// Toca o ataque; retorna false (sem chamar o callback) se a casa não tem personagem.
    /// Ataque certo: os monstros morrem. Ataque errado: todo mundo leva dano.
    /// </summary>
    public bool PlayAttack(bool success, Action onComplete) {
        if (occupant == null) return false;
        bool isSoldier = occupant.Prefix == SoldierSpriteName;
        AttackReaction reaction = success
            ? (isSoldier ? AttackReaction.None : AttackReaction.Death)
            : AttackReaction.Hit;
        occupant.PlayAttack(success, reaction, onComplete);
        return true;
    }

    /// <summary>Contorno vermelho ao vivo, usado em soldados que encostam em outro soldado (regra 3).</summary>
    public void SetOutline(bool visible) {
        if (outline == null) {
            if (!visible || BGColor == null) return;
            outline = CreateOutline();
        }
        outline.enabled = visible;
    }

    public int SortingLayerID => BGColor != null ? BGColor.sortingLayerID : 0;

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

    private void ShowGrass(bool visible) {
        if (grass != null && grass.activeSelf != visible) grass.SetActive(visible);
    }

    // Moldura vazada gerada em código, desenhada por cima do fundo da casa e dos personagens.
    private SpriteRenderer CreateOutline() {
        var go = new GameObject("Outline");
        go.transform.SetParent(BGColor.transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = OutlineSprite;
        sr.color = outlineColor;
        sr.sortingLayerID = BGColor.sortingLayerID;
        sr.sortingOrder = BGColor.sortingOrder + 2;

        Vector2 target = BGColor.sprite != null ? (Vector2)BGColor.sprite.bounds.size : Vector2.one;
        Vector2 own = sr.sprite.bounds.size;
        go.transform.localPosition = BGColor.sprite != null ? BGColor.sprite.bounds.center : Vector3.zero;
        go.transform.localScale = new Vector3(target.x / own.x, target.y / own.y, 1f);
        return sr;
    }

    private static Sprite outlineSprite;

    private static Sprite OutlineSprite {
        get {
            if (outlineSprite != null) return outlineSprite;
            const int size = 32;
            const int border = 3;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "CellOutline"
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    bool edge = x < border || y < border || x >= size - border || y >= size - border;
                    pixels[y * size + x] = edge ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            outlineSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return outlineSprite;
        }
    }

    private void OnDestroy() {
        colorTween?.Kill();
    }
}
