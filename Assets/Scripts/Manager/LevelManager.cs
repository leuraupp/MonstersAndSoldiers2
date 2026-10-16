// Scripts/Managers/LevelManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelManager : MonoBehaviour {
    public static LevelManager Instance;

    [SerializeField] private LevelData[] allLevels;
    private int currentLevelIndex = 0;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    private void Start() {
        if (allLevels != null && allLevels.Length > 0 && GridController.Instance != null) {
            GridController.Instance.GenerateGrid(GetCurrentLevel());
        }
    }

    public LevelData GetCurrentLevel() {
        return allLevels[currentLevelIndex];
    }

    /// <summary>Carrega uma cena pelo índice do Build Profile (usado pelos botões do menu).</summary>
    public void LoadLevel(int sceneIndex) {
        SceneManager.LoadScene(sceneIndex);
    }

    public void LoadNextLevel(int sceneIndex) {
        LoadLevel(sceneIndex);
    }

    public void LoadNextGrid() {
        currentLevelIndex = (currentLevelIndex + 1) % allLevels.Length;
        GridController.Instance.TransitionToNextGrid(GetCurrentLevel());
    }

    /// <summary>Recomeça o nível atual do zero (mesmo mapa).</summary>
    public void ReloadCurrentLevel() {
        GridController.Instance.GenerateGrid(GetCurrentLevel());
    }

    public void ExitGame() {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false; // Para o Play Mode no Editor
#else
        Application.Quit(); // Funciona no build final
#endif
    }
}
