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

    // Vencer grava recordes no PlayerPrefs do Editor; os testes devolvem os valores do jogador.
    private static readonly string[] RecordKeys = { "best.Tutorial", "best.Easy", "best.Medium", "best.Hard" };
    private readonly System.Collections.Generic.Dictionary<string, string> savedRecords =
        new System.Collections.Generic.Dictionary<string, string>();

    [SetUp]
    public void SaveRecords() {
        savedRecords.Clear();
        foreach (string key in RecordKeys) {
            if (PlayerPrefs.HasKey(key)) savedRecords[key] = PlayerPrefs.GetString(key);
        }
    }

    [TearDown]
    public void RestoreRecords() {
        foreach (string key in RecordKeys) {
            if (savedRecords.TryGetValue(key, out string value)) PlayerPrefs.SetString(key, value);
            else PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
        Time.timeScale = 1f;
    }

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
        Assert.IsNotEmpty(firstLevel.solution, "O primeiro nível precisa de solução salva.");

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

        // Solução salva do primeiro nível do tutorial.
        foreach (Vector2Int s in firstLevel.solution) {
            Assert.IsTrue(board.SetState(new GridPos(s.x, s.y), CellState.Soldier));
        }
        Assert.IsTrue(RuleValidator.Validate(board).IsValid);

        GameManager.Instance.OnAttackButtonPressed();
        Assert.AreEqual(2, GameManager.Instance.AttackAttempts, "O segundo ataque não começou.");
        VictoryScreen victory = Object.FindAnyObjectByType<VictoryScreen>(FindObjectsInactive.Include);
        Assert.IsNotNull(victory, "A cena não tem tela de vitória.");
        deadline = Time.realtimeSinceStartup + Timeout;
        while (!victory.IsOpen) {
            Assert.Less(Time.realtimeSinceStartup, deadline, "A tela de vitória não abriu.");
            yield return null;
        }
        ContinueFromVictory(victory, 2);
        deadline = Time.realtimeSinceStartup + Timeout;
        while (grid.CurrentLevel == firstLevel || grid.IsTransitioning) {
            Assert.Less(Time.realtimeSinceStartup, deadline, "Não avançou para o próximo nível.");
            yield return null;
        }
        Assert.AreNotSame(firstLevel, grid.CurrentLevel);
        Assert.AreEqual(0, grid.Board.SoldierCount);

        // Ataque errado no nível 2 também termina sem erro.
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
        VictoryScreen victory = Object.FindAnyObjectByType<VictoryScreen>(FindObjectsInactive.Include);
        Assert.IsNotNull(victory, "A cena não tem tela de vitória.");
        deadline = Time.realtimeSinceStartup + Timeout;
        while (!victory.IsOpen) {
            Assert.Less(Time.realtimeSinceStartup, deadline, "A tela de vitória não abriu.");
            yield return null;
        }
        ContinueFromVictory(victory, 1);
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

    /// <summary>Confere a tela de vitória aberta e aperta Próximo mapa.</summary>
    private static void ContinueFromVictory(VictoryScreen victory, int attempts) {
        Assert.AreEqual(MatchState.Won, GameManager.Instance.Session.State);
        Assert.AreEqual(attempts, GameManager.Instance.AttackAttempts);
        Assert.IsFalse(GameManager.Instance.Session.Pause(), "Não dá para pausar com a vitória aberta.");
        victory.Next();
        Assert.IsFalse(victory.IsOpen);
    }

    [UnityTest]
    public IEnumerator Pause_StopsClockAndInput_ThenRestartKeepsSeed() {
        yield return new EnterPlayMode();

        EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        float deadline = Time.realtimeSinceStartup + Timeout;
        while (GridController.Instance == null || GridController.Instance.Board == null) {
            Assert.Less(Time.realtimeSinceStartup, deadline, "Cena não carregou o tabuleiro.");
            yield return null;
        }

        LevelManager.Instance.StartProcedural(Difficulty.Easy, 4242);
        yield return null;
        GameManager game = GameManager.Instance;
        BoardModel board = GridController.Instance.Board;
        board.SetState(FirstFreeCell(board), CellState.Soldier);

        // Deixa o relógio andar um pouco.
        deadline = Time.realtimeSinceStartup + 0.3f;
        while (Time.realtimeSinceStartup < deadline) yield return null;
        Assert.Greater(game.Session.ElapsedSeconds, 0);

        PauseMenu pause = Object.FindAnyObjectByType<PauseMenu>();
        Assert.IsNotNull(pause, "A cena não tem menu de pausa.");
        game.TogglePause();
        yield return null;
        Assert.AreEqual(MatchState.Paused, game.Session.State);
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsFalse(Object.FindAnyObjectByType<InputController>().BoardInputEnabled);
        Assert.IsTrue(pause.transform.Find("Panel").gameObject.activeSelf, "O painel de pausa não abriu.");

        double frozen = game.Session.ElapsedSeconds;
        deadline = Time.realtimeSinceStartup + 0.3f;
        while (Time.realtimeSinceStartup < deadline) yield return null;
        Assert.AreEqual(frozen, game.Session.ElapsedSeconds, "O tempo andou durante a pausa.");

        // Reiniciar: despausa, mesmo mapa e seed, tabuleiro e tempo zerados.
        pause.Restart();
        yield return null;
        Assert.AreEqual(MatchState.Playing, game.Session.State);
        Assert.AreEqual(1f, Time.timeScale);
        Assert.IsFalse(pause.transform.Find("Panel").gameObject.activeSelf);
        Assert.AreEqual(4242, LevelManager.Instance.CurrentSeed);
        Assert.AreNotSame(board, GridController.Instance.Board);
        Assert.AreEqual(0, GridController.Instance.Board.SoldierCount);
        Assert.Less(game.Session.ElapsedSeconds, 0.5);

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator Menu_ButtonsEndIntroEquallySpaced() {
        yield return new EnterPlayMode();

        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/MainMenu/MainMenu.unity", new LoadSceneParameters(LoadSceneMode.Single));
        // A animação de entrada termina em ~1,8 s.
        float until = Time.realtimeSinceStartup + 3f;
        while (Time.realtimeSinceStartup < until) yield return null;

        string[] names = { "StartGame", "Options", "Credits", "Exit" };
        var ys = new float[names.Length];
        for (int i = 0; i < names.Length; i++) {
            GameObject go = GameObject.Find(names[i]);
            Assert.IsNotNull(go, names[i]);
            ys[i] = ((RectTransform)go.transform).anchoredPosition.y;
            Debug.Log($"Menu {names[i]} y={ys[i]}");
        }
        for (int i = 1; i < ys.Length; i++) {
            Assert.AreEqual(ys[1] - ys[0], ys[i] - ys[i - 1], 0.5f, $"{names[i]} fora do espaçamento: {string.Join(", ", ys)}");
            Assert.Less(ys[i], ys[i - 1], $"{names[i]} fora de ordem: {string.Join(", ", ys)}");
        }

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator Menu_ModeSelect_StartsMatch_ThenBackToMenu() {
        yield return new EnterPlayMode();

        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/MainMenu/MainMenu.unity", new LoadSceneParameters(LoadSceneMode.Single));
        float deadline = Time.realtimeSinceStartup + Timeout;
        MainMenuController menu = null;
        while (menu == null) {
            Assert.Less(Time.realtimeSinceStartup, deadline, "O menu não carregou.");
            yield return null;
            menu = Object.FindAnyObjectByType<MainMenuController>();
        }
        yield return null; // Start do menu liga os botões

        // Jogar abre a seleção de modo; Médio carrega a partida no modo procedural.
        GameObject modes = GameObject.Find("ModeSelect");
        Assert.IsNull(modes, "A seleção de modo deveria começar fechada.");
        menu.OpenModeSelect();
        modes = GameObject.Find("ModeSelect");
        Assert.IsNotNull(modes);
        modes.transform.Find("Panel/Medium").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();

        deadline = Time.realtimeSinceStartup + Timeout;
        while (GridController.Instance == null || GridController.Instance.Board == null) {
            Assert.Less(Time.realtimeSinceStartup, deadline, "A partida não carregou.");
            yield return null;
        }
        Assert.AreEqual(LevelManager.LevelMode.Procedural, LevelManager.Instance.Mode);
        Assert.AreEqual(Difficulty.Medium, LevelManager.Instance.CurrentDifficulty);
        Assert.AreEqual(6, GridController.Instance.Board.Rows);
        Assert.AreEqual(8, GridController.Instance.Board.Cols);
        Assert.AreNotEqual(0, LevelManager.Instance.CurrentSeed);

        // Pausa > Menu volta ao menu principal com o tempo normal.
        GameManager.Instance.Pause();
        Object.FindAnyObjectByType<PauseMenu>().GoToMenu();
        deadline = Time.realtimeSinceStartup + Timeout;
        while (Object.FindAnyObjectByType<MainMenuController>() == null) {
            Assert.Less(Time.realtimeSinceStartup, deadline, "Não voltou ao menu.");
            yield return null;
        }
        Assert.AreEqual(1f, Time.timeScale);

        yield return new ExitPlayMode();
    }

    private static GridPos FirstFreeCell(BoardModel board) {
        for (int r = 0; r < board.Rows; r++) {
            for (int c = 0; c < board.Cols; c++) {
                if (!board.IsMonster(new GridPos(r, c))) return new GridPos(r, c);
            }
        }
        throw new AssertionException("Tabuleiro sem casa livre.");
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
            GridPos target = FirstFreeCell(board);
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
