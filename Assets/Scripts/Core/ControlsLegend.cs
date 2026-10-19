using System.Collections.Generic;

namespace MonstersAndSoldiers.Core {
    /// <summary>Último tipo de dispositivo usado; decide qual legenda aparece (GDD 8.3).</summary>
    public enum ControlScheme {
        KeyboardMouse,
        Gamepad
    }

    /// <summary>Uma linha da legenda: as teclas e o que elas fazem.</summary>
    public readonly struct ControlHint {
        public readonly string Keys;
        public readonly string Action;

        public ControlHint(string keys, string action) {
            Keys = keys;
            Action = action;
        }
    }

    /// <summary>Texto da legenda de controles da partida, igual aos controles da GDD 4.2.</summary>
    public static class ControlsLegend {
        private static readonly ControlHint[] KeyboardMouse = {
            new ControlHint("Botão esquerdo", "soldado"),
            new ControlHint("Botão direito", "grama"),
            new ControlHint("Arrastar", "várias casas"),
            new ControlHint("Setas / WASD", "mover"),
            new ControlHint("Espaço / Enter", "atacar"),
            new ControlHint("ESC", "pausa")
        };

        private static readonly ControlHint[] Gamepad = {
            new ControlHint("A / X", "soldado"),
            new ControlHint("B / O", "grama"),
            new ControlHint("Analógico / D-pad", "mover"),
            new ControlHint("Y / Triângulo", "atacar"),
            new ControlHint("Start / Options", "pausa")
        };

        public static IReadOnlyList<ControlHint> For(ControlScheme scheme) {
            return scheme == ControlScheme.Gamepad ? Gamepad : KeyboardMouse;
        }
    }
}
