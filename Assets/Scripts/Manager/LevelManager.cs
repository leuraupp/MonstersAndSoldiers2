// Scripts/Managers/LevelManager.cs
using UnityEngine;

public class LevelManager : MonoBehaviour {
    public static LevelManager Instance;

    [SerializeField] private LevelData[] allLevels;
    private int currentLevelIndex = 0;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    void Start() {
        GridController.Instance.GenerateGrid(GetCurrentLevel());
    }

    public LevelData GetCurrentLevel() {
        return allLevels[currentLevelIndex];
    }

    public void LoadNextLevel() {
        currentLevelIndex = (currentLevelIndex + 1) % allLevels.Length;
        // Implemente a lógica de carregamento aqui
    }

    public void ReloadCurrentLevel() {
        // Implemente recarregamento
    }
}