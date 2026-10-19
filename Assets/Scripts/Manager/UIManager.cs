using MonstersAndSoldiers.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD da partida (GDD 8.3): modo e seed, tempo e os botões Atacar e Pausa.
/// A contagem de soldados fica nas pistas das bordas do tabuleiro.
/// </summary>
public class UIManager : MonoBehaviour {
    public static UIManager Instance;

    [SerializeField] private Button attackButton;

    [Header("Fase 3")]
    [SerializeField] private TextMeshProUGUI modeText;
    [SerializeField] private TextMeshProUGUI seedText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Button pauseButton;

    private int shownSeconds = -1;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseButtonClick);
        if (GridController.Instance == null) return;
        GridController.Instance.BoardChanged += Bind;
        Bind(GridController.Instance.Board);
    }

    private void Update() {
        if (timerText == null || GameManager.Instance == null) return;
        int seconds = (int)GameManager.Instance.Session.ElapsedSeconds;
        if (seconds == shownSeconds) return;
        shownSeconds = seconds;
        timerText.text = MatchSession.FormatTime(seconds);
    }

    private void OnDestroy() {
        if (GridController.Instance != null) GridController.Instance.BoardChanged -= Bind;
        if (Instance == this) Instance = null;
    }

    public void OnAttackButtonClick() {
        GameManager.Instance.OnAttackButtonPressed();
    }

    public void OnPauseButtonClick() {
        if (GameManager.Instance != null) GameManager.Instance.Pause();
    }

    private void Bind(BoardModel newBoard) {
        RefreshModeAndSeed();
    }

    private void RefreshModeAndSeed() {
        if (modeText != null) modeText.text = ModeLabel();
        if (seedText != null) {
            int seed = LevelManager.Instance != null ? LevelManager.Instance.CurrentSeed : 0;
            seedText.text = seed != 0 ? $"Seed {seed}" : "";
        }
    }

    /// <summary>"Fácil", "Médio", "Difícil" ou "Tutorial 2/4".</summary>
    public static string ModeLabel() {
        LevelManager levels = LevelManager.Instance;
        if (levels == null) return "";
        if (levels.Mode == LevelManager.LevelMode.Procedural) return DifficultyLabel(levels.CurrentDifficulty);
        return $"Tutorial {levels.CurrentLevelNumber}/{levels.LevelCount}";
    }

    public static string DifficultyLabel(Difficulty difficulty) {
        return difficulty switch {
            Difficulty.Easy => "Fácil",
            Difficulty.Medium => "Médio",
            Difficulty.Hard => "Difícil",
            _ => difficulty.ToString()
        };
    }
}
