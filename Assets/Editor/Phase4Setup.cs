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
