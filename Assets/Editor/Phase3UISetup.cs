using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Monta a UI da Fase 3 nas cenas: HUD, pausa, opções e vitória na partida; seleção de modo, opções
/// e créditos no MainMenu. Pode rodar de novo à vontade (apaga e refaz os mesmos objetos).
/// Menu: Tools > Phase 3 > Build UI. Batchmode: -executeMethod Phase3UISetup.BuildAllBatch.
/// </summary>
public static class Phase3UISetup {
    private const string MatchScenePath = "Assets/Scenes/Gameplay/LevelTest/level_01_test.unity";
    private const string MenuScenePath = "Assets/Scenes/MainMenu/MainMenu.unity";
    private const string FontPath = "Assets/Art/Fonts/FantasyRPGtext (size 8) SDF.asset";
    private const string TitleFontPath = "Assets/Art/Fonts/FantasyRPGtitle (size 11) SDF.asset";
    private const float Px = 4f; // 1 pixel da arte = 4 px no Canvas

    private static readonly Color Ink = new Color32(0xF4, 0xE9, 0xD0, 0xFF);
    private static readonly Color Gold = new Color32(0xFF, 0xD0, 0x5A, 0xFF);
    private static readonly Color Dim = new Color(0f, 0f, 0f, 0.6f);

    [MenuItem("Tools/Phase 3/Build UI")]
    private static void BuildFromMenu() {
        Debug.Log(BuildMatchScene());
        Debug.Log(BuildMenuScene());
    }

