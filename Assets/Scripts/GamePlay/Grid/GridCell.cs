using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public enum FaceDirection {
    Up,
    Down,
    Left,
    Right
}

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
    public AnimationSwitcher AnimationSwitcher {
        get { return animationSwitcher; }
        set { animationSwitcher = value; }
    }
    public FaceDirection CurrentFaceDirection { get; private set; }

    private Color originalColor;
    private bool isMouseOver;
    private bool isSelected = false;
    private Vector2Int gridPosition;
    private AnimationSwitcher animationSwitcher;

    private Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                                new Vector2Int(1,1), new Vector2Int(-1,-1), new Vector2Int(1,-1), new Vector2Int(-1,1) };


    void Start() {
        originalColor = BGColor.color; // Guarda a cor original
        CurrentFaceDirection = FaceDirection.Down;
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
                animationSwitcher = sprite.GetComponent<AnimationSwitcher>();
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
            SetFaceDirection();
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

    public void SetFaceDirection() {
        UpdateSoldierFacingDirection();
        UpdateMonsterFacingDirections();
    }

    private void ResetColor() {
        BGColor.color = originalColor;
    }

    private FaceDirection DetermineFaceDirection(Vector2Int currentPosition, Vector2Int targetCell) {
        Vector2Int difference = targetCell - currentPosition;

        Debug.Log("Difference: " + difference);

        FaceDirection faceDirection;

        if (difference.y == 0) {
            if (difference.x > 0) {
                faceDirection = FaceDirection.Up;
            } else {
                faceDirection = FaceDirection.Down;
            }
        }
        else {
            if (difference.y > 0) {
                faceDirection = FaceDirection.Right;
            } else {
                faceDirection = FaceDirection.Left;
            }
        }

        return faceDirection;
    }

    private List<GridCell> GetAdjacentMonsters(Vector2Int soldierPosition) {
        List<GridCell> adjacentMonsters = new List<GridCell>();

        foreach (Vector2Int dir in directions) {
            Vector2Int checkPos = soldierPosition + dir;
            if (GridController.Instance.IsValidPosition(checkPos)) {
                GridCell cell = GridController.Instance.CurrentGrid.GetCellAtPosition(checkPos);
                if (cell != null && cell.CurrentType == CellType.Slime || cell.CurrentType == CellType.Goblin || cell.CurrentType == CellType.Orc) {
                    adjacentMonsters.Add(cell);
                }
            }
        }
        return adjacentMonsters;
    }

    private void UpdateSoldierFacingDirection() {
        List<GridCell> adjacentMonsters = GetAdjacentMonsters(gridPosition);

        if (adjacentMonsters.Count > 0) {
            // Escolhe o primeiro monstro na lista como referência
            Vector2Int targetPosition = adjacentMonsters[0].gridPosition;
            FaceDirection faceDirection =  DetermineFaceDirection(gridPosition, targetPosition);
            CurrentFaceDirection = faceDirection;
            FaceDirectionAnimation(faceDirection);
        }
    }

    private void UpdateMonsterFacingDirections() {
        List<GridCell> nearbyMonsters = GetAdjacentMonsters(gridPosition);

        foreach (GridCell monsterCell in nearbyMonsters) {
            //monsterCell.SetFaceDirection(DetermineFaceDirection(soldierCell.GridPosition));
            FaceDirection faceDirection = DetermineFaceDirection(monsterCell.gridPosition, gridPosition);
            monsterCell.FaceDirectionAnimation(faceDirection);
            monsterCell.CurrentFaceDirection = faceDirection;
        }
    }

    private void FaceDirectionAnimation(FaceDirection faceDirection) {
        Animator animator = transform.Find(CurrentType.ToString()).GetComponent<Animator>();

        animator.ResetTrigger("Up");
        animator.ResetTrigger("Down");
        animator.ResetTrigger("Left");
        animator.ResetTrigger("Right");

        Debug.Log("Face direction: " + faceDirection);

        animator.SetTrigger(faceDirection.ToString());
    }
}
