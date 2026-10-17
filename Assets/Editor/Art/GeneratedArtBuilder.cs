using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Monta os clips e o AnimatorController de cada personagem a partir das folhas geradas por Tools/art
/// e liga tudo no GridCell.prefab. Estados: "{Prefixo}_{Idle|Attack|Hit|Death}_{Down|Left|Right|Up}",
/// que é o que o <see cref="AnimationSwitcher"/> procura.
///
/// Menu: Tools > Art > Rebuild Generated Animations. Também roda quando o terminal escreve
/// Temp/ArtBridge/request.txt (resposta em Temp/ArtBridge/result.txt), igual ao TestBridge.
/// </summary>
[InitializeOnLoad]
public static class GeneratedArtBuilder {
    private const string AnimRoot = "Assets/Art/Animations/Characters";
    private const string PrefabPath = "Assets/Prefabs/GridCell/GridCell.prefab";

    // Pasta da arte -> nome do filho no GridCell (e prefixo dos estados).
    private static readonly (string art, string prefix)[] Characters = {
        ("Slime", "Slime"), ("Goblin", "Goblin"), ("Orc", "Orc"), ("Paladin", "Soldier"),
    };
    private static readonly string[] Directions = { "Down", "Left", "Right", "Up" };
    private static readonly (string name, float fps, bool loop)[] Actions = {
        ("Idle", 6f, true), ("Attack", 12f, false), ("Hit", 12f, false), ("Death", 10f, false),
    };

    private static readonly string BridgeDir = Path.GetFullPath("Temp/ArtBridge");
    private static double nextPoll;

    static GeneratedArtBuilder() {
        EditorApplication.update += Poll;
    }

