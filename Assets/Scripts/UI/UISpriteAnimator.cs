using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Anima uma <see cref="Image"/> de UI trocando sprites em loop (os clips dos personagens animam
/// SpriteRenderer, que não existe no Canvas). Usa tempo real, então anda mesmo com o jogo pausado.
/// </summary>
[RequireComponent(typeof(Image))]
public class UISpriteAnimator : MonoBehaviour {
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 6f;
    [Tooltip("Começa num quadro diferente para personagens lado a lado não respirarem juntos")]
    [SerializeField] private int startFrame;

    private Image image;
    private float startTime;

    private void Awake() {
        image = GetComponent<Image>();
    }

    private void OnEnable() {
        startTime = Time.unscaledTime;
    }

    private void Update() {
        if (frames == null || frames.Length == 0 || framesPerSecond <= 0f) return;
        int index = (startFrame + (int)((Time.unscaledTime - startTime) * framesPerSecond)) % frames.Length;
        image.sprite = frames[index];
    }
}
