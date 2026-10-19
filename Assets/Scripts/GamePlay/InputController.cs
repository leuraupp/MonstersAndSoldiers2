using System;
using System.Collections.Generic;
using MonstersAndSoldiers.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Único ponto de entrada de input da partida (GDD 4.2 e 4.3): mouse, teclado e controle.
/// Traduz input em ações no <see cref="BoardModel"/> da grid atual.
/// </summary>
public class InputController : MonoBehaviour {
    [Header("Gamepad cursor")]
    [SerializeField] private float stickDeadZone = 0.5f;
    [SerializeField] private float repeatDelay = 0.35f;
    [SerializeField] private float repeatInterval = 0.12f;

    /// <summary>Atacar (Espaço, Enter, Y / Triângulo).</summary>
    public event Action AttackPressed;

    /// <summary>Pausa (ESC, Start / Options). O menu de pausa chega na Fase 3.</summary>
    public event Action PausePressed;

    /// <summary>Quando false, nenhuma ação chega ao tabuleiro (transições e animações).</summary>
    public bool BoardInputEnabled { get; set; } = true;

    /// <summary>Último tipo de dispositivo usado (GDD 8.3), também fora do tabuleiro.</summary>
    public ControlScheme Scheme { get; private set; } = ControlScheme.KeyboardMouse;

    /// <summary>Disparado quando o jogador troca entre mouse/teclado e controle.</summary>
    public event Action<ControlScheme> SchemeChanged;

    private InputAction pointAction;
    private InputAction primaryAction;
    private InputAction secondaryAction;
    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction pauseAction;

    private Camera cam;
    private GridPos? cursor;
    private bool usingPointer = true;

    // Pintura por arrasto: o estado decidido no primeiro clique é aplicado nas casas seguintes.
    private CellState? paintState;
    private GridPos? lastPainted;

    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    private PointerEventData pointerData;
    private EventSystem pointerDataOwner;

    private Vector2Int heldDirection;
    private float nextRepeatTime;

    private void Awake() {
        pointAction = new InputAction("Point", InputActionType.PassThrough, "<Pointer>/position");

        primaryAction = new InputAction("Primary", InputActionType.Button);
        primaryAction.AddBinding("<Mouse>/leftButton");
        primaryAction.AddBinding("<Gamepad>/buttonSouth");

        secondaryAction = new InputAction("Secondary", InputActionType.Button);
        secondaryAction.AddBinding("<Mouse>/rightButton");
        secondaryAction.AddBinding("<Gamepad>/buttonEast");

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddBinding("<Gamepad>/leftStick");
        moveAction.AddBinding("<Gamepad>/dpad");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");

        attackAction = new InputAction("Attack", InputActionType.Button);
        attackAction.AddBinding("<Keyboard>/space");
        attackAction.AddBinding("<Keyboard>/enter");
        attackAction.AddBinding("<Gamepad>/buttonNorth");

        pauseAction = new InputAction("Pause", InputActionType.Button);
        pauseAction.AddBinding("<Keyboard>/escape");
        pauseAction.AddBinding("<Gamepad>/start");
    }

    private void OnEnable() {
        foreach (InputAction a in AllActions()) a.Enable();
    }

    private void OnDisable() {
        foreach (InputAction a in AllActions()) a.Disable();
    }

    private void OnDestroy() {
        foreach (InputAction a in AllActions()) a.Dispose();
    }

    private InputAction[] AllActions() =>
        new[] { pointAction, primaryAction, secondaryAction, moveAction, attackAction, pauseAction };

    private void Update() {
        UpdateScheme();
        if (pauseAction.WasPressedThisFrame()) PausePressed?.Invoke();
        if (attackAction.WasPressedThisFrame() && BoardInputEnabled) AttackPressed?.Invoke();

        GridController controller = GridController.Instance;
        GridView grid = controller != null ? controller.CurrentGrid : null;
        if (grid == null || grid.Board == null || !BoardInputEnabled || controller.IsTransitioning) {
            ResetPaint();
            if (grid != null) grid.SetHighlight(null);
            return;
        }

        UpdateDeviceMode();
        if (usingPointer) UpdatePointerCursor(grid);
        else UpdateDirectionalCursor(grid);

        grid.SetHighlight(cursor);
        HandleActions(grid.Board);
    }

