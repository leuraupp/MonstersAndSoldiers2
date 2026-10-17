using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Desloca uma camada do fundo do menu conforme o ponteiro (parallax) e com um balanço lento.
/// O deslocamento anda em passos de 1 pixel da arte, para não borrar a pixel art.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MenuParallax : MonoBehaviour {
    [Tooltip("Deslocamento máximo, em px do Canvas, com o ponteiro na borda da tela")]
    [SerializeField] private float strength = 8f;
    [Tooltip("Balanço horizontal contínuo, em px do Canvas (0 = parado)")]
    [SerializeField] private float swayAmplitude;
    [SerializeField] private float swayPeriod = 14f;
    [Tooltip("Tamanho de 1 pixel da arte no Canvas")]
    [SerializeField] private float pixelSnap = 4f;

    private RectTransform rect;
    private Vector2 basePosition;
    private Vector2 current;

    private void Awake() {
        rect = (RectTransform)transform;
        basePosition = rect.anchoredPosition;
    }

    private void OnDisable() {
        rect.anchoredPosition = basePosition;
    }

    private void Update() {
        Vector2 target = -PointerOffset() * strength;
        if (swayAmplitude > 0f && swayPeriod > 0f) {
            target.x += Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI / swayPeriod) * swayAmplitude;
        }
        current = Vector2.Lerp(current, target, 1f - Mathf.Exp(-3f * Time.unscaledDeltaTime));
        Vector2 offset = pixelSnap > 0f
            ? new Vector2(Mathf.Round(current.x / pixelSnap), Mathf.Round(current.y / pixelSnap)) * pixelSnap
            : current;
        rect.anchoredPosition = basePosition + offset;
    }

    /// <summary>Ponteiro em [-1, 1] a partir do centro da tela; zero sem mouse.</summary>
    private static Vector2 PointerOffset() {
        Vector2 position;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return Vector2.zero;
        position = Mouse.current.position.ReadValue();
#else
        position = Input.mousePosition;
#endif
        if (Screen.width <= 0 || Screen.height <= 0) return Vector2.zero;
        var offset = new Vector2(position.x / Screen.width * 2f - 1f, position.y / Screen.height * 2f - 1f);
        return new Vector2(Mathf.Clamp(offset.x, -1f, 1f), Mathf.Clamp(offset.y, -1f, 1f));
    }
}
