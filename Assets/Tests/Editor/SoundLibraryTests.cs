using NUnit.Framework;
using UnityEngine;

/// <summary>Sorteio de música do <see cref="SoundLibrary"/>: menu e partida dividem faixas, mas a troca de tela troca a música.</summary>
public class SoundLibraryTests {
    private SoundLibrary library;
    private AudioClip a;
    private AudioClip b;

    [SetUp]
    public void SetUp() {
        a = AudioClip.Create("a", 100, 1, 44100, false);
        b = AudioClip.Create("b", 100, 1, 44100, false);
        library = ScriptableObject.CreateInstance<SoundLibrary>();
        library.menuMusic = new[] { a, b };
        library.matchMusic = new[] { a, b };
    }

    [TearDown]
    public void TearDown() {
        Object.DestroyImmediate(library);
        Object.DestroyImmediate(a);
        Object.DestroyImmediate(b);
    }

    [Test]
    public void PickMusic_AvoidsCurrentClip_WhenThereIsAnotherOption() {
        for (int i = 0; i < 50; i++) {
            Assert.AreSame(b, library.PickMusic(MusicTrack.Match, avoid: a));
            Assert.AreSame(a, library.PickMusic(MusicTrack.Menu, avoid: b));
        }
    }

    [Test]
    public void PickMusic_KeepsOnlyClip_WhenItIsTheOneToAvoid() {
        library.matchMusic = new[] { a };
        Assert.AreSame(a, library.PickMusic(MusicTrack.Match, avoid: a));
    }

    [Test]
    public void PickMusic_ReturnsNull_WithoutClips() {
        library.matchMusic = new AudioClip[0];
        Assert.IsNull(library.PickMusic(MusicTrack.Match, avoid: a));
        Assert.IsNull(library.PickMusic(MusicTrack.None));
    }
}
