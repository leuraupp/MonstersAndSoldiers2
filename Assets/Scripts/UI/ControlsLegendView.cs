using System.Text;
using MonstersAndSoldiers.Core;
using TMPro;
using UnityEngine;

/// <summary>
/// Legenda de controles do painel esquerdo (GDD 8.3). Só desenha: o texto vem de <see cref="ControlsLegend"/>
/// e o dispositivo atual vem do <see cref="InputController"/>. Montada por Tools > Phase 4 > Add Controls Legend.
/// </summary>
public class ControlsLegendView : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Color keyColor = new Color32(0xFF, 0xD0, 0x5A, 0xFF);

    private InputController input;

    private void Start() {
        input = FindAnyObjectByType<InputController>();
        if (input != null) input.SchemeChanged += Show;
        Show(input != null ? input.Scheme : ControlScheme.KeyboardMouse);
    }

    private void OnDestroy() {
        if (input != null) input.SchemeChanged -= Show;
    }

    private void Show(ControlScheme scheme) {
        if (text == null) return;
        string color = ColorUtility.ToHtmlStringRGB(keyColor);
        var sb = new StringBuilder();
        foreach (ControlHint hint in ControlsLegend.For(scheme)) {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append("<color=#").Append(color).Append('>').Append(hint.Keys).Append("</color>  ").Append(hint.Action);
        }
        text.text = sb.ToString();
    }
}
