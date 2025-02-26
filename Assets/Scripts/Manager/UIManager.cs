using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    static public UIManager Instance;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    [SerializeField] private TextMeshProUGUI soldiersRemainingText;
    [SerializeField] private Button attackButton;

    void Update() {
        soldiersRemainingText.text = $"{GridController.Instance.RemainingSoldiers} / {GridController.Instance.CurrentLevel.maxSoldiers}";
    }

    public void OnAttackButtonClick() {
        GameManager.Instance.OnAttackButtonPressed();
    }

    public void ShowValidationResults(HashSet<Vector2Int> invalidSoldiers, HashSet<Vector2Int> unsatisfiedMonsters, bool soldierLimitExceeded) {
        if (invalidSoldiers.Count > 0) {
            GridController.Instance.HighlightInvalidSoldiers(invalidSoldiers);
        }

        if (unsatisfiedMonsters.Count > 0) {
            GridController.Instance.HighlightUnsatisfiedMonsters(unsatisfiedMonsters);
        }

        if (soldierLimitExceeded) {
            GridController.Instance.ShowSoldierLimitExceededMessage();
        }
    }
}
