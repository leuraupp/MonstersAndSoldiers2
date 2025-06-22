using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameManager : MonoBehaviour
{
    static public GameManager Instance;

    [Header("Feedback Settings")]
    [SerializeField] private float successDelay = 1f;

    private bool isAnimating = false;
    private int pendingAnimations = 0;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    public void OnAttackButtonPressed() {
        if (isAnimating) return;

        ValidationResult result = ValidateAttack();
        isAnimating = true;
        pendingAnimations = 0;

        // Conta quantas células precisam animar
        var cells = GridController.Instance.CurrentGrid.GetNonEmptyCells();
        pendingAnimations = cells.Count;

        // Registra o evento de fim de animação para cada célula
        foreach (var cell in cells) {
            cell.AnimationSwitcher.OnAttackAnimationComplete += () => OnCellAnimationComplete(result.IsValid);
            cell.AnimationSwitcher.AnimationPlay("Attack", cell.CurrentType.ToString(), cell.CurrentFaceDirection.ToString(), result.IsValid);
        }

        if (!result.IsValid) {
            UIManager.Instance.ShowValidationResults(result.invalidSoldiers, result.unsatisfiedMonsters, result.soldierLimitExceeded);
        }
    }

    private void OnCellAnimationComplete(bool wasSuccessful) {
        pendingAnimations--;
        
        if (pendingAnimations <= 0) {
            isAnimating = false;
            if (wasSuccessful) {
                StartCoroutine(DelayedWin());
            }
        }
    }

    private System.Collections.IEnumerator DelayedWin() {
        yield return new WaitForSeconds(successDelay);
        WinGame();
    }

    private ValidationResult ValidateAttack() {
        var result = new ValidationResult();
        result.invalidSoldiers = new HashSet<Vector2Int>();
        result.unsatisfiedMonsters = new HashSet<Vector2Int>();

        var soldierPositions = GridController.Instance.SoldierPositions;

        foreach (var soldier in soldierPositions) {
            foreach (var dir in allDirections) {
                if (soldierPositions.Contains(soldier + dir)) {
                    result.invalidSoldiers.Add(soldier);
                    result.invalidSoldiers.Add(soldier + dir);
                }
            }
        }

        var monsterPlacements = GridController.Instance.CurrentLevel.monsterPlacements;

        foreach (var monster in monsterPlacements) {
            int count = CountAdjacentSoldiers(monster.gridPosition);
            if (count < GridController.Instance.GetRequiredSoldiers(monster.monsterType)) {
                result.unsatisfiedMonsters.Add(monster.gridPosition);
            }
        }

        result.soldierLimitExceeded = soldierPositions.Count > GridController.Instance.CurrentLevel.maxSoldiers;

        return result;
    }

    private int CountAdjacentSoldiers(Vector2Int center) {
        int count = 0;
        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                if (x == 0 && y == 0)
                    continue; // Ignora o próprio centro
                Vector2Int checkPos = new Vector2Int(center.x + x, center.y + y);
                if (GridController.Instance.SoldierPositions.Contains(checkPos))
                    count++;
            }
        }
        return count;
    }

    private void WinGame() {
        Debug.Log("You win!");
        LevelManager.Instance.LoadNextGrid();
    }

    private void FailGame() {
        Debug.Log("You lose!");
    }

    private readonly Vector2Int[] allDirections = {
    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
    new Vector2Int(1,1), new Vector2Int(-1,-1), new Vector2Int(1,-1), new Vector2Int(-1,1)
};

    public struct ValidationResult {
        public HashSet<Vector2Int> invalidSoldiers;
        public HashSet<Vector2Int> unsatisfiedMonsters;
        public bool soldierLimitExceeded;
        public bool IsValid => invalidSoldiers != null && invalidSoldiers.Count == 0 &&
                              unsatisfiedMonsters != null && unsatisfiedMonsters.Count == 0 &&
                              !soldierLimitExceeded;
    }
}
