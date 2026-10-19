using System;
using UnityEngine;

/// <summary>Efeitos sonoros do jogo (GDD 9).</summary>
public enum Sfx {
    CursorMove,
    PlaceSoldier,
    PlaceGrass,
    ClearCell,
    MonsterClick,
    Error,
    Attack,
    MonsterDeath,
    Victory,
    UiClick
}

public enum MusicTrack {
    None,
    Menu,
    Match
}

/// <summary>
/// Catálogo de áudio carregado de Resources pelo <see cref="AudioService"/>.
/// Entradas sem clip ficam em silêncio, então dá para preencher aos poucos.
/// </summary>
[CreateAssetMenu(menuName = "Monsters & Soldiers/Sound Library", fileName = "SoundLibrary")]
public class SoundLibrary : ScriptableObject {
    public const string ResourcesPath = "Audio/SoundLibrary";

    [Serializable]
    public class SfxEntry {
        public Sfx id;
        [Tooltip("Um é sorteado a cada toque, para o som não cansar")]
        public AudioClip[] clips = Array.Empty<AudioClip>();
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Header("Música")]
    public AudioClip menuMusic;
    public AudioClip matchMusic;
    [Range(0f, 1f)] public float musicVolume = 0.7f;

    [Header("Efeitos")]
    public SfxEntry[] effects = Array.Empty<SfxEntry>();

    public AudioClip Music(MusicTrack track) {
        return track switch {
            MusicTrack.Menu => menuMusic,
            MusicTrack.Match => matchMusic,
            _ => null
        };
    }

    public SfxEntry Find(Sfx id) {
        foreach (SfxEntry entry in effects) {
            if (entry != null && entry.id == id) return entry;
        }
        return null;
    }

    // Uma entrada por efeito, para o Inspector já mostrar a lista completa.
    private void Reset() {
        var ids = (Sfx[])Enum.GetValues(typeof(Sfx));
        effects = new SfxEntry[ids.Length];
        for (int i = 0; i < ids.Length; i++) effects[i] = new SfxEntry { id = ids[i] };
    }
}
