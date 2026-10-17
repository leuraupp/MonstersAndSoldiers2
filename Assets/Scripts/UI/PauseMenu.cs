using MonstersAndSoldiers.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Pausa (GDD 8.4): Continuar, Reiniciar (mesma seed), Opções e Sair para o menu.
/// Abre e fecha seguindo o estado do MatchSession, então ESC, Start e o botão da HUD dão no mesmo.
/// </summary>
public class PauseMenu : MonoBehaviour {
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private OptionsPanel optionsPanel;

    private MatchSession session;

    private void Awake() {
        panel.SetActive(false);
    }

    private void Start() {
        resumeButton.onClick.AddListener(Resume);
        restartButton.onClick.AddListener(Restart);
        optionsButton.onClick.AddListener(OpenOptions);
        menuButton.onClick.AddListener(GoToMenu);

        if (GameManager.Instance == null) return;
        session = GameManager.Instance.Session;
        session.StateChanged += OnStateChanged;
    }

    private void OnDestroy() {
        if (session != null) session.StateChanged -= OnStateChanged;
    }

    private void OnStateChanged(MatchState state) {
        bool paused = state == MatchState.Paused;
        panel.SetActive(paused);
        if (!paused) {
            if (optionsPanel != null) optionsPanel.Close();
            return;
        }

        int seed = LevelManager.Instance != null ? LevelManager.Instance.CurrentSeed : 0;
        if (infoText != null) {
            infoText.text = UIManager.ModeLabel() + (seed != 0 ? $"\nSeed {seed}" : "")
                          + $"\n{MatchSession.FormatTime(session.ElapsedSeconds)}";
        }
        Select(resumeButton);
    }

    public void Resume() => GameManager.Instance.Resume();

    public void Restart() {
        GameManager.Instance.Resume();
        LevelManager.Instance.ReloadCurrentLevel();
    }

    public void OpenOptions() {
        if (optionsPanel == null) return;
        optionsPanel.Open(() => Select(optionsButton));
    }

    public void GoToMenu() {
        Time.timeScale = 1f;
        LevelManager.Instance.LoadMainMenu();
    }

    // Seleção inicial para o controle conseguir navegar (GDD 4.2).
    private static void Select(Selectable selectable) {
        if (selectable != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }
}