    private static void Poll() {
        if (EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 1.0;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;

        string request = Path.Combine(BridgeDir, "request.txt");
        if (!File.Exists(request)) return;
        string command = File.ReadAllText(request).Trim();
        File.Delete(request);

        string result;
        try {
            result = "status: ok\n" + command switch {
                "capture" => CapturePreview(),
                "menu" => GeneratedMenuSetup.Apply(),
                "capture-menu" => GeneratedMenuSetup.Capture(BridgeDir),
                _ => BuildAll(),
            };
        } catch (Exception e) {
            result = "status: error\n" + e;
        }
        File.WriteAllText(Path.Combine(BridgeDir, "result.txt"), result, Encoding.UTF8);
    }

    [MenuItem("Tools/Art/Rebuild Generated Animations")]
    private static void BuildFromMenu() {
        Debug.Log(BuildAll());
    }

    public static string BuildAll() {
        var log = new StringBuilder();
        AssetDatabase.Refresh();
        ReimportGeneratedTextures();

        var controllers = new Dictionary<string, AnimatorController>();
        foreach ((string art, string prefix) in Characters) {
            controllers[prefix] = BuildCharacter(art, prefix, log);
        }
        UpdatePrefab(controllers, log);
        AssetDatabase.SaveAssets();
        return log.ToString();
    }

    // Garante que folhas importadas antes do GeneratedArtImporter existir recebam as configurações certas.
    private static void ReimportGeneratedTextures() {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] {
            GeneratedArtImporter.CharactersRoot.TrimEnd('/'), GeneratedArtImporter.UIRoot.TrimEnd('/'),
        });
        AssetDatabase.StartAssetEditing();
        try {
            foreach (string guid in guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                bool sliced = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Any();
                if (importer.filterMode != FilterMode.Point || !sliced) {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
        } finally {
            AssetDatabase.StopAssetEditing();
        }
    }

    private static AnimatorController BuildCharacter(string art, string prefix, StringBuilder log) {
        string folder = $"{AnimRoot}/{art}";
        EnsureFolder(folder);

        string controllerPath = $"{folder}/{art}.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState child in machine.states) machine.RemoveState(child.state);

        AnimatorState defaultState = null;
        int clips = 0;
        foreach ((string action, float fps, bool loop) in Actions) {
            foreach (string dir in Directions) {
                Sprite[] frames = LoadFrames($"{GeneratedArtImporter.CharactersRoot}{art}/{art}_{action}_{dir}.png");
                if (frames.Length == 0) {
                    log.AppendLine($"AVISO: sem quadros para {art} {action} {dir}");
                    continue;
                }
                string stateName = $"{prefix}_{action}_{dir}";
                AnimationClip clip = BuildClip($"{folder}/{stateName}.anim", stateName, frames, fps, loop);
                AnimatorState state = machine.AddState(stateName);
                state.motion = clip;
                state.writeDefaultValues = false;
                if (action == "Idle" && dir == "Down") defaultState = state;
                clips++;
            }
        }
        if (defaultState != null) machine.defaultState = defaultState;
        EditorUtility.SetDirty(controller);
        log.AppendLine($"{art}: {clips} clips -> {controllerPath}");
        return controller;
    }

    private static Sprite[] LoadFrames(string sheetPath) {
        return AssetDatabase.LoadAllAssetsAtPath(sheetPath)
            .OfType<Sprite>()
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int i) ? i : 0)
            .ToArray();
    }

    private static AnimationClip BuildClip(string path, string name, Sprite[] frames, float fps, bool loop) {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        bool isNew = clip == null;
        if (isNew) clip = new AnimationClip();
        clip.name = name;
        clip.frameRate = fps;

        // Uma chave por quadro + uma repetindo o último, para ele durar 1/fps como os outros.
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++) {
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        }
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[^1] };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (isNew) AssetDatabase.CreateAsset(clip, path);
        else EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void UpdatePrefab(Dictionary<string, AnimatorController> controllers, StringBuilder log) {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try {
            foreach ((string art, string prefix) in Characters) {
                Transform child = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == prefix);
                if (child == null) {
                    log.AppendLine($"AVISO: GridCell sem filho '{prefix}'");
                    continue;
                }
                // 32 px a 32 px/unidade = uma casa; o pivô no centro deixa os pés perto da borda de baixo.
                child.localScale = Vector3.one;
                child.localPosition = Vector3.zero;

                if (child.TryGetComponent(out SpriteRenderer sr)) {
                    Sprite[] idle = LoadFrames($"{GeneratedArtImporter.CharactersRoot}{art}/{art}_Idle_Down.png");
                    if (idle.Length > 0) sr.sprite = idle[0];
                    sr.drawMode = SpriteDrawMode.Simple;
                    sr.flipX = false;
                }
                if (child.TryGetComponent(out Animator animator)) {
                    animator.runtimeAnimatorController = controllers[prefix];
                }
                if (child.TryGetComponent(out AnimationSwitcher switcher)) {
                    var so = new SerializedObject(switcher);
                    so.FindProperty("walkController").objectReferenceValue = controllers[prefix];
                    so.FindProperty("attackController").objectReferenceValue = null;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                log.AppendLine($"GridCell/{prefix}: {art}");
            }
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        } finally {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>
    /// Renderiza uma casa com cada personagem numa cena de preview (não mexe na cena aberta)
    /// e salva em Temp/ArtBridge/gridcell_preview.png.
    /// </summary>
    public static string CapturePreview() {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var rt = new RenderTexture(640, 160, 24);
        try {
            for (int i = 0; i < Characters.Length; i++) {
                var cell = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                cell.transform.position = new Vector3(i * 1.4f - 2.1f, 0, 0);
                foreach (Transform t in cell.GetComponentsInChildren<Transform>(true)) {
                    if (t.parent != cell.transform || t.name == "BG") continue;
                    t.gameObject.SetActive(t.name == Characters[i].prefix);
                }
            }
            var camGo = new GameObject("Camera", typeof(Camera));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, scene);
            Camera cam = camGo.GetComponent<Camera>();
            cam.scene = scene;
            cam.orthographic = true;
            cam.orthographicSize = 0.8f;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.23f, 0.21f, 0.29f);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            string path = Path.Combine(BridgeDir, "gridcell_preview.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return path;
        } finally {
            rt.Release();
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void EnsureFolder(string path) {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
