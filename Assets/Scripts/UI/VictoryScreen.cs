using MonstersAndSoldiers.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Tela de vitória (GDD 8.4): tempo, tentativas, seed e aviso de recorde.</summary>
public class VictoryScreen : MonoBehaviour {
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private GameObject recordBadge;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button menuButton;

    public bool IsOpen => panel.activeSelf;

    private void Awake() {
        panel.SetActive(false);
    }

    private void Start() {
        nextButton.onClick.AddListener(Next);
        menuButton.onClick.AddListener(GoToMenu);
    }

    public void Show() {
        GameManager game = GameManager.Instance;
        int seed = LevelManager.Instance != null ? LevelManager.Instance.CurrentSeed : 0;
        statsText.text = $"{UIManager.ModeLabel()}\n"
                       + $"Tempo  {MatchSession.FormatTime(game.Session.ElapsedSeconds)}\n"
                       + $"Tentativas  {game.AttackAttempts}"
                       + (seed != 0 ? $"\nSeed  {seed}" : "");
        if (recordBadge != null) recordBadge.SetActive(game.LastWinWasRecord);
        panel.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(nextButton.gameObject);
    }

    public void Next() {
        panel.SetActive(false);
        LevelManager.Instance.LoadNextGrid();
    }

    public void GoToMenu() {
        LevelManager.Instance.LoadMainMenu();
    }
}
