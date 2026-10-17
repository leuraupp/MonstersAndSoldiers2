using UnityEngine;

/// <summary>Move um elemento do menu na horizontal em velocidade constante e volta ao início ao sair (nuvens).</summary>
[RequireComponent(typeof(RectTransform))]
public class MenuDrift : MonoBehaviour {
    [Tooltip("Velocidade em px do Canvas por segundo (negativo = para a esquerda)")]
    [SerializeField] private float speed = 12f;
    [Tooltip("Faixa de x (anchoredPosition) em que o elemento circula")]
    [SerializeField] private float minX = -200f;
    [SerializeField] private float maxX = 2120f;
    [SerializeField] private float pixelSnap = 4f;

    private RectTransform rect;
    private float x;

    private void Awake() {
        rect = (RectTransform)transform;
        x = rect.anchoredPosition.x;
    }

    private void Update() {
        float span = maxX - minX;
        if (span <= 0f) return;
        x += speed * Time.unscaledDeltaTime;
        if (x > maxX) x -= span;
        if (x < minX) x += span;
        Vector2 p = rect.anchoredPosition;
        p.x = pixelSnap > 0f ? Mathf.Round(x / pixelSnap) * pixelSnap : x;
        rect.anchoredPosition = p;
    }
}
