using System.Collections.Generic;

namespace MonstersAndSoldiers.Core {
    /// <summary>Último tipo de dispositivo usado; decide qual legenda aparece (GDD 8.3).</summary>
    public enum ControlScheme {
        KeyboardMouse,
        Gamepad
    }

    /// <summary>Família do controle conectado; decide quais ícones de botão aparecem.</summary>
    public enum GamepadStyle {
        Xbox,
        PlayStation
    }

    /// <summary>
    /// Uma linha da legenda: as teclas, o que elas fazem e, quando existe, o ícone do botão.
    /// <see cref="Icon"/> é um id neutro ("mouse_left", "pad_south"...) traduzido por <see cref="ControlsLegend.SpriteName"/>.
    /// </summary>
    public readonly struct ControlHint {
        public readonly string Keys;
        public readonly string Action;
        public readonly string Icon;

        public ControlHint(string keys, string action, string icon = null) {
            Keys = keys;
            Action = action;
            Icon = icon;
        }
    }

    /// <summary>Texto da legenda de controles da partida, igual aos controles da GDD 4.2.</summary>
    public static class ControlsLegend {
        public const string MouseLeft = "mouse_left";
        public const string MouseRight = "mouse_right";
        public const string PadSouth = "pad_south";
        public const string PadEast = "pad_east";
        public const string PadWest = "pad_west";
        public const string PadNorth = "pad_north";
        public const string PadDpad = "pad_dpad";
        public const string PadStart = "pad_start";

        private static readonly ControlHint[] KeyboardMouse = {
            new ControlHint("Botão esquerdo", "adiciona soldado", MouseLeft),
            new ControlHint("Botão direito", "adiciona grama", MouseRight),
            new ControlHint("Arrastar", "várias casas"),
            new ControlHint("Setas / WASD", "mover"),
            new ControlHint("Espaço / Enter", "atacar"),
            new ControlHint("ESC", "pausa")
        };

        private static readonly ControlHint[] Gamepad = {
            new ControlHint("A / X", "adiciona soldado", PadSouth),
            new ControlHint("B / O", "adiciona grama", PadEast),
            new ControlHint("Analógico / D-pad", "mover", PadDpad),
            new ControlHint("Y / Triângulo", "atacar", PadNorth),
            new ControlHint("Start / Options", "pausa", PadStart)
        };

        public static IReadOnlyList<ControlHint> For(ControlScheme scheme) {
            return scheme == ControlScheme.Gamepad ? Gamepad : KeyboardMouse;
        }

        /// <summary>
        /// Nome do sprite (folha Art/UI/Input/InputIcons) para um id de ícone. Botões de controle
        /// mudam conforme a família: o botão de baixo é A no Xbox e Xis no PlayStation. Id desconhecido: null.
        /// </summary>
        public static string SpriteName(string icon, GamepadStyle style) {
            bool ps = style == GamepadStyle.PlayStation;
            switch (icon) {
                case MouseLeft: return "Mouse_Left";
                case MouseRight: return "Mouse_Right";
                case PadSouth: return ps ? "PS_Cross" : "Xbox_A";
                case PadEast: return ps ? "PS_Circle" : "Xbox_B";
                case PadWest: return ps ? "PS_Square" : "Xbox_X";
                case PadNorth: return ps ? "PS_Triangle" : "Xbox_Y";
                case PadDpad: return "Pad_DPad";
                case PadStart: return ps ? "PS_Options" : "Xbox_Menu";
                default: return null;
            }
        }
    }
}
