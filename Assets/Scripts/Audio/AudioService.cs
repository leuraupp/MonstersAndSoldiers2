using System.Collections.Generic;
using DG.Tweening;
using MonstersAndSoldiers.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Música e efeitos sonoros (GDD 9), com os volumes das opções (GDD 8.4).
/// Nasce sozinho antes da primeira cena e vive entre as cenas, então não precisa estar em nenhuma.
/// Os clips vêm de um <see cref="SoundLibrary"/> em Resources; sem ele o jogo roda em silêncio.
/// </summary>
public class AudioService : MonoBehaviour {
    private const float MusicFade = 0.6f;
    // Vários soldados ou monstros fazendo a mesma coisa no mesmo frame tocam um som só.
    private const float SameSfxInterval = 0.06f;

    public static AudioService Instance { get; private set; }

    private SoundLibrary library;
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private MusicTrack currentTrack = MusicTrack.None;
    private readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() {
        if (Instance != null) return;
        var go = new GameObject("AudioService");
        DontDestroyOnLoad(go);
        go.AddComponent<AudioService>();
    }

    /// <summary>Toca um efeito, se houver clip para ele.</summary>
    public static void Play(Sfx id) {
        if (Instance != null) Instance.PlaySfx(id);
    }

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        library = Resources.Load<SoundLibrary>(SoundLibrary.ResourcesPath);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        OptionsService.Changed += OnOptionsChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy() {
        if (Instance != this) return;
        Instance = null;
        OptionsService.Changed -= OnOptionsChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        musicSource.DOKill();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        HookButtons(scene);
        PlayMusic(TrackFor(scene));
    }

    // Partida tem tabuleiro; o resto é menu.
    private static MusicTrack TrackFor(Scene scene) {
        foreach (GameObject root in scene.GetRootGameObjects()) {
            if (root.GetComponentInChildren<GridController>(true) != null) return MusicTrack.Match;
        }
        return MusicTrack.Menu;
    }

    // Clique de botão em todas as telas, sem precisar mexer em cada cena ou prefab.
    private static void HookButtons(Scene scene) {
        foreach (GameObject root in scene.GetRootGameObjects()) {
            foreach (Button button in root.GetComponentsInChildren<Button>(true)) {
                button.onClick.AddListener(PlayUiClick);
            }
        }
    }

    private static void PlayUiClick() => Play(Sfx.UiClick);

    // ---------- Música ----------

    public void PlayMusic(MusicTrack track) {
        if (track == currentTrack) return;
        currentTrack = track;

        AudioClip clip = library != null ? library.Music(track) : null;
        musicSource.DOKill();
        if (clip == null) {
            musicSource.DOFade(0f, MusicFade).SetUpdate(true).OnComplete(musicSource.Stop);
            return;
        }

        float target = MusicVolume();
        Sequence fade = DOTween.Sequence().SetUpdate(true).SetTarget(musicSource);
        if (musicSource.isPlaying) fade.Append(musicSource.DOFade(0f, MusicFade));
        fade.AppendCallback(() => {
            musicSource.clip = clip;
            musicSource.volume = 0f;
            musicSource.Play();
        });
        fade.Append(musicSource.DOFade(target, MusicFade));
    }

    private float MusicVolume() {
        float baseVolume = library != null ? library.musicVolume : 1f;
        return baseVolume * OptionsService.Options.MusicVolume;
    }

    private void OnOptionsChanged(GameOptions options) {
        if (musicSource.clip == null) return;
        musicSource.DOKill();
        musicSource.volume = MusicVolume();
    }

    // ---------- Efeitos ----------

    private void PlaySfx(Sfx id) {
        SoundLibrary.SfxEntry entry = library != null ? library.Find(id) : null;
        if (entry == null || entry.clips == null || entry.clips.Length == 0) return;

        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(id, out float last) && now - last < SameSfxInterval) return;
        lastPlayed[id] = now;

        AudioClip clip = entry.clips[Random.Range(0, entry.clips.Length)];
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, entry.volume * OptionsService.Options.SfxVolume);
    }
}
