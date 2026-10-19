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
        (Sfx.Victory, 0.8f, new[] { "jingles_NES00" }),
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
