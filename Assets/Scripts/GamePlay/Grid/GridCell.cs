using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class GridCell : MonoBehaviour {
    [Header("Visuals")]
    [SerializeField] private List<GameObject> sprites;
    [SerializeField] private SpriteRenderer BGColor;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color leftClickColor = Color.green;
    [SerializeField] private Color rightClickColor = Color.red;

    [Header("Size Settings")]
    [SerializeField] private float baseSize = 1f; // Tamanho original do sprite

    public float BaseSize => baseSize;

    public CellType CurrentType { get; private set; }

    private Color originalColor;
    private bool isMouseOver;
    private bool isSelected = false;
    private Vector2Int gridPosition;


    void Start() {
        originalColor = BGColor.color; // Guarda a cor original
    }

    private void Update() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        bool wasMouseOver = isMouseOver;
        isMouseOver = Physics.Raycast(ray, out hit) && hit.collider.gameObject == gameObject;

        // Detecta mudança de estado
        if (!wasMouseOver && isMouseOver)
            OnMouseEnter();
        if (wasMouseOver && !isMouseOver)
            OnMouseExit();

        // Detecta cliques
        if (isMouseOver) {
            if (Input.GetMouseButtonDown(0))
                OnLeftClick();
            if (Input.GetMouseButtonDown(1))
                OnRightClick();
        }
    }

    public void Initialize(Color color) {
        SetColor(color);
    }

    public void SetColor(Color newColor) {
        BGColor.color = newColor;
    }

    public void SetType(CellType newType) {
        CurrentType = newType;
        SetCellSprite();
        if (newType == CellType.Soldier) {
            GridController.Instance.TryPlaceSoldier(gridPosition);
        } else {
            GridController.Instance.TryRemoveSoldier(gridPosition);
        }
        name = newType.ToString() + " " + gridPosition;
    }

    public void SetGridPosition(Vector2Int position) {
        gridPosition = position;
    }

    private void SetCellSprite() {
        foreach (GameObject sprite in sprites) {
            if (sprite.name == CurrentType.ToString()) {
                sprite.SetActive(true);
            } else {
                sprite.SetActive(false);
            }
        }
    }

    private void OnMouseEnter() {
        if (!isSelected) {
            BGColor.color = highlightColor;
        }
    }

    private void OnMouseExit() {
        if (!isSelected) {
            BGColor.color = originalColor;
        }
    }

    private void OnLeftClick() {
        if (CurrentType != CellType.Empty && CurrentType != CellType.Soldier) {
            return;
        }
        isSelected = !isSelected;
        if (isSelected) {
            BGColor.color = leftClickColor;
            SetType(CellType.Soldier);
        } else {
            BGColor.color = highlightColor;
            SetType(CellType.Empty);
        }
        
    }

    private void OnRightClick() {
        if (CurrentType != CellType.Empty && CurrentType != CellType.Soldier) {
            return;
        }
        isSelected = !isSelected;
        if (isSelected) {
            BGColor.color = rightClickColor;
        } else {
            BGColor.color = highlightColor;
        }
    }

    public void FlashRed() {
        BGColor.DOColor(new Color(152f / 255f, 54f / 255f, 54f / 255f), 0.3f).SetLoops(2, LoopType.Yoyo);
    }

    private void ResetColor() {
        BGColor.color = originalColor;
    }

}