    public static void BuildAllBatch() {
        try {
            Debug.Log(BuildMatchScene());
            Debug.Log(BuildMenuScene());
            Directory.CreateDirectory("Logs/Phase3");
            Capture(MatchScenePath, "Logs/Phase3/match_hud.png");
            Capture(MatchScenePath, "Logs/Phase3/match_pause.png", "PauseMenu/Panel");
            Capture(MatchScenePath, "Logs/Phase3/match_victory.png", "VictoryScreen/Panel");
            Capture(MatchScenePath, "Logs/Phase3/match_options.png", "OptionsPanel/Panel");
            Capture(MatchScenePath, "Logs/Phase3/match_hint.png", "TutorialHint/Balloon");
            Capture(MenuScenePath, "Logs/Phase3/menu_main.png");
            Capture(MenuScenePath, "Logs/Phase3/menu_modes.png", "ModeSelect", "!MainButtons");
            Capture(MenuScenePath, "Logs/Phase3/menu_credits.png", "Credits", "!MainButtons");
            EditorApplication.Exit(0);
        } catch (System.Exception e) {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    // ---------------- Partida ----------------

    public static string BuildMatchScene() {
        Scene scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
        Transform root = RootCanvas(scene).transform;

        // HUD (GDD 8.3): modo, seed e tempo no topo esquerdo; pausa no topo direito.
        Transform hud = Recreate(root, "MatchHud");
        Stretch((RectTransform)hud);
        TextMeshProUGUI mode = Text(hud, "ModeText", "Fácil", 44, TextAlignmentOptions.TopLeft, Gold);
        Place(mode.rectTransform, new Vector2(0, 1), new Vector2(40, -30), new Vector2(500, 60));
        TextMeshProUGUI seed = Text(hud, "SeedText", "Seed 123456", 32, TextAlignmentOptions.TopLeft, Ink);
        Place(seed.rectTransform, new Vector2(0, 1), new Vector2(40, -90), new Vector2(500, 44));
        TextMeshProUGUI timer = Text(hud, "TimerText", "00:00", 56, TextAlignmentOptions.TopLeft, Ink);
        Place(timer.rectTransform, new Vector2(0, 1), new Vector2(40, -140), new Vector2(500, 70));
        Button pause = SpriteButton(hud, "PauseButton", "Button_Pause_Icon");
        Place((RectTransform)pause.transform, new Vector2(1, 1), new Vector2(-40, -30), null);

        UIManager ui = Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
        var so = new SerializedObject(ui);
        so.FindProperty("modeText").objectReferenceValue = mode;
        so.FindProperty("seedText").objectReferenceValue = seed;
        so.FindProperty("timerText").objectReferenceValue = timer;
        so.FindProperty("pauseButton").objectReferenceValue = pause;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Balão do tutorial (GDD 6.1), à esquerda entre o tempo e o contador de soldados.
        Transform hintRoot = Recreate(root, "TutorialHint");
        Stretch((RectTransform)hintRoot);
        var hint = hintRoot.gameObject.AddComponent<TutorialHint>();
        RectTransform balloon = Panel(hintRoot, "Balloon", new Vector2(600, 460));
        Place(balloon, new Vector2(0, 0.5f), new Vector2(24, 70), null);
        Text(balloon, "Title", "Nova regra", 40, TextAlignmentOptions.Top, Gold, TitleFontPath)
            .rectTransform.Let(r => Place(r, new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(500, 56)));
        TextMeshProUGUI hintText = Text(balloon, "Text", "A vila está cercada! Cada Slime precisa de 1 soldado numa das 8 casas ao redor.\n" +
            "Soldados não podem se encostar, nem na diagonal. Clique numa casa para pôr um soldado.", 30, TextAlignmentOptions.TopLeft, Ink);
        hintText.textWrappingMode = TextWrappingModes.Normal;
        hintText.lineSpacing = 10;
        hintText.enableAutoSizing = true;
        hintText.fontSizeMin = 18;
        hintText.fontSizeMax = 28;
        hintText.rectTransform.anchorMin = Vector2.zero;
        hintText.rectTransform.anchorMax = Vector2.one;
        hintText.rectTransform.offsetMin = new Vector2(52, 104);  // acima do botão
        hintText.rectTransform.offsetMax = new Vector2(-52, -84); // abaixo da faixa do título do painel
        Button hintOk = TextButton(balloon, "Ok", "Entendi", -350);
        Link(hint, ("balloon", balloon.gameObject), ("text", hintText), ("closeButton", hintOk));

        OptionsPanel options = BuildOptions(root);

        // Pausa (GDD 8.4).
        Transform pauseRoot = Recreate(root, "PauseMenu");
        Stretch((RectTransform)pauseRoot);
        var pauseMenu = pauseRoot.gameObject.AddComponent<PauseMenu>();
        RectTransform pausePanel = Overlay(pauseRoot, "Panel", new Vector2(720, 820));
        Text(pausePanel, "Title", "Pausa", 72, TextAlignmentOptions.Top, Gold, TitleFontPath)
            .rectTransform.Let(r => Place(r, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(600, 90)));
        TextMeshProUGUI info = Text(pausePanel, "Info", "Fácil\nSeed 123456\n00:00", 34, TextAlignmentOptions.Top, Ink);
        Place(info.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(600, 140));
        Button resume = TextButton(pausePanel, "Resume", "Continuar", -320);
        Button restart = TextButton(pausePanel, "Restart", "Reiniciar", -430);
        Button optionsButton = TextButton(pausePanel, "Options", "Opções", -540);
        Button menu = TextButton(pausePanel, "Menu", "Menu", -650);
        Link(pauseMenu, ("panel", pausePanel.parent.gameObject), ("infoText", info), ("resumeButton", resume),
             ("restartButton", restart), ("optionsButton", optionsButton), ("menuButton", menu), ("optionsPanel", options));
        options.transform.SetAsLastSibling();

        // Vitória (GDD 8.4).
        Transform victoryRoot = Recreate(root, "VictoryScreen");
        Stretch((RectTransform)victoryRoot);
        var victory = victoryRoot.gameObject.AddComponent<VictoryScreen>();
        RectTransform victoryPanel = Overlay(victoryRoot, "Panel", new Vector2(760, 720));
        Text(victoryPanel, "Title", "Vitória!", 80, TextAlignmentOptions.Top, Gold, TitleFontPath)
            .rectTransform.Let(r => Place(r, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(640, 100)));
        TextMeshProUGUI stats = Text(victoryPanel, "Stats", "Fácil\nTempo  01:23\nTentativas  2\nSeed  123456",
                                     38, TextAlignmentOptions.Top, Ink);
        Place(stats.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(640, 220));
        TextMeshProUGUI record = Text(victoryPanel, "Record", "Novo recorde!", 44, TextAlignmentOptions.Center, Gold);
        Place(record.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -400), new Vector2(640, 60));
        Button next = TextButton(victoryPanel, "Next", "Próximo mapa", -480);
        Button victoryMenu = TextButton(victoryPanel, "Menu", "Menu", -590);
        Link(victory, ("panel", victoryPanel.parent.gameObject), ("statsText", stats), ("recordBadge", record.gameObject),
             ("nextButton", next), ("menuButton", victoryMenu));

        pausePanel.parent.gameObject.SetActive(false);
        victoryPanel.parent.gameObject.SetActive(false);
        options.transform.SetAsLastSibling();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Partida: HUD, pausa, opções e vitória montados";
    }

    // ---------------- Menu principal ----------------

    public static string BuildMenuScene() {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        Canvas canvas = RootCanvas(scene);
        Transform root = canvas.transform;

        // Os botões originais (Jogar, Sair) passam a morar num grupo que some quando outro painel abre.
        Transform main = root.Find("MainButtons");
        if (main == null) {
            main = new GameObject("MainButtons", typeof(RectTransform)).transform;
            main.SetParent(root, false);
            Stretch((RectTransform)main);
            foreach (string name in new[] { "StartGame", "Exit" }) {
                Transform b = root.Find(name);
                if (b != null) b.SetParent(main, true);
            }
        }
        Transform play = main.Find("StartGame");
        Transform exit = main.Find("Exit");

        // Jogar agora abre a seleção de modo em vez de carregar a cena direto.
        Button playButton = play.GetComponent<Button>();
        for (int i = playButton.onClick.GetPersistentEventCount() - 1; i >= 0; i--) {
            UnityEventTools.RemovePersistentListener(playButton.onClick, i);
        }

        // Opções e Créditos entre Jogar e Sair (mesma arte dos botões existentes).
        Button optionsButton = MenuArtButton(main, "Options", "Button_Opcoes");
        Button creditsButton = MenuArtButton(main, "Credits", "Button_Creditos");
        float y = ((RectTransform)play).anchoredPosition.y;
        ((RectTransform)optionsButton.transform).anchoredPosition = new Vector2(0, y - 110);
        ((RectTransform)creditsButton.transform).anchoredPosition = new Vector2(0, y - 220);
        ((RectTransform)exit).anchoredPosition = new Vector2(0, y - 330);
        exit.SetAsLastSibling();

        // Seleção de modo (GDD 8.2).
        Transform modes = Recreate(root, "ModeSelect");
        Stretch((RectTransform)modes);
        RectTransform modePanel = Panel(modes, "Panel", new Vector2(820, 660));
        modePanel.anchoredPosition = new Vector2(0, -190); // abaixo do logo
        Text(modePanel, "Title", "Escolha o modo", 60, TextAlignmentOptions.Top, Gold, TitleFontPath)
            .rectTransform.Let(r => Place(r, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(700, 80)));
        Button tutorial = ModeButton(modePanel, "Tutorial", "Tutorial", -120);
        Button easy = ModeButton(modePanel, "Easy", "Fácil  6x4", -230);
        Button medium = ModeButton(modePanel, "Medium", "Médio  8x6", -340);
        Button hard = ModeButton(modePanel, "Hard", "Difícil  8x8", -450);
        Button back = TextButton(modePanel, "Back", "Voltar", -560);

        // Créditos (GDD 8.1; a lista de assets entra na Fase 4).
        Transform credits = Recreate(root, "Credits");
        Stretch((RectTransform)credits);
        RectTransform creditsPanel = Panel(credits, "Panel", new Vector2(900, 640));
        creditsPanel.anchoredPosition = new Vector2(0, -210);
        Text(creditsPanel, "Title", "Créditos", 60, TextAlignmentOptions.Top, Gold, TitleFontPath)
            .rectTransform.Let(r => Place(r, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(800, 80)));
        Text(creditsPanel, "Body", "Feito por Leonardo Raupp\n\nlinkedin.com/in/leonardo-raupp-225b5754\ngithub.com/leuraupp",
             34, TextAlignmentOptions.Top, Ink)
            .rectTransform.Let(r => Place(r, new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(800, 320)));
        Button creditsBack = TextButton(creditsPanel, "Back", "Voltar", -520);

        modes.gameObject.SetActive(false);
        credits.gameObject.SetActive(false);
        OptionsPanel options = BuildOptions(root);

        Transform controllerT = root.Find("MainMenuController");
        if (controllerT != null) Object.DestroyImmediate(controllerT.gameObject);
        var controllerGo = new GameObject("MainMenuController");
        controllerGo.transform.SetParent(root, false);
        var controller = controllerGo.AddComponent<MainMenuController>();
        Link(controller, ("mainButtons", main.gameObject), ("playButton", playButton), ("optionsButton", optionsButton),
             ("creditsButton", creditsButton), ("exitButton", exit.GetComponent<Button>()),
             ("modePanel", modes.gameObject), ("tutorialButton", tutorial), ("easyButton", easy),
             ("mediumButton", medium), ("hardButton", hard), ("modeBackButton", back),
             ("optionsPanel", options), ("creditsPanel", credits.gameObject), ("creditsBackButton", creditsBack));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "MainMenu: seleção de modo, opções e créditos montados";
    }

    // ---------------- Opções (compartilhado) ----------------

    private static OptionsPanel BuildOptions(Transform root) {
        Transform optionsRoot = Recreate(root, "OptionsPanel");
        Stretch((RectTransform)optionsRoot);
        var options = optionsRoot.gameObject.AddComponent<OptionsPanel>();
        RectTransform panel = Overlay(optionsRoot, "Panel", new Vector2(820, 820));
        Text(panel, "Title", "Opções", 64, TextAlignmentOptions.Top, Gold, TitleFontPath)
            .rectTransform.Let(r => Place(r, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(700, 80)));

        Slider music = LabeledSlider(panel, "Music", "Música", -180);
        Slider sfx = LabeledSlider(panel, "Sfx", "Efeitos", -290);

        // Resolução e tela cheia: o OptionsPanel esconde o grupo no WebGL.
        var screen = new GameObject("ScreenSettings", typeof(RectTransform));
        screen.transform.SetParent(panel, false);
        Stretch((RectTransform)screen.transform);
        Text(screen.transform, "ResolutionLabel", "Resolução", 36, TextAlignmentOptions.Left, Ink)
            .rectTransform.Let(r => Place(r, new Vector2(0.5f, 1), new Vector2(-180, -400), new Vector2(300, 60)));
        GameObject dropdownGo = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
        dropdownGo.name = "Resolution";
        dropdownGo.transform.SetParent(screen.transform, false);
        Place((RectTransform)dropdownGo.transform, new Vector2(0.5f, 1), new Vector2(150, -400), new Vector2(340, 60));
        foreach (TMP_Text t in dropdownGo.GetComponentsInChildren<TMP_Text>(true)) {
            t.font = Font(FontPath);
            t.fontSize = 28;
            t.color = Ink;
            t.fontStyle = FontStyles.UpperCase;
        }
        GameObject toggleGo = DefaultControls.CreateToggle(new DefaultControls.Resources());
        toggleGo.name = "Fullscreen";
        toggleGo.transform.SetParent(screen.transform, false);
        Place((RectTransform)toggleGo.transform, new Vector2(0.5f, 1), new Vector2(150, -490), new Vector2(340, 50));
        // Lista de resoluções no tom dos painéis em vez do branco padrão.
        foreach (Image img in dropdownGo.GetComponentsInChildren<Image>(true)) {
            if (img.name == "Dropdown" || img.name == "Resolution" || img.name == "Template" || img.name == "Item Background") {
                img.color = new Color32(0x45, 0x55, 0xA0, 0xFF);
            }
        }
        Object.DestroyImmediate(toggleGo.GetComponentInChildren<Text>().gameObject);
        Text(toggleGo.transform, "Label", "Tela cheia", 34, TextAlignmentOptions.Left, Ink)
            .rectTransform.Let(r => { r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 1); r.offsetMin = new Vector2(40, 0); r.offsetMax = Vector2.zero; });

        Button back = TextButton(panel, "Back", "Voltar", -660);
        Link(options, ("panel", panel.parent.gameObject), ("musicSlider", music), ("sfxSlider", sfx),
             ("screenSettings", screen), ("resolutionDropdown", dropdownGo.GetComponent<TMP_Dropdown>()),
             ("fullscreenToggle", toggleGo.GetComponent<Toggle>()), ("backButton", back));
        panel.parent.gameObject.SetActive(false);
        return options;
    }

