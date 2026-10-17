using System;
using System.Collections;
using MonstersAndSoldiers.Core;
using UnityEngine;

/// <summary>
/// Fluxo da partida: recebe o pedido de ataque, valida o tabuleiro e decide entre vitória e nova tentativa.
/// Tempo, tentativas e pausa ficam no MatchSession (C# puro); aqui só se liga isso à Unity.
/// </summary>
public class GameManager : MonoBehaviour {
    public static GameManager Instance;

    [Header("Feedback Settings")]
    [SerializeField] private float successDelay = 1f;

    private InputController input;
    private bool isAnimating;

    public MatchSession Session { get; } = new MatchSession();
    public BestTimes BestTimes { get; private set; }

    /// <summary>Tentativas de ataque no nível atual (mostradas na tela de vitória).</summary>
    public int AttackAttempts => Session.Attempts;

    /// <summary>True enquanto a animação de ataque (ou a espera da vitória) está rodando.</summary>
    public bool IsAnimating => isAnimating;

    /// <summary>Recorde batido na última vitória (GDD 8.4).</summary>
    public bool LastWinWasRecord { get; private set; }

    /// <summary>Disparado quando o ataque acerta, já com o recorde registrado.</summary>
    public event Action Won;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }

        input = FindAnyObjectByType<InputController>();
        if (input == null) input = gameObject.AddComponent<InputController>();
        BestTimes = new BestTimes(new PlayerPrefsStore());
        Session.StateChanged += OnSessionStateChanged;
    }

    private void OnEnable() {
        if (input != null) {
            input.AttackPressed += OnAttackButtonPressed;
            input.PausePressed += TogglePause;
        }
    }

    private void Start() {
        if (GridController.Instance != null) GridController.Instance.BoardChanged += OnBoardChanged;
    }

    private void Update() {
        // unscaledDeltaTime: a pausa zera o timeScale, e o relógio não pode depender disso.
        Session.Tick(Time.unscaledDeltaTime);
    }

    private void OnDisable() {
        if (input != null) {
            input.AttackPressed -= OnAttackButtonPressed;
            input.PausePressed -= TogglePause;
        }
    }

    private void OnDestroy() {
        Session.StateChanged -= OnSessionStateChanged;
        if (GridController.Instance != null) GridController.Instance.BoardChanged -= OnBoardChanged;
        if (Instance == this) {
            Instance = null;
            Time.timeScale = 1f;
        }
    }

    private void OnBoardChanged(BoardModel board) {
        isAnimating = false;
        LastWinWasRecord = false;
        Session.Reset();
        SetInputEnabled(true);
    }

    public void TogglePause() => Session.TogglePause();
    public void Pause() => Session.Pause();
    public void Resume() => Session.Resume();

    // A pausa congela animações e DOTween pelo timeScale; o tabuleiro só aceita input jogando.
    private void OnSessionStateChanged(MatchState state) {
        Time.timeScale = state == MatchState.Paused ? 0f : 1f;
        SetInputEnabled(state == MatchState.Playing);
    }

    /// <summary>Chamado pelo botão Atacar da UI e pelo atalho de teclado/controle.</summary>
    public void OnAttackButtonPressed() {
        GridController grid = GridController.Instance;
        if (isAnimating || grid == null || grid.Board == null || grid.IsTransitioning) return;
        if (!Session.BeginAttack()) return;

        ValidationResult result = RuleValidator.Validate(grid.Board);
        isAnimating = true;

        if (!result.IsValid) {
            grid.CurrentGrid.ShowValidationErrors(result);
        }

        grid.CurrentGrid.PlayAttack(result, () => OnAttackFinished(result.IsValid));
    }

    private void OnAttackFinished(bool success) {
        Session.EndAttack(success);
        if (success) {
            LastWinWasRecord = BestTimes.Submit(CurrentModeKey(), Session.ElapsedSeconds);
            Won?.Invoke();
            StartCoroutine(DelayedWin());
        } else {
            isAnimating = false;
        }
    }

    /// <summary>Chave do recorde do modo atual: uma por dificuldade e uma para o tutorial.</summary>
    public static string CurrentModeKey() {
        LevelManager levels = LevelManager.Instance;
        if (levels == null || levels.Mode != LevelManager.LevelMode.Procedural) return BestTimes.TutorialKey;
        return BestTimes.KeyFor(levels.CurrentDifficulty);
    }

    private IEnumerator DelayedWin() {
        yield return new WaitForSeconds(successDelay);
        Debug.Log($"Nível concluído em {MatchSession.FormatTime(Session.ElapsedSeconds)}, " +
                  $"{AttackAttempts} tentativa(s){(LastWinWasRecord ? ", recorde" : "")}.");
        LevelManager.Instance.LoadNextGrid();
    }

    private void SetInputEnabled(bool enabled) {
        if (input != null) input.BoardInputEnabled = enabled;
    }
}
