using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Balão do tutorial (GDD 6.1): mostra a regra nova de cada nível feito à mão até o jogador fechar.
/// </summary>
public class TutorialHint : MonoBehaviour {
    [SerializeField] private GameObject balloon;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Button closeButton;

    public bool IsOpen => balloon.activeSelf;

    private void Awake() {
        balloon.SetActive(false);
    }

    private void Start() {
        closeButton.onClick.AddListener(Close);
        if (GridController.Instance == null) return;
        GridController.Instance.BoardChanged += OnBoardChanged;
        if (GridController.Instance.Board != null) OnBoardChanged(null);
    }

    private void OnDestroy() {
        if (GridController.Instance != null) GridController.Instance.BoardChanged -= OnBoardChanged;
    }

    private void OnBoardChanged(MonstersAndSoldiers.Core.BoardModel board) {
        LevelData level = GridController.Instance.CurrentLevel;
        bool show = level != null && !string.IsNullOrWhiteSpace(level.tutorialHint);
        if (show) text.text = level.tutorialHint;
        balloon.SetActive(show);
    }

    public void Close() => balloon.SetActive(false);
}