    // ---------------- Peças ----------------

    private static Canvas RootCanvas(Scene scene) {
        return scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Canvas>(true)).First(c => c.isRootCanvas);
    }

    private static Transform Recreate(Transform parent, string name) {
        Transform old = parent.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    /// <summary>Fundo escuro de tela cheia (bloqueia cliques no tabuleiro) com um painel no centro.</summary>
    private static RectTransform Overlay(Transform parent, string name, Vector2 size) {
        var dim = new GameObject(name, typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(parent, false);
        Stretch((RectTransform)dim.transform);
        dim.GetComponent<Image>().color = Dim;
        return Panel(dim.transform, "Box", size);
    }

    private static RectTransform Panel(Transform parent, string name, Vector2 size) {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = Sprite("UI/Original/Panel");
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1f / Px;
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        return rect;
    }

    private static TextMeshProUGUI Text(Transform parent, string name, string value, float size,
                                        TextAlignmentOptions align, Color color, string font = FontPath) {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = Font(font);
        text.text = value;
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.fontStyle = FontStyles.UpperCase; // as minúsculas da fonte pixel ficam ilegíveis
        return text;
    }

    /// <summary>Botão com a arte em branco (Button_Blank) e o texto por cima.</summary>
    private static Button TextButton(Transform parent, string name, string label, float y) {
        Button button = SpriteButton(parent, name, "Button_Blank");
        Place((RectTransform)button.transform, new Vector2(0.5f, 1), new Vector2(0, y), null);
        TextMeshProUGUI text = Text(button.transform, "Label", label, 36, TextAlignmentOptions.Center, Ink);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(0, 6); // a arte tem sombra embaixo
        return button;
    }

    /// <summary>Botão de modo, mais largo, com um "Best" embaixo para o recorde.</summary>
    private static Button ModeButton(Transform parent, string name, string label, float y) {
        Button button = TextButton(parent, name, label, y);
        var rect = (RectTransform)button.transform;
        rect.sizeDelta = new Vector2(rect.sizeDelta.x * 1.5f, rect.sizeDelta.y);
        ((Image)button.targetGraphic).type = Image.Type.Sliced;
        TextMeshProUGUI best = Text(button.transform, "Best", "", 26, TextAlignmentOptions.Center, Gold);
        Place(best.rectTransform, new Vector2(0.5f, 0), new Vector2(0, -34), new Vector2(rect.sizeDelta.x, 34));
        return button;
    }

    private static Button MenuArtButton(Transform parent, string name, string sheet) {
        Transform old = parent.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        Button button = SpriteButton(parent, name, sheet);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        return button;
    }

    /// <summary>Botão com troca de sprite: quadro 0 normal, 1 destaque, 2 pressionado.</summary>
    private static Button SpriteButton(Transform parent, string name, string sheet) {
        Sprite[] states = Sprites($"UI/Original/{sheet}");
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = states[0];
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        if (states.Length >= 3) {
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState {
                highlightedSprite = states[1], selectedSprite = states[1], pressedSprite = states[2],
            };
        }
        ((RectTransform)go.transform).sizeDelta = new Vector2(states[0].rect.width * Px, states[0].rect.height * Px);
        return button;
    }

    private static Slider LabeledSlider(Transform parent, string name, string label, float y) {
        Text(parent, name + "Label", label, 36, TextAlignmentOptions.Left, Ink)
            .rectTransform.Let(r => Place(r, new Vector2(0.5f, 1), new Vector2(-180, y), new Vector2(300, 60)));
        GameObject go = DefaultControls.CreateSlider(new DefaultControls.Resources());
        go.name = name;
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, new Vector2(0.5f, 1), new Vector2(150, y - 10), new Vector2(340, 40));
        var slider = go.GetComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 0.8f;
        go.transform.Find("Fill Area/Fill").GetComponent<Image>().color = Gold;
        return slider;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2? size) {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        if (size.HasValue) rect.sizeDelta = size.Value;
    }

    private static void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private static void Link(Object target, params (string field, Object value)[] links) {
        var so = new SerializedObject(target);
        foreach (var (field, value) in links) {
            SerializedProperty p = so.FindProperty(field);
            if (p == null) throw new System.ArgumentException($"{target.GetType().Name} não tem o campo {field}");
            p.objectReferenceValue = value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Let<T>(this T value, System.Action<T> action) => action(value);

    private static TMP_FontAsset Font(string path) => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);

    private static Sprite Sprite(string pathInArt) {
        return AssetDatabase.LoadAllAssetsAtPath($"Assets/Art/{pathInArt}.png").OfType<Sprite>().First();
    }

    private static Sprite[] Sprites(string pathInArt) {
        Sprite[] all = AssetDatabase.LoadAllAssetsAtPath($"Assets/Art/{pathInArt}.png").OfType<Sprite>().ToArray();
        if (all.Length <= 1) return all;
        return all.OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
    }

    // ---------------- Captura para conferir o layout ----------------

    /// <summary>Renderiza a cena salva com os painéis pedidos ligados e grava um PNG.</summary>
    private static void Capture(string scenePath, string outPath, params string[] showPaths) {
        Scene scene = EditorSceneManager.OpenPreviewScene(scenePath);
        var rt = new RenderTexture(1920, 1080, 24);
        try {
            Canvas canvas = RootCanvas(scene);
            foreach (string path in showPaths) {
                bool hide = path.StartsWith("!");
                Transform t = canvas.transform.Find(hide ? path.Substring(1) : path);
                if (t != null) t.gameObject.SetActive(!hide);
            }
            var camGo = new GameObject("CaptureCamera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(camGo, scene);
            Camera cam = camGo.GetComponent<Camera>();
            cam.scene = scene;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x3A, 0x5A, 0x40, 0xFF);
            cam.targetTexture = rt;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(1920, 1080);
            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        } finally {
            rt.Release();
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
