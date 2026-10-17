using MonstersAndSoldiers.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu principal e seleção de modo (GDD 8.1 e 8.2): Jogar abre Tutorial, Fácil, Médio e Difícil,
/// cada um com o melhor tempo salvo. Sair some no WebGL.
/// </summary>
public class MainMenuController : MonoBehaviour {
    [Header("Menu principal")]
    [SerializeField] private GameObject mainButtons;
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button exitButton;

    [Header("Seleção de modo")]
    [SerializeField] private GameObject modePanel;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button easyButton;
    [SerializeField] private Button mediumButton;
    [SerializeField] private Button hardButton;
    [SerializeField] private Button modeBackButton;

    [Header("Outros painéis")]
    [SerializeField] private OptionsPanel optionsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private Button creditsBackButton;

    private void Start() {
        // O Exit da cena já chama LevelManager.ExitGame pelo inspector; aqui só some no navegador.
        if (exitButton != null && Application.platform == RuntimePlatform.WebGLPlayer) exitButton.gameObject.SetActive(false);

        if (playButton != null) {
            playButton.onClick.AddListener(OpenModeSelect);
        }
        if (optionsButton != null) optionsButton.onClick.AddListener(OpenOptions);
        if (creditsButton != null) creditsButton.onClick.AddListener(OpenCredits);
        if (creditsBackButton != null) creditsBackButton.onClick.AddListener(ShowMain);

        tutorialButton.onClick.AddListener(() => { MatchRequest.RequestTutorial(); LoadMatch(); });
        easyButton.onClick.AddListener(() => StartProcedural(Difficulty.Easy));
        mediumButton.onClick.AddListener(() => StartProcedural(Difficulty.Medium));
        hardButton.onClick.AddListener(() => StartProcedural(Difficulty.Hard));
        modeBackButton.onClick.AddListener(ShowMain);

        ShowMain();
    }

    public void ShowMain() {
        modePanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        mainButtons.SetActive(true);
        Select(playButton);
    }

    public void OpenModeSelect() {
        var best = new BestTimes(new PlayerPrefsStore());
        SetBestLabel(tutorialButton, best.Get(BestTimes.TutorialKey));
        SetBestLabel(easyButton, best.Get(BestTimes.KeyFor(Difficulty.Easy)));
        SetBestLabel(mediumButton, best.Get(BestTimes.KeyFor(Difficulty.Medium)));
        SetBestLabel(hardButton, best.Get(BestTimes.KeyFor(Difficulty.Hard)));

        mainButtons.SetActive(false);
        modePanel.SetActive(true);
        Select(easyButton);
    }

    public void OpenOptions() {
        if (optionsPanel == null) return;
        mainButtons.SetActive(false);
        optionsPanel.Open(() => { mainButtons.SetActive(true); Select(optionsButton); });
    }

    public void OpenCredits() {
        if (creditsPanel == null) return;
        mainButtons.SetActive(false);
        creditsPanel.SetActive(true);
        Select(creditsBackButton);
    }

    private static void StartProcedural(Difficulty difficulty) {
        MatchRequest.RequestProcedural(difficulty);
        LoadMatch();
    }

    private static void LoadMatch() => SceneManager.LoadScene(LevelManager.MatchScene);

    // O melhor tempo fica num TMP filho chamado "Best" (GDD 8.2).
    private static void SetBestLabel(Button button, double? seconds) {
        Transform t = button.transform.Find("Best");
        if (t == null || !t.TryGetComponent(out TMP_Text text)) return;
        text.text = seconds.HasValue ? $"Recorde {MatchSession.FormatTime(seconds.Value)}" : "";
    }

    private static void Select(Selectable selectable) {
        if (selectable != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }
}
