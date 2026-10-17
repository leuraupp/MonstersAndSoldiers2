using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Aplica a arte original da UI (Assets/Art/UI/Original) no MainMenu: fundo em camadas com parallax,
/// estrelas piscando, nuvens, bandeiras, vaga-lumes, personagens animados, logo e botões com entrada animada.
/// As posições vêm de TitleLayout.json, gerado por Tools/art/title.py junto com a arte.
/// Pode rodar de novo à vontade (refaz os mesmos objetos). Menu: Tools > Art > Apply Original Art To Main Menu.
/// </summary>
public static class GeneratedMenuSetup {
    private const string ScenePath = "Assets/Scenes/MainMenu/MainMenu.unity";
    private const string LayoutPath = "Assets/Art/UI/Original/TitleLayout.json";
    private const float Px = 4f; // 1 pixel da arte = 4 px no Canvas de 1920x1080

    // Camadas de trás para frente e quanto cada uma anda com o ponteiro (px do Canvas, no máximo 4 * pad).
    private static readonly (string name, float parallax)[] Layers = {
        ("Sky", 4f), ("Far", 10f), ("Mid", 18f), ("Near", 28f),
    };

    private static readonly (string objectName, string sheet)[] Buttons = {
        ("StartGame", "MenuButton_Jogar"), ("Exit", "MenuButton_Sair"),
    };

#pragma warning disable 0649 // preenchidos pelo JsonUtility
    [System.Serializable] private class Point { public int x, y; }
    [System.Serializable] private class CloudInfo { public string sprite; public int x, y; public float speed; }
    [System.Serializable] private class ActorInfo { public string name, sheet; public int x, feetY, startFrame; }
    [System.Serializable] private class Area { public int x, y, w, h; }
    [System.Serializable] private class Layout {
        public int width, height, pad;
        public Point[] flags, twinkles;
        public CloudInfo[] clouds;
        public ActorInfo[] actors;
        public Area fireflyArea;
    }
#pragma warning restore 0649

    [MenuItem("Tools/Art/Apply Original Art To Main Menu")]
    private static void ApplyFromMenu() {
        Debug.Log(Apply());
    }

    public static string Apply() {
        var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(LayoutPath));
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        try {
            Canvas canvas = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Canvas>(true))
                .First(c => c.isRootCanvas);
            Transform root = canvas.transform;

            // Fundo: cobre a tela inteira mantendo 16:9 (corta as bordas em telas de outra proporção).
            // A cor sólida só aparece se alguma camada deslizar além da borda.
            Image bg = root.Find("BG").GetComponent<Image>();
            bg.sprite = null;
            bg.color = new Color32(0x1a, 0x1c, 0x3a, 0xff);
            bg.type = Image.Type.Simple;
            bg.raycastTarget = false;
            RectTransform bgRect = bg.rectTransform;
            bgRect.anchorMin = bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(layout.width * Px, layout.height * Px);
            bgRect.anchoredPosition = Vector2.zero;
            if (!bg.TryGetComponent(out AspectRatioFitter fitter)) fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = (float)layout.width / layout.height;
            for (int i = bg.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(bg.transform.GetChild(i).gameObject);

            var layers = new Dictionary<string, RectTransform>();
            foreach ((string name, float parallax) in Layers) {
                RectTransform layer = NewImage(bg.transform, name, Sprite($"UI/Original/Title_{name}"));
                layer.anchorMin = layer.anchorMax = new Vector2(0.5f, 0.5f);
                layer.sizeDelta = new Vector2((layout.width + 2 * layout.pad) * Px, layout.height * Px);
                layer.anchoredPosition = Vector2.zero;
                Set(layer.gameObject.AddComponent<MenuParallax>(), so => so.FindProperty("strength").floatValue = parallax);
                layers[name] = layer;
            }

            // Posição de um ponto da arte (x da tela de 480, y de cima para baixo) dentro de uma camada.
            Vector2 At(float x, float y) => new Vector2((x + layout.pad) * Px, (layout.height - y) * Px);

            Transform twinkles = Group(layers["Sky"], "Twinkles");
            Sprite[] twinkle = Sprites("UI/Original/Title_Twinkle");
            for (int i = 0; i < layout.twinkles.Length; i++) {
                RectTransform star = NewImage(twinkles, $"Twinkle{i}", twinkle[0]);
                Place(star, At(layout.twinkles[i].x, layout.twinkles[i].y), new Vector2(0.5f, 0.5f), twinkle[0]);
                Animate(star, twinkle, 3f + (i * 7 % 5) * 0.5f, i * 3);
            }

            Transform clouds = Group(layers["Sky"], "Clouds");
            foreach (CloudInfo info in layout.clouds) {
                Sprite sprite = Sprite($"UI/Original/{info.sprite}");
                RectTransform cloud = NewImage(clouds, info.sprite, sprite);
                Place(cloud, At(info.x, info.y), new Vector2(0.5f, 1f), sprite);
                Set(cloud.gameObject.AddComponent<MenuDrift>(), so => {
                    so.FindProperty("speed").floatValue = info.speed * Px;
                    so.FindProperty("minX").floatValue = -40 * Px;
                    so.FindProperty("maxX").floatValue = (layout.width + 2 * layout.pad + 40) * Px;
                });
            }

            Transform flags = Group(layers["Far"], "Flags");
            Sprite[] flag = Sprites("UI/Original/Title_Flag");
            for (int i = 0; i < layout.flags.Length; i++) {
                RectTransform f = NewImage(flags, $"Flag{i}", flag[0]);
                Place(f, At(layout.flags[i].x, layout.flags[i].y - 1), new Vector2(0f, 1f), flag[0]);
                Animate(f, flag, 6f, i);
            }

            // Personagens e vaga-lumes andam junto com o chão da frente.
            Transform actors = Group(layers["Near"], "MenuActors");
            foreach (ActorInfo actor in layout.actors) {
                Sprite[] frames = Sprites(actor.sheet);
                RectTransform rect = NewImage(actors, actor.name, frames[0]);
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 4f / 32f);       // pés na linha 28 do quadro de 32
                rect.sizeDelta = new Vector2(64 * Px, 64 * Px); // personagem em 2x sobre o fundo
                rect.anchoredPosition = At(actor.x, actor.feetY);
                Animate(rect, frames, 6f, actor.startFrame);
            }

