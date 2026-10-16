using MonstersAndSoldiers.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    public static UIManager Instance;

    [SerializeField] private TextMeshProUGUI soldiersRemainingText;
    [SerializeField] private Button attackButton;

    private BoardModel board;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        if (GridController.Instance == null) return;
        GridController.Instance.BoardChanged += Bind;
        Bind(GridController.Instance.Board);
    }

    private void OnDestroy() {
        if (GridController.Instance != null) GridController.Instance.BoardChanged -= Bind;
        Bind(null);
        if (Instance == this) Instance = null;
    }

    public void OnAttackButtonClick() {
        GameManager.Instance.OnAttackButtonPressed();
    }

    private void Bind(BoardModel newBoard) {
        if (board != null) board.CellChanged -= OnCellChanged;
        board = newBoard;
        if (board != null) board.CellChanged += OnCellChanged;
        RefreshSoldierCount();
    }

    private void OnCellChanged(GridPos pos, CellState state) {
        RefreshSoldierCount();
    }

    // HUD da GDD 8.3: "soldados colocados / total".
    private void RefreshSoldierCount() {
        if (soldiersRemainingText == null) return;
        soldiersRemainingText.text = board != null ? $"{board.SoldierCount} / {board.RequiredSoldierTotal}" : "";
    }
}
