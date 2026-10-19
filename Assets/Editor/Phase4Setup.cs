using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Monta o que a Fase 4 precisa nas cenas e nos assets. Pode rodar de novo à vontade (refaz os mesmos objetos).
/// Menu: Tools > Phase 4.
/// </summary>
public static class Phase4Setup {
    private const string MatchScenePath = "Assets/Scenes/Gameplay/LevelTest/level_01_test.unity";
    private const string SoundLibraryPath = "Assets/Resources/" + SoundLibrary.ResourcesPath + ".asset";

    private static readonly Color Ink = new Color32(0xF4, 0xE9, 0xD0, 0xFF);

    [MenuItem("Tools/Phase 4/Add Controls Legend")]
    private static void AddControlsLegendFromMenu() => Debug.Log(AddControlsLegend());

    [MenuItem("Tools/Phase 4/Create Sound Library")]
    private static void CreateSoundLibraryFromMenu() => Debug.Log(CreateSoundLibrary());

    [MenuItem("Tools/Phase 4/Fill Sound Library (Kenney)")]
    private static void FillSoundLibraryFromMenu() => Debug.Log(FillSoundLibrary());

    public static void FillSoundLibraryBatch() {
        Debug.Log(CreateSoundLibrary());
        Debug.Log(FillSoundLibrary());
    }

    [MenuItem("Tools/Phase 4/Remove Physics Leftovers")]
    private static void RemovePhysicsLeftoversFromMenu() => Debug.Log(RemovePhysicsLeftovers());

    public static void RemovePhysicsLeftoversBatch() => Debug.Log(RemovePhysicsLeftovers());

    [MenuItem("Tools/Phase 4/Apply Board Frame And Attack Button")]
    private static void ApplyBoardAndButtonFromMenu() => ApplyBoardAndButtonBatch();

    public static void ApplyBoardAndButtonBatch() {
        Debug.Log(ApplyBoardFrame());
        Debug.Log(ApplyAttackButton());
    }

    private const string GridPrefabPath = "Assets/Prefabs/GridCell/Grid.prefab";
    private const string BoardFramePath = "Assets/Art/Board/Original/BoardFrame.png";
    private const int BoardFrameBorder = 10; // igual a BORDER em Tools/art/board.py

    /// <summary>Moldura gerada por Tools/art/board.py no lugar do recorte do pacote de UI de terceiros.</summary>
    public static string ApplyBoardFrame() {
        AssetDatabase.ImportAsset(BoardFramePath, ImportAssetOptions.ForceSynchronousImport);
        if (!(AssetImporter.GetAtPath(BoardFramePath) is TextureImporter importer)) {
            return $"{BoardFramePath} não encontrado; rode python Tools/art/board.py antes.";
        }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.spriteBorder = Vector4.one * BoardFrameBorder;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; // Tiled exige FullRect
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BoardFramePath);
        GameObject grid = PrefabUtility.LoadPrefabContents(GridPrefabPath);
        Transform bg = grid.transform.Find("BG");
        bg.localScale = Vector3.one; // a GridView ajusta o tamanho pelo SpriteRenderer.size
        var frame = bg.GetComponentInChildren<SpriteRenderer>(true);
        frame.sprite = sprite;
        frame.drawMode = SpriteDrawMode.Tiled;
        frame.tileMode = SpriteTileMode.Continuous;
        frame.size = new Vector2(10.7f, 10.7f);

