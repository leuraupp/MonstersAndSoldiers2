using System;
using System.Globalization;

namespace MonstersAndSoldiers.Core {
    /// <summary>Armazenamento chave/valor. No jogo é o PlayerPrefs; nos testes, um dicionário.</summary>
    public interface IKeyValueStore {
        bool HasKey(string key);
        string GetString(string key, string fallback);
        void SetString(string key, string value);
        void Save();
    }

    /// <summary>Melhores tempos por modo (GDD 8.2 e 8.4), salvos localmente.</summary>
    public class BestTimes {
        private readonly IKeyValueStore store;

        public BestTimes(IKeyValueStore store) {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public static string KeyFor(Difficulty difficulty) => $"best.{difficulty}";
        public const string TutorialKey = "best.Tutorial";

        public double? Get(string modeKey) {
            if (!store.HasKey(modeKey)) return null;
            return double.TryParse(store.GetString(modeKey, ""), NumberStyles.Float, CultureInfo.InvariantCulture, out double v)
                ? v : (double?)null;
        }

        /// <summary>Grava o tempo se for recorde. Devolve true quando bateu o recorde (ou é o primeiro).</summary>
        public bool Submit(string modeKey, double seconds) {
            double? best = Get(modeKey);
            if (best.HasValue && seconds >= best.Value) return false;
            store.SetString(modeKey, seconds.ToString("R", CultureInfo.InvariantCulture));
            store.Save();
            return true;
        }
    }

    /// <summary>Opções do jogador (GDD 8.4): volumes e tela. Salvas entre sessões.</summary>
    public class GameOptions {
        private const string MusicKey = "opt.music";
        private const string SfxKey = "opt.sfx";
        private const string FullscreenKey = "opt.fullscreen";
        private const string ResolutionKey = "opt.resolution";

        private readonly IKeyValueStore store;

        public float MusicVolume { get; set; } = 0.8f;
        public float SfxVolume { get; set; } = 0.8f;
        public bool Fullscreen { get; set; } = true;

        /// <summary>Resolução como "LARGURAxALTURA"; vazio = a atual do sistema.</summary>
        public string Resolution { get; set; } = "";

        public GameOptions(IKeyValueStore store) {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            Load();
        }

        public void Load() {
            MusicVolume = Clamp01(ReadFloat(MusicKey, MusicVolume));
            SfxVolume = Clamp01(ReadFloat(SfxKey, SfxVolume));
            Fullscreen = store.GetString(FullscreenKey, Fullscreen ? "1" : "0") == "1";
            Resolution = store.GetString(ResolutionKey, Resolution);
        }

        public void Save() {
            store.SetString(MusicKey, Clamp01(MusicVolume).ToString("R", CultureInfo.InvariantCulture));
            store.SetString(SfxKey, Clamp01(SfxVolume).ToString("R", CultureInfo.InvariantCulture));
            store.SetString(FullscreenKey, Fullscreen ? "1" : "0");
            store.SetString(ResolutionKey, Resolution ?? "");
            store.Save();
        }

        /// <summary>Lê "1920x1080". False se estiver vazio ou mal formado.</summary>
        public static bool TryParseResolution(string text, out int width, out int height) {
            width = height = 0;
            if (string.IsNullOrEmpty(text)) return false;
            string[] parts = text.Split('x');
            return parts.Length == 2
                && int.TryParse(parts[0], out width) && int.TryParse(parts[1], out height)
                && width > 0 && height > 0;
        }

        private float ReadFloat(string key, float fallback) {
            return float.TryParse(store.GetString(key, ""), NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
                ? v : fallback;
        }

        private static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
    }
}
