using System.Collections.Generic;
using MonstersAndSoldiers.Core;
using NUnit.Framework;

namespace MonstersAndSoldiers.Tests {
    public class MatchSessionTests {
        [Test]
        public void Time_RunsOnlyWhilePlaying() {
            var s = new MatchSession();
            s.Tick(2);
            s.Pause();
            s.Tick(5);
            s.Resume();
            s.Tick(1);

            Assert.AreEqual(3, s.ElapsedSeconds, 1e-9);
        }

        [Test]
        public void Attack_CountsAttempts_AndStopsClock() {
            var s = new MatchSession();
            Assert.IsTrue(s.BeginAttack());
            s.Tick(4);
            Assert.IsFalse(s.BeginAttack(), "Não dá para atacar durante o ataque.");
            s.EndAttack(false);
            s.BeginAttack();
            s.EndAttack(true);
            s.Tick(10);

            Assert.AreEqual(2, s.Attempts);
            Assert.AreEqual(MatchState.Won, s.State);
            Assert.AreEqual(0, s.ElapsedSeconds, 1e-9);
        }

        [Test]
        public void PauseDuringAttack_ResumesToAttackResult() {
            var s = new MatchSession();
            s.BeginAttack();
            s.Pause();
            s.EndAttack(true);
            Assert.AreEqual(MatchState.Paused, s.State);

            s.Resume();
            Assert.AreEqual(MatchState.Won, s.State);
        }

        [Test]
        public void CannotPauseAfterWin() {
            var s = new MatchSession();
            s.BeginAttack();
            s.EndAttack(true);
            Assert.IsFalse(s.Pause());
        }

        [Test]
        public void Reset_ClearsTimeAndAttempts() {
            var s = new MatchSession();
            s.Tick(7);
            s.BeginAttack();
            s.Pause();
            s.Reset();

            Assert.AreEqual(MatchState.Playing, s.State);
            Assert.AreEqual(0, s.Attempts);
            Assert.AreEqual(0, s.ElapsedSeconds, 1e-9);
        }

        [TestCase(0, "00:00")]
        [TestCase(59.9, "00:59")]
        [TestCase(61, "01:01")]
        [TestCase(3600, "60:00")]
        public void FormatTime(double seconds, string expected) {
            Assert.AreEqual(expected, MatchSession.FormatTime(seconds));
        }
    }

    public class SaveDataTests {
        private class MemoryStore : IKeyValueStore {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public int Saves;
            public bool HasKey(string key) => Values.ContainsKey(key);
            public string GetString(string key, string fallback) => Values.TryGetValue(key, out string v) ? v : fallback;
            public void SetString(string key, string value) => Values[key] = value;
            public void Save() => Saves++;
        }

        [Test]
        public void BestTimes_KeepsOnlyRecords() {
            var store = new MemoryStore();
            var best = new BestTimes(store);
            string key = BestTimes.KeyFor(Difficulty.Medium);

            Assert.IsNull(best.Get(key));
            Assert.IsTrue(best.Submit(key, 90.5));
            Assert.IsFalse(best.Submit(key, 120));
            Assert.IsFalse(best.Submit(key, 90.5), "Empate não é recorde.");
            Assert.IsTrue(best.Submit(key, 45));
            Assert.AreEqual(45, best.Get(key).Value, 1e-9);
            Assert.IsNull(best.Get(BestTimes.KeyFor(Difficulty.Hard)), "Cada dificuldade tem o próprio recorde.");
        }

        [Test]
        public void Options_RoundTripThroughStore() {
            var store = new MemoryStore();
            var options = new GameOptions(store) {
                MusicVolume = 0.25f, SfxVolume = 1.5f, Fullscreen = false, Resolution = "1280x720"
            };
            options.Save();

            var loaded = new GameOptions(store);
            Assert.AreEqual(0.25f, loaded.MusicVolume, 1e-6);
            Assert.AreEqual(1f, loaded.SfxVolume, 1e-6, "Volume fica entre 0 e 1.");
            Assert.IsFalse(loaded.Fullscreen);
            Assert.AreEqual("1280x720", loaded.Resolution);
            Assert.AreEqual(1, store.Saves);
        }

        [Test]
        public void Options_DefaultsWhenStoreIsEmpty() {
            var options = new GameOptions(new MemoryStore());
            Assert.AreEqual(0.8f, options.MusicVolume, 1e-6);
            Assert.IsTrue(options.Fullscreen);
            Assert.AreEqual("", options.Resolution);
        }

        [TestCase("1920x1080", true, 1920, 1080)]
        [TestCase("", false, 0, 0)]
        [TestCase("abc", false, 0, 0)]
        [TestCase("0x720", false, 0, 720)]
        public void ParseResolution(string text, bool ok, int w, int h) {
            Assert.AreEqual(ok, GameOptions.TryParseResolution(text, out int width, out int height));
            if (ok) {
                Assert.AreEqual(w, width);
                Assert.AreEqual(h, height);
            }
        }
    }
}
