// Scripts/Managers/LevelManager.cs
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour {
    public static LevelManager Instance;

    [SerializeField] private LevelData[] allLevels;
    private int currentLevelIndex = 0;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        if (allLevels.Length > 0) {
            GridController.Instance.GenerateGrid(GetCurrentLevel());
        }
    }
    public LevelData GetCurrentLevel() {
        return allLevels[currentLevelIndex];
    }

    public void LoadLevel(int level) {
        SceneManager.LoadScene(level);
    }

    public void LoadNextLevel(int level) {   
        SceneManager.LoadScene(level);
    }

    public void LoadNextGrid() {
        currentLevelIndex = (currentLevelIndex + 1) % allLevels.Length;
        GridController.Instance.TransitionToNextGrid(GetCurrentLevel());
    }

    public void ExitGame() {
        #if UNITY_EDITOR
            EditorApplication.isPlaying = false; // Para o Play Mode no Editor
        #else
            Application.Quit(); // Funciona no build final
        #endif
    }

    public void ReloadCurrentLevel() {
        // Implemente recarregamento
    }
}