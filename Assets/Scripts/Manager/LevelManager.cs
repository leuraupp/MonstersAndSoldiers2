// Scripts/Managers/LevelManager.cs
using MonstersAndSoldiers.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelManager : MonoBehaviour {
    public static LevelManager Instance;

    public enum LevelMode {
        HandMade,
        Procedural
    }

    [SerializeField] private LevelData[] allLevels;

    [Header("Modo procedural (GDD 6.2)")]
    [SerializeField] private LevelMode mode = LevelMode.HandMade;
    [SerializeField] private Difficulty difficulty = Difficulty.Easy;
    [Tooltip("Seed do primeiro mapa. 0 = sorteia.")]
    [SerializeField] private int startSeed = 0;

    private int currentLevelIndex = 0;

    public LevelMode Mode => mode;
    public Difficulty CurrentDifficulty => difficulty;

    /// <summary>Número (a partir de 1) do nível feito à mão em jogo.</summary>
    public int CurrentLevelNumber => currentLevelIndex + 1;
    public int LevelCount => allLevels != null ? allLevels.Length : 0;

    /// <summary>True no último nível do tutorial: o próximo passo é o modo Fácil.</summary>
    public bool IsLastHandMadeLevel => mode == LevelMode.HandMade && currentLevelIndex >= LevelCount - 1;

    /// <summary>Mapa procedural em jogo. Null nos níveis feitos à mão.</summary>
    public GeneratedPuzzle CurrentPuzzle { get; private set; }

    /// <summary>Seed do mapa atual (exibida na pausa e na vitória na Fase 3). 0 nos níveis feitos à mão.</summary>
    public int CurrentSeed => CurrentPuzzle != null ? CurrentPuzzle.Seed : 0;

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
        if (GridController.Instance == null) return;

        // Pedido vindo do menu (GDD 8.2) tem prioridade sobre o modo configurado na cena.
        if (MatchRequest.TryConsume(out bool tutorial, out Difficulty requested)) {
            mode = tutorial ? LevelMode.HandMade : LevelMode.Procedural;
            difficulty = requested;
            startSeed = 0;
            currentLevelIndex = 0;
        }

        if (mode == LevelMode.Procedural) {
            StartProcedural(difficulty, startSeed != 0 ? startSeed : NewSeed());
        } else if (allLevels != null && allLevels.Length > 0) {
            GridController.Instance.GenerateGrid(GetCurrentLevel());
        }
    }

    public LevelData GetCurrentLevel() {
        return allLevels[currentLevelIndex];
    }

    /// <summary>Entra no modo procedural com um mapa da seed informada.</summary>
    public void StartProcedural(Difficulty newDifficulty, int seed) {
        mode = LevelMode.Procedural;
        difficulty = newDifficulty;
        CurrentPuzzle = Generate(seed);
        GridController.Instance.GenerateGrid(CurrentPuzzle.CreateBoard());
    }

    /// <summary>Volta para os níveis feitos à mão, a partir do índice informado.</summary>
    public void StartHandMade(int levelIndex = 0) {
        mode = LevelMode.HandMade;
        CurrentPuzzle = null;
        currentLevelIndex = Mathf.Clamp(levelIndex, 0, allLevels.Length - 1);
        GridController.Instance.GenerateGrid(GetCurrentLevel());
    }

    /// <summary>Carrega uma cena pelo índice do Build Profile (usado pelos botões do menu).</summary>
    public void LoadLevel(int sceneIndex) {
        SceneManager.LoadScene(sceneIndex);
    }

    public void LoadNextLevel(int sceneIndex) {
        LoadLevel(sceneIndex);
    }

    // Índices do Build Profile: 0 = MainMenu, 1 = partida.
    public const int MainMenuScene = 0;
    public const int MatchScene = 1;

    public void LoadMainMenu() {
        Time.timeScale = 1f;
        LoadLevel(MainMenuScene);
    }

    /// <summary>Próximo nível; no modo procedural, novo mapa com nova seed na mesma dificuldade.</summary>
    public void LoadNextGrid() {
        if (mode == LevelMode.Procedural) {
            CurrentPuzzle = Generate(NewSeed());
            GridController.Instance.TransitionToNextGrid(CurrentPuzzle.CreateBoard());
            return;
        }

        // Fim do tutorial: segue para o modo Fácil.
        if (IsLastHandMadeLevel) {
            mode = LevelMode.Procedural;
            difficulty = Difficulty.Easy;
            LoadNextGrid();
            return;
        }

        currentLevelIndex++;
        GridController.Instance.TransitionToNextGrid(GetCurrentLevel());
    }

    /// <summary>Recomeça o nível atual do zero (mesmo mapa; no procedural, mesma seed).</summary>
    public void ReloadCurrentLevel() {
        if (mode == LevelMode.Procedural && CurrentPuzzle != null) {
            GridController.Instance.GenerateGrid(CurrentPuzzle.CreateBoard());
            return;
        }
        GridController.Instance.GenerateGrid(GetCurrentLevel());
    }

    private GeneratedPuzzle Generate(int seed) {
        GeneratedPuzzle puzzle = PuzzleGenerator.Generate(difficulty, seed);
        Debug.Log($"Mapa {difficulty} seed {seed} ({puzzle.Attempts} tentativa(s){(puzzle.IsUnique ? "" : ", sem solução única")}).");
        return puzzle;
    }

    // Seeds de até 6 dígitos ficam fáceis de ler e digitar.
    private static int NewSeed() => Random.Range(1, 1000000);

    public void ExitGame() {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false; // Para o Play Mode no Editor
#else
        Application.Quit(); // Funciona no build final
#endif
    }
}