        var view = new SerializedObject(grid.GetComponent<GridView>());
        view.FindProperty("boardFrame").objectReferenceValue = frame;
        view.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(grid, GridPrefabPath);
        PrefabUtility.UnloadPrefabContents(grid);
        return "Moldura do tabuleiro trocada pelo BoardFrame.png (9-slice, Tiled).";
    }

    /// <summary>Botão Atacar com a arte gerada (Button_Atacar, 3 estados) e menor que o antigo.</summary>
    public static string ApplyAttackButton() {
        Scene scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
        var button = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            .FirstOrDefault(b => b.name == "AttackButton");
        if (button == null) return "AttackButton não encontrado na cena da partida.";

        Sprite[] states = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/UI/Original/Button_Atacar.png")
            .OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (states.Length < 3) return "Button_Atacar.png precisa de 3 quadros (normal, hover, pressed).";

        var image = (UnityEngine.UI.Image)button.targetGraphic;
        image.sprite = states[0];
        image.type = UnityEngine.UI.Image.Type.Simple;
        image.preserveAspect = true;
        button.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
        button.spriteState = new UnityEngine.UI.SpriteState {
            highlightedSprite = states[1], selectedSprite = states[1], pressedSprite = states[2],
        };

        // 3 px de tela por pixel da arte (o menu usa 4). O botão em si tem 58x16 dentro da célula
        // de 72x20 (Tools/art/attack_button.py), então aparece com 174x48.
        var rect = (RectTransform)button.transform;
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(states[0].rect.width * 3f, states[0].rect.height * 3f);

        // O texto "ATACAR" já está desenhado no sprite.
        foreach (Transform child in button.transform) child.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"AttackButton com a arte Button_Atacar, {rect.sizeDelta.x}x{rect.sizeDelta.y}.";
    }

    [MenuItem("Tools/Phase 4/Apply Match Look (Frame, Button, Backdrop)")]
    private static void ApplyMatchLookFromMenu() => ApplyMatchLookBatch();

    public static void ApplyMatchLookBatch() {
        Debug.Log(ApplyBoardFrame());
        Debug.Log(ApplyAttackButton());
        Debug.Log(AddMatchBackdrop());
    }

    /// <summary>Céu noturno do título como fundo da partida, atrás de tudo.</summary>
    public static string AddMatchBackdrop() {
        Scene scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
        GameObject old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "MatchBackdrop");
        if (old != null) Object.DestroyImmediate(old);

        var sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/UI/Original/Title_Background.png")
            .OfType<Sprite>().FirstOrDefault();
        if (sprite == null) return "Title_Background.png sem sprite.";

        var go = new GameObject("MatchBackdrop", typeof(SpriteRenderer), typeof(CameraBackdrop));
        go.transform.position = new Vector3(0, 0, 50);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = -1000;
        sr.color = new Color(0.75f, 0.75f, 0.8f); // um pouco mais escuro, para o tabuleiro se destacar
        SceneManager.MoveGameObjectToScene(go, scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Fundo da partida: céu noturno do título (MatchBackdrop).";
    }

    [MenuItem("Tools/Phase 4/Remove Soldier Counter")]
    private static void RemoveSoldierCounterFromMenu() => Debug.Log(RemoveSoldierCounter());

    public static void RemoveSoldierCounterBatch() => Debug.Log(RemoveSoldierCounter());

    /// <summary>O "colocados / total" saiu do HUD: as pistas das bordas já dizem isso.</summary>
    public static string RemoveSoldierCounter() {
        Scene scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
        string[] names = { "SoldierLimitText", "SoldierIcon" };
        var found = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .Where(t => names.Contains(t.name))
            .ToArray();
        foreach (Transform t in found) Object.DestroyImmediate(t.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"Contador de soldados removido do HUD ({found.Length} objeto(s)).";
    }

    [MenuItem("Tools/Phase 4/Apply Cell Tile")]
    private static void ApplyCellTileFromMenu() => Debug.Log(ApplyCellTile());

    public static void ApplyCellTileBatch() => Debug.Log(ApplyCellTile());

    private const string CellTilePath = "Assets/Art/Board/Original/Cell.png";

    /// <summary>Usa o tile gerado por Tools/art/cell.py (paleta única) como fundo das casas.</summary>
    public static string ApplyCellTile() {
        AssetDatabase.ImportAsset(CellTilePath, ImportAssetOptions.ForceSynchronousImport);
        if (!(AssetImporter.GetAtPath(CellTilePath) is TextureImporter importer)) {
            return $"{CellTilePath} não encontrado; rode python Tools/art/cell.py antes.";
        }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CellTilePath);
        GameObject cell = PrefabUtility.LoadPrefabContents(GridCellPrefabPath);
        var view = cell.GetComponent<GridCell>();
        var bg = (SpriteRenderer)new SerializedObject(view).FindProperty("BGColor").objectReferenceValue;
        bg.sprite = sprite;
        PrefabUtility.SaveAsPrefabAsset(cell, GridCellPrefabPath);
        PrefabUtility.UnloadPrefabContents(cell);
        return "Fundo das casas trocado pelo Cell.png (PPU 32, Point, sem compressão).";
    }

    private const string MenuScenePath = "Assets/Scenes/MainMenu/MainMenu.unity";
    private const string GridCellPrefabPath = "Assets/Prefabs/GridCell/GridCell.prefab";

    /// <summary>
    /// O input do tabuleiro usa <see cref="GridView.TryWorldToCell"/>, então os raycasters de física da câmera
    /// e os colliders 3D das casas sobraram de versões antigas. O BoxCollider2D do Grid fica: ele dá os limites.
    /// </summary>
    public static string RemovePhysicsLeftovers() {
        var report = new System.Text.StringBuilder();

        GameObject cell = PrefabUtility.LoadPrefabContents(GridCellPrefabPath);
        Collider[] colliders = cell.GetComponentsInChildren<Collider>(true);
        foreach (Collider c in colliders) Object.DestroyImmediate(c);
        PrefabUtility.SaveAsPrefabAsset(cell, GridCellPrefabPath);
        PrefabUtility.UnloadPrefabContents(cell);
        report.AppendLine($"GridCell: {colliders.Length} collider(s) 3D removido(s).");

        foreach (string path in new[] { MatchScenePath, MenuScenePath }) {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var raycasters = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<UnityEngine.EventSystems.PhysicsRaycaster>(true))
                .ToArray(); // Physics2DRaycaster herda de PhysicsRaycaster
            foreach (var r in raycasters) Object.DestroyImmediate(r);
            if (raycasters.Length > 0) {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            report.AppendLine($"{Path.GetFileNameWithoutExtension(path)}: {raycasters.Length} raycaster(s) de física removido(s).");
        }
        return report.ToString().TrimEnd();
    }

    private const string KenneyDir = "Assets/Audio/Kenney/";

    // Escolha inicial por nome de arquivo (CC0, créditos em CREDITS.md). Troque à vontade no Inspector.
    private static readonly (Sfx id, float volume, string[] files)[] KenneyEffects = {
        (Sfx.CursorMove, 0.4f, new[] { "tick_001" }),
        (Sfx.PlaceSoldier, 0.8f, new[] { "beltHandle1", "beltHandle2" }),
        (Sfx.PlaceGrass, 0.6f, new[] { "footstep_grass_000", "footstep_grass_001", "footstep_grass_002" }),
        (Sfx.ClearCell, 0.5f, new[] { "minimize_001" }),
        (Sfx.MonsterClick, 0.7f, new[] { "impactSoft_medium_000", "impactSoft_medium_001" }),
        (Sfx.Error, 0.7f, new[] { "error_004" }),
        (Sfx.Attack, 0.8f, new[] { "knifeSlice", "knifeSlice2", "drawKnife1" }),
        (Sfx.MonsterDeath, 0.8f, new[] { "impactPunch_heavy_000", "impactPunch_heavy_001", "impactPunch_heavy_002" }),
        (Sfx.Victory, 0.8f, new[] { "jingles_NES12" }), // termina subindo; o NES00 descia e soava como derrota
        (Sfx.UiClick, 0.6f, new[] { "click_002" })
    };

    /// <summary>Liga os clips da Kenney ao SoundLibrary e ajusta a importação para efeitos curtos.</summary>
    public static string FillSoundLibrary() {
        var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(SoundLibraryPath);
        if (library == null) return $"{SoundLibraryPath} não existe; rode Create Sound Library antes.";

        int missing = 0;
        var entries = new System.Collections.Generic.List<SoundLibrary.SfxEntry>();
        foreach ((Sfx id, float volume, string[] files) in KenneyEffects) {
            var clips = new System.Collections.Generic.List<AudioClip>();
            foreach (string file in files) {
                string path = KenneyDir + file + ".ogg";
                ConfigureShortSfx(path);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null) clips.Add(clip);
                else missing++;
            }
            entries.Add(new SoundLibrary.SfxEntry { id = id, volume = volume, clips = clips.ToArray() });
        }

        library.effects = entries.ToArray();

        // As duas faixas valem para menu e partida; o AudioService sorteia uma (a cada fase, na partida).
        var music = new System.Collections.Generic.List<AudioClip>();
        foreach (string path in MusicFiles) {
            ConfigureMusic(path);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null) music.Add(clip);
            else missing++;
        }
        library.menuMusic = music.ToArray();
        library.matchMusic = music.ToArray();

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        return missing == 0 ? "SoundLibrary preenchido com os efeitos da Kenney e as músicas." : $"SoundLibrary preenchido; {missing} clip(s) não encontrados.";
    }

    // Músicas CC0 do OpenGameArt (créditos em CREDITS.md).
    private static readonly string[] MusicFiles = {
        "Assets/Audio/Music/perces.mp3",
        "Assets/Audio/Music/lonely_night.ogg"
    };

    // Música: streaming, para não ocupar memória com a faixa inteira descomprimida.
    private static void ConfigureMusic(string path) {
        if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) return;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        if (settings.loadType == AudioClipLoadType.Streaming) return;
        settings.loadType = AudioClipLoadType.Streaming;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
    }

    // Efeito curto: mono e descomprimido ao carregar, que é o mais barato de tocar (inclusive no WebGL).
    private static void ConfigureShortSfx(string path) {
        if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) return;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        if (importer.forceToMono && settings.loadType == AudioClipLoadType.DecompressOnLoad) return;
        importer.forceToMono = true;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
    }

    /// <summary>Legenda de controles (GDD 8.3) no canto inferior esquerdo do HUD da partida.</summary>
    public static string AddControlsLegend() {
        Scene scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
        Canvas canvas = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Canvas>(true))
            .First(c => c.isRootCanvas);
        Transform hud = canvas.transform.Find("MatchHud");
        if (hud == null) return "MatchHud não encontrado; rode Tools > Phase 3 > Build UI antes.";

        Transform old = hud.Find("ControlsLegend");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var go = new GameObject("ControlsLegend", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(hud, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(40, 24);
        rect.sizeDelta = new Vector2(560, 180);

        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontBuilder.TextFontPath);
        text.text = "Botão esquerdo  soldado";
        text.fontSize = 24;
        text.lineSpacing = 4;
        text.color = Ink;
        text.alignment = TextAlignmentOptions.BottomLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.richText = true;
        text.raycastTarget = false;

        var view = go.AddComponent<ControlsLegendView>();
        var so = new SerializedObject(view);
        so.FindProperty("text").objectReferenceValue = text;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Legenda de controles criada em MatchHud/ControlsLegend.";
    }

    /// <summary>Catálogo de áudio vazio em Resources, com uma entrada por efeito.</summary>
    public static string CreateSoundLibrary() {
        var existing = AssetDatabase.LoadAssetAtPath<SoundLibrary>(SoundLibraryPath);
        if (existing != null) {
            Selection.activeObject = existing;
            return $"{SoundLibraryPath} já existe.";
        }

        Directory.CreateDirectory(Path.GetDirectoryName(SoundLibraryPath));
        var library = ScriptableObject.CreateInstance<SoundLibrary>();
        // Reset() só roda no Inspector; aqui a lista é preenchida à mão.
        library.effects = System.Enum.GetValues(typeof(Sfx)).Cast<Sfx>()
            .Select(id => new SoundLibrary.SfxEntry { id = id })
            .ToArray();
        AssetDatabase.CreateAsset(library, SoundLibraryPath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = library;
        return $"{SoundLibraryPath} criado. Arraste os clips no Inspector.";
    }
}
