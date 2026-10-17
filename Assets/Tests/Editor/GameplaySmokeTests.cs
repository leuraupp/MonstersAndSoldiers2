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

    // Folgado de propósito: com o Editor em segundo plano a Unity reduz o frame rate e tudo fica mais lento.
    private const float Timeout = 30f;

    [UnityTest]
    public IEnumerator WrongAttack_ThenSolve_AdvancesToNextLevel() {
        yield return new EnterPlayMode();

        EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        float deadline = Time.realtimeSinceStartup + Timeout;
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
        deadline = Time.realtimeSinceStartup + Timeout;
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
        deadline = Time.realtimeSinceStartup + Timeout;
        while (grid.CurrentLevel == firstLevel || grid.IsTransitioning) {
            Assert.Less(Time.realtimeSinceStartup, deadline, "Não avançou para o próximo nível.");
            yield return null;
        }
        Assert.AreNotSame(firstLevel, grid.CurrentLevel);
        Assert.AreEqual(0, grid.Board.SoldierCount);

        // Nível 2 tem Goblins, que não têm controller de ataque: o ataque precisa terminar sem erro.
        GameManager.Instance.OnAttackButtonPressed();
        Assert.IsTrue(GameManager.Instance.IsAnimating);
        deadline = Time.realtimeSinceStartup + Timeout;
        while (GameManager.Instance.IsAnimating) {
            Assert.Less(Time.realtimeSinceStartup, deadline, "O ataque não terminou.");
            yield return null;
        }
        Assert.AreEqual(1, GameManager.Instance.AttackAttempts);

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator ProceduralMode_ShowsClues_RestartsSameSeed_AndAdvances() {
        yield return new EnterPlayMode();

        EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        float deadline = Time.realtimeSinceStartup + Timeout;
        while (GridController.Instance == null || GridController.Instance.Board == null) {
            Assert.Less(Time.realtimeSinceStartup, deadline, "Cena não carregou o tabuleiro.");
            yield return null;
        }

        GridController grid = GridController.Instance;
        LevelManager levels = LevelManager.Instance;
        levels.StartProcedural(Difficulty.Hard, 12345);
        yield return null;

        GeneratedPuzzle puzzle = levels.CurrentPuzzle;
        BoardModel board = grid.Board;
        Assert.AreEqual(12345, levels.CurrentSeed);
        Assert.IsNull(grid.CurrentLevel);
        Assert.AreEqual(8, board.Rows);
        Assert.AreEqual(8, board.Cols);
        Assert.IsTrue(board.HasClues);

        // Pistas: 8 à esquerda e 8 em cima, com os números do mapa.
        TMPro.TextMeshPro[] labels = grid.CurrentGrid.GetComponentsInChildren<TMPro.TextMeshPro>();
        Assert.AreEqual(16, labels.Length);
        Assert.AreEqual(board.RowClues[0].ToString(), System.Array.Find(labels, l => l.name == "Row Clue 0").text);

        // Dois soldados encostados ganham contorno vermelho; ao tirar um, o contorno some.
        GridPos a = default, b = default;
        bool found = false;
        for (int r = 0; r < board.Rows && !found; r++) {
            for (int c = 0; c + 1 < board.Cols && !found; c++) {
                a = new GridPos(r, c);
                b = new GridPos(r, c + 1);
                found = !board.IsMonster(a) && !board.IsMonster(b);
            }
        }
        Assert.IsTrue(found);
        board.SetState(a, CellState.Soldier);
        board.SetState(b, CellState.Soldier);
        Assert.AreEqual(2, CountVisibleOutlines(grid.CurrentGrid));
        board.SetState(b, CellState.Empty);
        Assert.AreEqual(0, CountVisibleOutlines(grid.CurrentGrid));

        // Ataque errado pisca as pistas sem erro e continua no mesmo mapa.
        GameManager.Instance.OnAttackButtonPressed();
        deadline = Time.realtimeSinceStartup + Timeout;
        while (GameManager.Instance.IsAnimating) {
            Assert.Less(Time.realtimeSinceStartup, deadline, "O ataque não terminou.");
            yield return null;
        }
        Assert.AreSame(puzzle, levels.CurrentPuzzle);

        // Reiniciar: mesmo mapa, tabuleiro limpo.
        levels.ReloadCurrentLevel();
        yield return null;
        Assert.AreNotSame(board, grid.Board);
        board = grid.Board;
        Assert.AreEqual(0, board.SoldierCount);
        Assert.AreEqual(12345, levels.CurrentSeed);
        CollectionAssert.AreEquivalent(puzzle.Monsters, board.Monsters);

        // Solução certa: avança para um novo mapa (nova seed) na mesma dificuldade.
        foreach (GridPos p in puzzle.Solution) Assert.IsTrue(board.SetState(p, CellState.Soldier));
        GameManager.Instance.OnAttackButtonPressed();
        deadline = Time.realtimeSinceStartup + Timeout;
        while (grid.Board == board || grid.IsTransitioning) {
            Assert.Less(Time.realtimeSinceStartup, deadline, "Não avançou para o próximo mapa.");
            yield return null;
        }
        Assert.AreNotSame(puzzle, levels.CurrentPuzzle);
        Assert.AreEqual(Difficulty.Hard, levels.CurrentDifficulty);
        Assert.AreEqual(8, grid.Board.Rows);
        Assert.AreEqual(0, grid.Board.SoldierCount);

        yield return new ExitPlayMode();
    }

    private static int CountVisibleOutlines(GridView view) {
        int count = 0;
        foreach (SpriteRenderer sr in view.GetComponentsInChildren<SpriteRenderer>()) {
            if (sr.name == "Outline" && sr.enabled) count++;
        }
        return count;
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
            float deadline = Time.realtimeSinceStartup + Timeout;
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
