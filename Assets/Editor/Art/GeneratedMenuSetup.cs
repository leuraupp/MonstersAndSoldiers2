using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Aplica a arte original da UI (Assets/Art/UI/Original) no MainMenu: fundo, logo, personagens
/// animados e botões com troca de sprite. Pode rodar de novo à vontade (refaz os mesmos objetos).
/// Menu: Tools > Art > Apply Original Art To Main Menu.
/// </summary>
public static class GeneratedMenuSetup {
    private const string ScenePath = "Assets/Scenes/MainMenu/MainMenu.unity";
    private const float Px = 4f; // 1 pixel da arte = 4 px no Canvas de 1920x1080

    // Personagem, folha de idle e posição no fundo (em pixels da arte de 480x270, x do centro e y dos pés).
    private static readonly (string name, string sheet, float x, float feetY, int startFrame)[] Actors = {
        ("Paladin", "Characters/Paladin/Paladin_Idle_Right", 92, 244, 0),
        ("Slime", "Characters/Slime/Slime_Idle_Left", 300, 242, 1),
        ("Goblin", "Characters/Goblin/Goblin_Idle_Left", 340, 243, 2),
        ("Orc", "Characters/Orc/Orc_Idle_Left", 392, 244, 3),
    };

    private static readonly (string objectName, string sheet)[] Buttons = {
        ("StartGame", "Button_Jogar"), ("Exit", "Button_Sair"),
    };

    [MenuItem("Tools/Art/Apply Original Art To Main Menu")]
    private static void ApplyFromMenu() {
        Debug.Log(Apply());
    }

    public static string Apply() {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        try {
            Canvas canvas = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Canvas>(true))
                .First(c => c.isRootCanvas);
            Transform root = canvas.transform;

            // Fundo: cobre a tela inteira mantendo 16:9 (corta as bordas em telas de outra proporção).
            Image bg = root.Find("BG").GetComponent<Image>();
            bg.sprite = Sprite("UI/Original/Title_Background");
            bg.type = Image.Type.Simple;
            bg.preserveAspect = false;
            bg.raycastTarget = false;
            RectTransform bgRect = bg.rectTransform;
            bgRect.anchorMin = bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(480 * Px, 270 * Px);
            bgRect.anchoredPosition = Vector2.zero;
            if (!bg.TryGetComponent(out AspectRatioFitter fitter)) fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            int order = bg.transform.GetSiblingIndex() + 1;

            // Personagens: filhos do fundo, para acompanharem o corte/escala dele.
            Transform actors = FindOrCreate(bg.transform, "MenuActors");
            Stretch((RectTransform)actors);
            for (int i = actors.childCount - 1; i >= 0; i--) Object.DestroyImmediate(actors.GetChild(i).gameObject);
            foreach (var actor in Actors) {
                var go = new GameObject(actor.name, typeof(RectTransform), typeof(Image), typeof(UISpriteAnimator));
                go.transform.SetParent(actors, false);
                Sprite[] frames = Sprites(actor.sheet);
                var image = go.GetComponent<Image>();
                image.sprite = frames[0];
                image.raycastTarget = false;
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0.5f, 4f / 32f);   // pés na linha 28 do quadro de 32
                rect.sizeDelta = new Vector2(64 * Px, 64 * Px); // personagem em 2x sobre o fundo
                rect.anchoredPosition = new Vector2(actor.x * Px, (270 - actor.feetY) * Px);
                var so = new SerializedObject(go.GetComponent<UISpriteAnimator>());
                SerializedProperty list = so.FindProperty("frames");
                list.arraySize = frames.Length;
                for (int f = 0; f < frames.Length; f++) list.GetArrayElementAtIndex(f).objectReferenceValue = frames[f];
                so.FindProperty("startFrame").intValue = actor.startFrame;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Logo no topo.
            Transform logoT = FindOrCreate(root, "Logo");
            logoT.SetSiblingIndex(order);
            if (!logoT.TryGetComponent(out Image logo)) logo = logoT.gameObject.AddComponent<Image>();
            logo.sprite = Sprite("UI/Original/Logo");
            logo.raycastTarget = false;
            var logoRect = (RectTransform)logoT;
            logoRect.anchorMin = logoRect.anchorMax = new Vector2(0.5f, 1f);
            logoRect.pivot = new Vector2(0.5f, 1f);
            logoRect.sizeDelta = new Vector2(logo.sprite.rect.width * Px, logo.sprite.rect.height * Px);
            logoRect.anchoredPosition = new Vector2(0f, -100f);

            // Botões: texto já vem desenhado na arte, então o TMP filho fica desligado.
            float y = -10f;
            foreach (var (objectName, sheet) in Buttons) {
                Transform t = root.Find(objectName);
                if (t == null) continue;
                Sprite[] states = Sprites($"UI/Original/{sheet}");
                var image = t.GetComponent<Image>();
                image.sprite = states[0];
                image.type = Image.Type.Simple;
                var button = t.GetComponent<Button>();
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState {
                    highlightedSprite = states[1], selectedSprite = states[1], pressedSprite = states[2],
                };
                var rect = (RectTransform)t;
                rect.sizeDelta = new Vector2(states[0].rect.width * Px, states[0].rect.height * Px);
                rect.anchoredPosition = new Vector2(0f, y);
                y -= 110f;
                foreach (TMPro.TMP_Text text in t.GetComponentsInChildren<TMPro.TMP_Text>(true)) {
                    text.gameObject.SetActive(false);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return $"MainMenu atualizado ({Actors.Length} personagens, {Buttons.Length} botões)";
        } finally {
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }
    }

    /// <summary>Renderiza o MainMenu salvo numa cena de preview e grava Temp/ArtBridge/menu_preview.png.</summary>
    public static string Capture(string outDir) {
        Scene scene = EditorSceneManager.OpenPreviewScene(ScenePath);
        var rt = new RenderTexture(1920, 1080, 24);
        try {
            var camGo = new GameObject("CaptureCamera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(camGo, scene);
            Camera cam = camGo.GetComponent<Camera>();
            cam.scene = scene;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.targetTexture = rt;
            foreach (Canvas c in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Canvas>(true)).Where(c => c.isRootCanvas)) {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 5f;
                var scaler = c.GetComponent<CanvasScaler>();
                if (scaler != null) scaler.enabled = true;
                ((RectTransform)c.transform).sizeDelta = new Vector2(1920, 1080);
            }
            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            string path = Path.Combine(outDir, "menu_preview.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            return path;
        } finally {
            rt.Release();
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static Transform FindOrCreate(Transform parent, string name) {
        Transform t = parent.Find(name);
        if (t != null) return t;
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private static Sprite Sprite(string pathInArt) {
        return AssetDatabase.LoadAllAssetsAtPath($"Assets/Art/{pathInArt}.png").OfType<Sprite>().First();
    }

    private static Sprite[] Sprites(string pathInArt) {
        return AssetDatabase.LoadAllAssetsAtPath($"Assets/Art/{pathInArt}.png")
            .OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
            .ToArray();
    }
}
