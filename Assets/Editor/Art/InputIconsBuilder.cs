using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

/// <summary>
/// Gera Resources/UI/InputIcons.asset (TMP_SpriteAsset) a partir da folha Art/UI/Input/InputIcons.png,
/// para a <see cref="ControlsLegendView"/> escrever &lt;sprite name="Xbox_A"&gt; no texto da legenda.
/// Roda sozinho quando o asset ainda não existe; para refazer depois de mudar a folha: Tools > Art > Build Input Icons.
/// </summary>
[InitializeOnLoad]
public static class InputIconsBuilder {
    public const string AssetPath = "Assets/Resources/" + ControlsLegendView.IconsResourcePath + ".asset";
    private const float IconScale = 1.75f;

    static InputIconsBuilder() {
        EditorApplication.delayCall += () => {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(AssetPath) || !File.Exists(GeneratedArtImporter.InputIconsPath)) return;
            Debug.Log(Build());
        };
    }

    [MenuItem("Tools/Art/Build Input Icons")]
    private static void BuildFromMenu() => Debug.Log(Build());

    public static string Build() {
        string sheetPath = GeneratedArtImporter.InputIconsPath;
        AssetDatabase.ImportAsset(sheetPath, ImportAssetOptions.ForceUpdate);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
        if (texture == null) return $"Folha de ícones não encontrada em {sheetPath}.";

        Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath)
            .OfType<Sprite>()
            .ToDictionary(s => s.name);
        string[] missing = GeneratedArtImporter.InputIconNames.Where(n => !sprites.ContainsKey(n)).ToArray();
        if (missing.Length > 0) return "Ícones sem sprite na folha: " + string.Join(", ", missing);

        Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
        var asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(AssetPath);
        if (asset == null) {
            asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            AssetDatabase.CreateAsset(asset, AssetPath);
        }

        // Asset novo nasce sem versão e o TMP tentaria "migrar" tabelas antigas que não existem.
        var serialized = new SerializedObject(asset);
        SerializedProperty version = serialized.FindProperty("m_Version");
        if (version != null && version.stringValue != "1.1.0") {
            version.stringValue = "1.1.0";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        asset.spriteSheet = texture;
        asset.spriteGlyphTable.Clear();
        asset.spriteCharacterTable.Clear();

        int size = GeneratedArtImporter.InputIconSize;
        uint index = 0;
        foreach (string name in GeneratedArtImporter.InputIconNames) {
            Sprite sprite = sprites[name];
            // O TMP escala o sprite para a altura do ascendente da fonte; IconScale deixa o ícone maior que as letras,
            // e o bearing de 12 px desce o ícone para ele ficar centrado na linha.
            var glyph = new TMP_SpriteGlyph {
                index = index,
                sprite = sprite,
                scale = 1f,
                metrics = new GlyphMetrics(size, size, 0, size - 4, size + 2),
                glyphRect = new GlyphRect(sprite.rect)
            };
            asset.spriteGlyphTable.Add(glyph);
            asset.spriteCharacterTable.Add(new TMP_SpriteCharacter(0xFFFE, glyph) { name = name, scale = IconScale });
            index++;
        }

        if (asset.material == null) {
            var material = new Material(Shader.Find("TextMeshPro/Sprite")) { name = "InputIcons Material" };
            AssetDatabase.AddObjectToAsset(material, asset);
            asset.material = material;
        }
        asset.material.SetTexture(ShaderUtilities.ID_MainTex, texture);

        asset.UpdateLookupTables();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return $"Ícones de controle: {index} sprites em {AssetPath}.";
    }
}
