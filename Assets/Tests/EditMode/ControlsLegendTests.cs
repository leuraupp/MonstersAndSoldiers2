using System.Linq;
using MonstersAndSoldiers.Core;
using NUnit.Framework;

namespace MonstersAndSoldiers.Tests {
    public class ControlsLegendTests {
        // As ações que a GDD 4.2 lista para os dois dispositivos.
        private static readonly string[] SharedActions = { "soldado", "grama", "mover", "atacar", "pausa" };

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
            Assert.AreEqual("Botão esquerdo", Keys(hints, "soldado"));
            Assert.AreEqual("Botão direito", Keys(hints, "grama"));
            StringAssert.Contains("Espaço", Keys(hints, "atacar"));
            Assert.AreEqual("ESC", Keys(hints, "pausa"));
        }

        [Test]
        public void Gamepad_UsesFaceButtons() {
            var hints = ControlsLegend.For(ControlScheme.Gamepad);
            StringAssert.StartsWith("A", Keys(hints, "soldado"));
            StringAssert.StartsWith("B", Keys(hints, "grama"));
            StringAssert.StartsWith("Y", Keys(hints, "atacar"));
            StringAssert.StartsWith("Start", Keys(hints, "pausa"));
        }

        private static string Keys(System.Collections.Generic.IReadOnlyList<ControlHint> hints, string action) {
            return hints.First(h => h.Action == action).Keys;
        }
    }
}
