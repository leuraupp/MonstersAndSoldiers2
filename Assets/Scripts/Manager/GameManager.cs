using System.Collections;
using MonstersAndSoldiers.Core;
using UnityEngine;

/// <summary>
/// Fluxo da partida: recebe o pedido de ataque, valida o tabuleiro e decide entre vitória e nova tentativa.
/// </summary>
public class GameManager : MonoBehaviour {
    public static GameManager Instance;

    [Header("Feedback Settings")]
    [SerializeField] private float successDelay = 1f;

    private InputController input;
    private bool isAnimating;

    /// <summary>Tentativas de ataque no nível atual (mostradas na tela de vitória na Fase 3).</summary>
    public int AttackAttempts { get; private set; }

    /// <summary>True enquanto a animação de ataque (ou a espera da vitória) está rodando.</summary>
    public bool IsAnimating => isAnimating;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }

        input = FindAnyObjectByType<InputController>();
        if (input == null) input = gameObject.AddComponent<InputController>();
    }

    private void OnEnable() {
        if (input != null) input.AttackPressed += OnAttackButtonPressed;
    }

    private void Start() {
        if (GridController.Instance != null) GridController.Instance.BoardChanged += OnBoardChanged;
    }

    private void OnDisable() {
        if (input != null) input.AttackPressed -= OnAttackButtonPressed;
    }

    private void OnDestroy() {
        if (GridController.Instance != null) GridController.Instance.BoardChanged -= OnBoardChanged;
        if (Instance == this) Instance = null;
    }

    private void OnBoardChanged(BoardModel board) {
        AttackAttempts = 0;
        isAnimating = false;
        SetInputEnabled(true);
    }

    /// <summary>Chamado pelo botão Atacar da UI e pelo atalho de teclado/controle.</summary>
    public void OnAttackButtonPressed() {
        GridController grid = GridController.Instance;
        if (isAnimating || grid == null || grid.Board == null || grid.IsTransitioning) return;

        ValidationResult result = RuleValidator.Validate(grid.Board);
        AttackAttempts++;
        isAnimating = true;
        SetInputEnabled(false);

        if (!result.IsValid) {
            grid.CurrentGrid.ShowValidationErrors(result);
        }

        grid.CurrentGrid.PlayAttack(result, () => OnAttackFinished(result.IsValid));
    }

    private void OnAttackFinished(bool success) {
        if (success) {
            StartCoroutine(DelayedWin());
        } else {
            isAnimating = false;
            SetInputEnabled(true);
        }
    }

    private IEnumerator DelayedWin() {
        yield return new WaitForSeconds(successDelay);
        Debug.Log($"Nível concluído em {AttackAttempts} tentativa(s).");
        LevelManager.Instance.LoadNextGrid();
    }

    private void SetInputEnabled(bool enabled) {
        if (input != null) input.BoardInputEnabled = enabled;
    }
}
