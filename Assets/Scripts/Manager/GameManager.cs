using UnityEngine;

public class GameManager : MonoBehaviour
{
    static public GameManager Instance;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    public void ValidateAttack() {
        bool allValid = true;

        foreach (var monster in GridController.Instance.CurrentLevel.monsterPlacements) {
            int required = GridController.Instance.GetRequiredSoldiers(monster.monsterType);
            int current = CountAdjacentSoldiers(monster.gridPosition);

            Debug.Log($"Monster {monster.monsterType} at {monster.gridPosition} requires {required} soldiers, but has {current}");
            if (current < required) {
                allValid = false;
            }
        }

        if (allValid && GridController.Instance.SoldierPositions.Count == GridController.Instance.CurrentLevel.maxSoldiers) {
            WinGame();
        } else {
            FailGame();
        }
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
    }

    private void FailGame() {
        Debug.Log("You lose!");
    }
}