    // A legenda segue o dispositivo de qualquer ação nossa; o mouse conta também só de mexer.
    private void UpdateScheme() {
        InputDevice device = null;
        foreach (InputAction a in AllActions()) {
            if (a == pointAction || !a.WasPerformedThisFrame() || a.activeControl == null) continue;
            device = a.activeControl.device;
            break;
        }
        if (device == null && Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 1f) {
            device = Mouse.current;
        }
        if (device == null) return;

        ControlScheme scheme = device is Gamepad ? ControlScheme.Gamepad : ControlScheme.KeyboardMouse;
        if (scheme == Scheme) return;
        Scheme = scheme;
        SchemeChanged?.Invoke(scheme);
    }

    // O cursor segue o último tipo de dispositivo usado.
    private void UpdateDeviceMode() {
        if (moveAction.WasPerformedThisFrame() && moveAction.activeControl?.device is not Pointer) {
            usingPointer = false;
        } else if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 1f) {
            usingPointer = true;
        }
    }

    private void UpdatePointerCursor(GridView grid) {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector2 screen = pointAction.ReadValue<Vector2>();
        Vector2 world = cam.ScreenToWorldPoint(screen);
        cursor = !IsPointerOverUI(screen) && grid.TryWorldToCell(world, out GridPos pos) ? pos : (GridPos?)null;
    }

    // Só bloqueia quando há UI (Canvas) sob o ponteiro. IsPointerOverGameObject() também conta
    // Physics/Physics2D Raycasters na câmera, que acertariam as próprias casas do tabuleiro.
    private bool IsPointerOverUI(Vector2 screen) {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return false;

        if (pointerDataOwner != eventSystem) {
            pointerData = new PointerEventData(eventSystem);
            pointerDataOwner = eventSystem;
        }
        pointerData.position = screen;

        uiHits.Clear();
        eventSystem.RaycastAll(pointerData, uiHits);
        foreach (RaycastResult hit in uiHits) {
            if (hit.module is GraphicRaycaster) return true;
        }
        return false;
    }

    private void UpdateDirectionalCursor(GridView grid) {
        BoardModel board = grid.Board;
        if (!cursor.HasValue || !board.IsInside(cursor.Value)) {
            cursor = new GridPos(board.Rows - 1, 0);
        }

        Vector2 v = moveAction.ReadValue<Vector2>();
        Vector2Int dir = Vector2Int.zero;
        if (Mathf.Abs(v.x) >= stickDeadZone && Mathf.Abs(v.x) >= Mathf.Abs(v.y)) dir.x = (int)Mathf.Sign(v.x);
        else if (Mathf.Abs(v.y) >= stickDeadZone) dir.y = (int)Mathf.Sign(v.y);

        if (dir == Vector2Int.zero) {
            heldDirection = Vector2Int.zero;
            return;
        }

        bool firstPress = dir != heldDirection;
        if (!firstPress && Time.unscaledTime < nextRepeatTime) return;

        heldDirection = dir;
        nextRepeatTime = Time.unscaledTime + (firstPress ? repeatDelay : repeatInterval);

        var next = new GridPos(
            Mathf.Clamp(cursor.Value.Row + dir.y, 0, board.Rows - 1),
            Mathf.Clamp(cursor.Value.Col + dir.x, 0, board.Cols - 1));
        if (next != cursor.Value) AudioService.Play(Sfx.CursorMove);
        cursor = next;
    }

    private void HandleActions(BoardModel board) {
        bool primaryDown = primaryAction.WasPressedThisFrame();
        bool secondaryDown = secondaryAction.WasPressedThisFrame();

        if (!cursor.HasValue) {
            if (primaryDown || secondaryDown) ResetPaint();
            if (!primaryAction.IsPressed() && !secondaryAction.IsPressed()) ResetPaint();
            return;
        }

        GridPos pos = cursor.Value;

        if (primaryDown || secondaryDown) {
            ResetPaint();
            if (board.IsMonster(pos)) {
                AudioService.Play(Sfx.MonsterClick); // clicar em monstro só faz barulho
                return;
            }
            paintState = primaryDown ? board.NextStateForPrimary(pos) : board.NextStateForSecondary(pos);
            Paint(board, pos);
            return;
        }

        bool held = primaryAction.IsPressed() || secondaryAction.IsPressed();
        if (!held) {
            ResetPaint();
            return;
        }

        // Arrasto: só com o ponteiro, para o controle não pintar ao mover o cursor.
        if (usingPointer && paintState.HasValue && lastPainted != pos) Paint(board, pos);
    }

    private void Paint(BoardModel board, GridPos pos) {
        lastPainted = pos;
        if (paintState.HasValue) board.SetState(pos, paintState.Value);
    }

    private void ResetPaint() {
        paintState = null;
        lastPainted = null;
    }
}
