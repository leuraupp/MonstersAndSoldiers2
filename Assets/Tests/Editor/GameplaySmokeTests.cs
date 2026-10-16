using System.Collections;
using MonstersAndSoldiers.Core;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Smoke test da partida: carrega a cena de teste, erra, acerta e confere a transição de nível.
/// Fica em Assembly-CSharp-Editor (sem asmdef) para enxergar os scripts de gameplay.
/// Qualquer erro ou exceção logado durante o teste faz ele falhar.
/// </summary>
public class GameplaySmokeTests {
    private const string ScenePath = "Assets/Scenes/Gameplay/LevelTest/level_01_test.unity";

    [UnityTest]
    public IEnumerator WrongAttack_ThenSolve_AdvancesToNextLevel() {
        yield return new EnterPlayMode();

        EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        float deadline = Time.realtimeSinceStartup + 5f;
        while (GridController.Instance == null || GridController.Instance.Board == null) {
            Assert.Less(Time.realtimeSinceStartup, deadline, "Cena não carregou o tabuleiro.");
            yield return null;
        }

        // Esperas em tempo real: em testes EditMode, WaitForSeconds e IEnumerators aninhados não esperam de verdade.
        GridController grid = GridController.Instance;
        LevelData firstLevel = grid.CurrentLevel;
        BoardModel board = grid.Board;
        Assert.AreEqual(4, board.RequiredSoldierTotal);

        // Ataque errado: tabuleiro vazio continua no mesmo nível.
        GameManager.Instance.OnAttackButtonPressed();
        Assert.IsTrue(GameManager.Instance.IsAnimating);
        deadline = Time.realtimeSinceStartup + 5f;
        while (GameManager.Instance.IsAnimating) {
            Assert.Less(Time.realtimeSinceStartup, deadline, "O ataque não terminou.");
            yield return null;
        }
        Assert.AreEqual(1, GameManager.Instance.AttackAttempts);
        Assert.AreSame(firstLevel, grid.CurrentLevel);

        // Solução do Level_01 (4 slimes).
        foreach (GridPos p in new[] { new GridPos(0, 1), new GridPos(0, 3), new GridPos(2, 0), new GridPos(2, 2) }) {
            Assert.IsTrue(board.SetState(p, CellState.Soldier));
        }
        Assert.IsTrue(RuleValidator.Validate(board).IsValid);

        GameManager.Instance.OnAttackButtonPressed();
        Assert.AreEqual(2, GameManager.Instance.AttackAttempts, "O segundo ataque não começou.");
        deadline = Time.realtimeSinceStartup + 10f;
        while (grid.CurrentLevel == firstLevel || grid.IsTransitioning) {
            Assert.Less(Time.realtimeSinceStartup, deadline, "Não avançou para o próximo nível.");
            yield return null;
        }
        Assert.AreNotSame(firstLevel, grid.CurrentLevel);
        Assert.AreEqual(0, grid.Board.SoldierCount);

        // Nível 2 tem Goblins, que não têm controller de ataque: o ataque precisa terminar sem erro.
        GameManager.Instance.OnAttackButtonPressed();
        Assert.IsTrue(GameManager.Instance.IsAnimating);
        deadline = Time.realtimeSinceStartup + 5f;
        while (GameManager.Instance.IsAnimating) {
            Assert.Less(Time.realtimeSinceStartup, deadline, "O ataque não terminou.");
            yield return null;
        }
        Assert.AreEqual(1, GameManager.Instance.AttackAttempts);

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator MouseClicks_ReachTheBoard() {
        yield return new EnterPlayMode();

        // Sem Game View no batchmode: manda todo input para o jogo, mesmo sem foco.
        InputSettings settings = InputSystem.settings;
        var oldEditorBehavior = settings.editorInputBehaviorInPlayMode;
        var oldBackground = settings.backgroundBehavior;
        settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        Mouse mouse = InputSystem.AddDevice<Mouse>();

        try {
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            float deadline = Time.realtimeSinceStartup + 5f;
            while (GridController.Instance == null || GridController.Instance.Board == null) {
                Assert.Less(Time.realtimeSinceStartup, deadline, "Cena não carregou o tabuleiro.");
                yield return null;
            }

            GridView view = GridController.Instance.CurrentGrid;
            BoardModel board = view.Board;
            var target = new GridPos(0, 1);
            Vector2 screen = Camera.main.WorldToScreenPoint(view.CellToWorld(target));

            // Clique esquerdo: Vazia -> Soldado.
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }.WithButton(MouseButton.Left));
            for (int i = 0; i < 3; i++) yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen });
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(CellState.Soldier, board.GetState(target), "O clique esquerdo não chegou à casa.");

            // Clique direito: Soldado -> Grama.
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }.WithButton(MouseButton.Right));
            for (int i = 0; i < 3; i++) yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen });
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(CellState.Grass, board.GetState(target), "O clique direito não chegou à casa.");
        } finally {
            InputSystem.RemoveDevice(mouse);
            settings.editorInputBehaviorInPlayMode = oldEditorBehavior;
            settings.backgroundBehavior = oldBackground;
        }

        yield return new ExitPlayMode();
    }
}
