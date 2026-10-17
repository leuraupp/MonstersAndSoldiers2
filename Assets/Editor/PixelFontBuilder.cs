using TMPro;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Cria as fontes TMP da UI a partir da Pixelify Sans (OFL, já em Assets/Art/Fonts), que tem todos os
/// acentos do português. Atlas estático com ASCII + acentos, renderizado sem suavização para ficar pixelado.
/// Menu: Tools > Phase 3 > Build Pixel Fonts.
/// </summary>
public static class PixelFontBuilder {
    public const string TextFontPath = "Assets/Art/Fonts/PixelifySans-Regular SDF.asset";
    public const string TitleFontPath = "Assets/Art/Fonts/PixelifySans-Bold SDF.asset";

    private const string Characters =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
        "ÁÀÂÃÉÊÍÓÔÕÚÜÇáàâãéêíóôõúüç·•×–—…ºª°";

    [MenuItem("Tools/Phase 3/Build Pixel Fonts")]
    public static void Build() {
        Create("Assets/Art/Fonts/PixelifySans-Regular.ttf", TextFontPath);
        Create("Assets/Art/Fonts/PixelifySans-Bold.ttf", TitleFontPath);
        AssetDatabase.SaveAssets();
    }

    private static readonly string[] Scenes = {
        "Assets/Scenes/MainMenu/MainMenu.unity", "Assets/Scenes/Gameplay/LevelTest/level_01_test.unity",
    };

    /// <summary>
    /// Troca a fonte padrão do TMP (usada pelas pistas do tabuleiro) e todos os textos das cenas para a Pixelify.
    /// Títulos (fontes de título antigas ou texto grande) ficam com a versão Bold.
    /// </summary>
    [MenuItem("Tools/Phase 3/Use Pixel Fonts Everywhere")]
    public static string ApplyEverywhere() {
        var text = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TextFontPath);
        var title = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFontPath);

        var settings = new SerializedObject(TMP_Settings.instance);
        settings.FindProperty("m_defaultFontAsset").objectReferenceValue = text;
        settings.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();

        int changed = 0;
        foreach (string path in Scenes) {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (TMP_Text t in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TMP_Text>(true))) {
                if (t.font == text || t.font == title) continue;
                bool isTitle = t.font != null && (t.font.name.Contains("title") || t.font.name.Contains("ARCADE")) || t.fontSize >= 60;
                t.font = isTitle ? title : text;
                t.fontStyle &= ~FontStyles.UpperCase;
                EditorUtility.SetDirty(t);
                changed++;
            }
            EditorSceneManager.SaveScene(scene);
        }
        return $"Pixelify aplicada: fonte padrão do TMP e {changed} textos nas cenas";
    }

    private static void Create(string ttfPath, string assetPath) {
        var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (font == null) throw new System.IO.FileNotFoundException(ttfPath);

        // Já existe: não recria, para não trocar o GUID que as cenas referenciam. Apague o asset para refazer.
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null) return;

        // Raster sem suavização: os glifos ficam pixelados como o resto da arte.
        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 48, 4, GlyphRenderMode.RASTER_HINTED,
                                                            512, 512, AtlasPopulationMode.Dynamic, false);
        asset.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        if (!asset.TryAddCharacters(Characters, out string missing) && !string.IsNullOrEmpty(missing)) {
            Debug.LogWarning($"{asset.name}: sem glifo para '{missing}'");
        }
        asset.atlasPopulationMode = AtlasPopulationMode.Static;

        AssetDatabase.CreateAsset(asset, assetPath);
        asset.atlasTexture.name = asset.name + " Atlas";
        asset.atlasTexture.filterMode = FilterMode.Point;
        asset.material.name = asset.name + " Material";
        AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        EditorUtility.SetDirty(asset);
    }
}
