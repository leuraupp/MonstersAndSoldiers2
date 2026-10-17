using System;
using MonstersAndSoldiers.Core;
using UnityEngine;

/// <summary>
/// Opções do jogador (GDD 8.4) carregadas uma vez por sessão e aplicadas na tela.
/// Os volumes ficam disponíveis para o áudio da Fase 4 via <see cref="Changed"/>.
/// </summary>
public static class OptionsService {
    private static GameOptions options;

    public static GameOptions Options => options ??= new GameOptions(new PlayerPrefsStore());

    /// <summary>Disparado depois de salvar, para quem toca som reajustar o volume.</summary>
    public static event Action<GameOptions> Changed;

    /// <summary>Resolução e tela cheia só existem no desktop (GDD 8.4).</summary>
    public static bool SupportsScreenSettings => Application.platform != RuntimePlatform.WebGLPlayer && !Application.isMobilePlatform;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyOnStartup() {
        options = null; // recarrega a cada Play no Editor
        ApplyScreen();
    }

    public static void Save() {
        Options.Save();
        ApplyScreen();
        Changed?.Invoke(Options);
    }

    private static void ApplyScreen() {
        if (!SupportsScreenSettings || Application.isEditor) return;
        FullScreenMode mode = Options.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        if (GameOptions.TryParseResolution(Options.Resolution, out int w, out int h)) {
            Screen.SetResolution(w, h, mode);
        } else {
            Screen.fullScreenMode = mode;
        }
    }
}
