using System.Text;
using MonstersAndSoldiers.Core;
using TMPro;
using UnityEngine;

/// <summary>
/// Legenda de controles do painel esquerdo (GDD 8.3). Só desenha: o texto vem de <see cref="ControlsLegend"/>
/// e o dispositivo atual vem do <see cref="InputController"/>. Montada por Tools > Phase 4 > Add Controls Legend.
/// Os ícones (mouse, Xbox e PlayStation) são sprites do TMP_SpriteAsset gerado por Tools > Art > Build Input Icons.
/// </summary>
public class ControlsLegendView : MonoBehaviour {
    /// <summary>Caminho em Resources do sprite asset dos ícones de controle.</summary>
    public const string IconsResourcePath = "UI/InputIcons";

    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Color keyColor = new Color32(0xFF, 0xD0, 0x5A, 0xFF);
    [Tooltip("Vazio: carrega Resources/" + IconsResourcePath)]
    [SerializeField] private TMP_SpriteAsset icons;

    private InputController input;

    private void Start() {
        if (icons == null) icons = Resources.Load<TMP_SpriteAsset>(IconsResourcePath);
        if (text != null && icons != null) text.spriteAsset = icons;

        input = FindAnyObjectByType<InputController>();
        if (input != null) input.SchemeChanged += Show;
        Show(input != null ? input.Scheme : ControlScheme.KeyboardMouse);
    }

    private void OnDestroy() {
        if (input != null) input.SchemeChanged -= Show;
    }

    private void Show(ControlScheme scheme) {
        if (text == null) return;
        GamepadStyle style = input != null ? input.PadStyle : GamepadStyle.Xbox;
        string color = ColorUtility.ToHtmlStringRGB(keyColor);
        var sb = new StringBuilder();
        foreach (ControlHint hint in ControlsLegend.For(scheme)) {
            if (sb.Length > 0) sb.Append('\n');
            string sprite = HasIcon(hint, style, out string name) ? $"<sprite name=\"{name}\"> " : null;
            sb.Append(sprite);
            // No controle o ícone já é o botão; no mouse ele acompanha o nome do botão.
            if (sprite == null || scheme == ControlScheme.KeyboardMouse) {
                sb.Append("<color=#").Append(color).Append('>').Append(hint.Keys).Append("</color>  ");
            }
            sb.Append(hint.Action);
        }
        text.text = sb.ToString();
    }

    private bool HasIcon(ControlHint hint, GamepadStyle style, out string name) {
        name = ControlsLegend.SpriteName(hint.Icon, style);
        if (name == null || icons == null) return false;
        return icons.GetSpriteIndexFromName(name) >= 0;
    }
}
