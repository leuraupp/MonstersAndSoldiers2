using System.Linq;
using MonstersAndSoldiers.Core;
using NUnit.Framework;

namespace MonstersAndSoldiers.Tests {
    public class ControlsLegendTests {
        // As ações que a GDD 4.2 lista para os dois dispositivos.
        private static readonly string[] SharedActions = { "adiciona soldado", "adiciona grama", "mover", "atacar", "pausa" };

        [TestCase(ControlScheme.KeyboardMouse)]
        [TestCase(ControlScheme.Gamepad)]
        public void EverySchemeExplainsTheSameActions(ControlScheme scheme) {
            string[] actions = ControlsLegend.For(scheme).Select(h => h.Action).ToArray();
            CollectionAssert.IsSubsetOf(SharedActions, actions);
            Assert.IsTrue(ControlsLegend.For(scheme).All(h => !string.IsNullOrWhiteSpace(h.Keys)));
        }

        [Test]
        public void KeyboardMouse_UsesMouseButtonsAndKeys() {
            var hints = ControlsLegend.For(ControlScheme.KeyboardMouse);
            Assert.AreEqual("Botão esquerdo", Keys(hints, "adiciona soldado"));
            Assert.AreEqual("Botão direito", Keys(hints, "adiciona grama"));
            StringAssert.Contains("Espaço", Keys(hints, "atacar"));
            Assert.AreEqual("ESC", Keys(hints, "pausa"));
        }

        [Test]
        public void KeyboardMouse_MouseButtonsHaveIcons() {
            var hints = ControlsLegend.For(ControlScheme.KeyboardMouse);
            Assert.AreEqual(ControlsLegend.MouseLeft, Hint(hints, "adiciona soldado").Icon);
            Assert.AreEqual(ControlsLegend.MouseRight, Hint(hints, "adiciona grama").Icon);
            Assert.IsNull(Hint(hints, "pausa").Icon);
        }

        [Test]
        public void Gamepad_UsesFaceButtons() {
            var hints = ControlsLegend.For(ControlScheme.Gamepad);
            StringAssert.StartsWith("A", Keys(hints, "adiciona soldado"));
            StringAssert.StartsWith("B", Keys(hints, "adiciona grama"));
            StringAssert.StartsWith("Y", Keys(hints, "atacar"));
            StringAssert.StartsWith("Start", Keys(hints, "pausa"));
        }

        [Test]
        public void Gamepad_EveryRowHasAnIconForBothFamilies() {
            foreach (ControlHint hint in ControlsLegend.For(ControlScheme.Gamepad)) {
                Assert.IsNotNull(ControlsLegend.SpriteName(hint.Icon, GamepadStyle.Xbox), hint.Action);
                Assert.IsNotNull(ControlsLegend.SpriteName(hint.Icon, GamepadStyle.PlayStation), hint.Action);
            }
        }

        // Os ícones seguem os bindings do InputController: sul = soldado, leste = grama, norte = atacar.
        [TestCase(GamepadStyle.Xbox, "Xbox_A", "Xbox_B", "Xbox_Y", "Xbox_Menu")]
        [TestCase(GamepadStyle.PlayStation, "PS_Cross", "PS_Circle", "PS_Triangle", "PS_Options")]
        public void SpriteName_FollowsTheControllerFamily(GamepadStyle style, string soldier, string grass, string attack, string pause) {
            var hints = ControlsLegend.For(ControlScheme.Gamepad);
            Assert.AreEqual(soldier, ControlsLegend.SpriteName(Hint(hints, "adiciona soldado").Icon, style));
            Assert.AreEqual(grass, ControlsLegend.SpriteName(Hint(hints, "adiciona grama").Icon, style));
            Assert.AreEqual(attack, ControlsLegend.SpriteName(Hint(hints, "atacar").Icon, style));
            Assert.AreEqual(pause, ControlsLegend.SpriteName(Hint(hints, "pausa").Icon, style));
        }

        [Test]
        public void SpriteName_UnknownIconIsNull() {
            Assert.IsNull(ControlsLegend.SpriteName(null, GamepadStyle.Xbox));
            Assert.IsNull(ControlsLegend.SpriteName("teclado", GamepadStyle.PlayStation));
        }

        private static ControlHint Hint(System.Collections.Generic.IReadOnlyList<ControlHint> hints, string action) {
            return hints.First(h => h.Action == action);
        }

        private static string Keys(System.Collections.Generic.IReadOnlyList<ControlHint> hints, string action) {
            return Hint(hints, action).Keys;
        }
    }
}