            Transform fireflies = Group(layers["Near"], "Fireflies");
            Area a = layout.fireflyArea;
            Set(fireflies.gameObject.AddComponent<MenuFireflies>(), so => {
                so.FindProperty("sprite").objectReferenceValue = Sprite("UI/Original/Title_Firefly");
                so.FindProperty("area").rectValue = new Rect((a.x + layout.pad) * Px, (layout.height - a.y - a.h) * Px, a.w * Px, a.h * Px);
                so.FindProperty("size").floatValue = 5 * Px;
                so.FindProperty("wander").floatValue = 12 * Px;
            });

            int order = bg.transform.GetSiblingIndex() + 1;

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
            logoRect.anchoredPosition = new Vector2(0f, -88f);

            // Botões: texto já vem desenhado na arte, então o TMP filho fica desligado.
            float y = -20f;
            var buttonRects = new List<RectTransform>();
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
                y -= 120f;
                foreach (TMPro.TMP_Text text in t.GetComponentsInChildren<TMPro.TMP_Text>(true)) {
                    text.gameObject.SetActive(false);
                }
                if (!t.TryGetComponent(out MenuButtonFx _)) t.gameObject.AddComponent<MenuButtonFx>();
                buttonRects.Add(rect);
            }

            // Cortina da entrada: fica por cima de tudo. Desligada na cena (para não esconder nada no Editor);
            // o MenuIntro liga e apaga no Start.
            Transform fadeT = FindOrCreate(root, "Fade");
            fadeT.SetAsLastSibling();
            if (!fadeT.TryGetComponent(out Image fade)) fade = fadeT.gameObject.AddComponent<Image>();
            fade.color = new Color32(0x1b, 0x14, 0x28, 0xff);
            fade.raycastTarget = false;
            Stretch((RectTransform)fadeT);
            fadeT.gameObject.SetActive(false);

            Transform introT = FindOrCreate(root, "MenuIntro");
            if (!introT.TryGetComponent(out MenuIntro intro)) intro = introT.gameObject.AddComponent<MenuIntro>();
            Set(intro, so => {
                so.FindProperty("logo").objectReferenceValue = logoRect;
                so.FindProperty("fade").objectReferenceValue = fade;
                SerializedProperty list = so.FindProperty("buttons");
                list.arraySize = buttonRects.Count;
                for (int i = 0; i < buttonRects.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = buttonRects[i];
            });

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return $"MainMenu atualizado ({Layers.Length} camadas, {layout.actors.Length} personagens, {buttonRects.Count} botões)";
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
            Directory.CreateDirectory(outDir);
            string path = Path.Combine(outDir, "menu_preview.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            return path;
        } finally {
            rt.Release();
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    /// <summary>Para rodar sem o Editor aberto: Unity -batchmode -executeMethod GeneratedMenuSetup.BatchApplyAndCapture.</summary>
    public static void BatchApplyAndCapture() {
        Debug.Log(GeneratedArtBuilder.BuildAll());
        Debug.Log(Apply());
        Debug.Log(Capture(Path.GetFullPath("Tools/art/preview")));  // Temp some quando o Unity fecha
    }

    private static RectTransform NewImage(Transform parent, string name, Sprite sprite) {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return (RectTransform)go.transform;
    }

    /// <summary>Grupo vazio que ocupa a camada inteira, com origem no canto inferior esquerdo dela.</summary>
    private static Transform Group(Transform layer, string name) {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(layer, false);
        Stretch((RectTransform)go.transform);
        return go.transform;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 pivot, Sprite sprite) {
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = pivot;
        rect.sizeDelta = new Vector2(sprite.rect.width * Px, sprite.rect.height * Px);
        rect.anchoredPosition = position;
    }

    private static void Animate(RectTransform rect, Sprite[] frames, float fps, int startFrame) {
        Set(rect.gameObject.AddComponent<UISpriteAnimator>(), so => {
            SerializedProperty list = so.FindProperty("frames");
            list.arraySize = frames.Length;
            for (int f = 0; f < frames.Length; f++) list.GetArrayElementAtIndex(f).objectReferenceValue = frames[f];
            so.FindProperty("framesPerSecond").floatValue = fps;
            so.FindProperty("startFrame").intValue = startFrame % frames.Length;
        });
    }

    private static void Set(Object target, System.Action<SerializedObject> edit) {
        var so = new SerializedObject(target);
        edit(so);
        so.ApplyModifiedPropertiesWithoutUndo();
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
