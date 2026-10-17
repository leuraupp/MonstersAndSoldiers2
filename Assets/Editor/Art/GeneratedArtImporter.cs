using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Import settings da arte original gerada por Tools/art (pixel art): Point, sem compressão,
/// sprite sheets fatiados por quadro. Personagens em Assets/Art/Characters, UI em Assets/Art/UI/Original.
/// </summary>
public class GeneratedArtImporter : AssetPostprocessor {
    public const string CharactersRoot = "Assets/Art/Characters/";
    public const string BoardRoot = "Assets/Art/Board/Original/"; // enfeites da casa (grama), quadros de 32 como os personagens
    public const string UIRoot = "Assets/Art/UI/Original/";

    public const int CharacterFrame = 32;
    public const int CharacterPPU = 32;     // 1 quadro = 1 unidade = 1 casa
    private const int UIPPU = 25;           // Canvas usa 100 px/unidade: "Set Native Size" deixa a UI em 4x

    // Quadros e bordas 9-slice da UI (left, bottom, right, top).
    private static readonly Dictionary<string, (int w, int h, Vector4 border)> UISheets = new() {
        { "Button_Blank", (72, 20, new Vector4(4, 5, 4, 4)) },
        { "Button_Jogar", (72, 20, Vector4.zero) },
        { "Button_Opcoes", (72, 20, Vector4.zero) },
        { "Button_Creditos", (72, 20, Vector4.zero) },
        { "Button_Sair", (72, 20, Vector4.zero) },
        { "Button_Atacar", (72, 20, Vector4.zero) },
        { "Button_Pausa", (72, 20, Vector4.zero) },
        { "Button_Pause_Icon", (20, 20, Vector4.zero) },
        { "MenuButton_Jogar", (96, 24, Vector4.zero) },
        { "MenuButton_Opcoes", (96, 24, Vector4.zero) },
        { "MenuButton_Creditos", (96, 24, Vector4.zero) },
        { "MenuButton_Sair", (96, 24, Vector4.zero) },
        { "Title_Flag", (8, 5, Vector4.zero) },
        { "Title_Twinkle", (7, 7, Vector4.zero) },
    };
    private static readonly Dictionary<string, Vector4> SingleBorders = new() {
        { "Panel", new Vector4(8, 8, 8, 8) },
    };

    private static bool IsCharacter(string path) =>
        (path.StartsWith(CharactersRoot) || path.StartsWith(BoardRoot)) && path.EndsWith(".png");
    private static bool IsUI(string path) => path.StartsWith(UIRoot) && path.EndsWith(".png");

    private void OnPreprocessTexture() {
        if (!IsCharacter(assetPath) && !IsUI(assetPath)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;

        string name = Path.GetFileNameWithoutExtension(assetPath);
        importer.spritePixelsPerUnit = IsCharacter(assetPath) ? CharacterPPU : UIPPU;

        if (IsCharacter(assetPath)) {
            Slice(importer, name, CharacterFrame, CharacterFrame, Vector4.zero);
        } else if (UISheets.TryGetValue(name, out var sheet)) {
            Slice(importer, name, sheet.w, sheet.h, sheet.border);
        } else {
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            importer.SetTextureSettings(settings);
            importer.spriteBorder = SingleBorders.TryGetValue(name, out Vector4 b) ? b : Vector4.zero;
        }
    }

    /// <summary>Fatia a folha em quadros horizontais "{nome}_{i}", mantendo os IDs já existentes.</summary>
    private void Slice(TextureImporter importer, string name, int frameW, int frameH, Vector4 border) {
        importer.spriteImportMode = SpriteImportMode.Multiple;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteExtrude = 0;
        importer.SetTextureSettings(settings);

        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        int count = Mathf.Max(1, width / frameW);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        Dictionary<string, SpriteRect> existing = provider.GetSpriteRects().ToDictionary(r => r.name);
        var rects = new List<SpriteRect>();
        for (int i = 0; i < count; i++) {
            string spriteName = $"{name}_{i}";
            SpriteRect rect = existing.TryGetValue(spriteName, out SpriteRect old) ? old : new SpriteRect { name = spriteName, spriteID = GUID.Generate() };
            rect.rect = new Rect(i * frameW, height - frameH, frameW, frameH);
            rect.alignment = SpriteAlignment.Center;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.border = border;
            rects.Add(rect);
        }
        provider.SetSpriteRects(rects.ToArray());

        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (names != null) {
            names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
        }
        provider.Apply();
    }
}
