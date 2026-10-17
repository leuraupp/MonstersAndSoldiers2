using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vaga-lumes do menu: cria imagens pequenas que vagueiam (ruído de Perlin) e piscam dentro de uma área.
/// A área é dada em px do Canvas, relativa ao canto inferior esquerdo do pai.
/// </summary>
public class MenuFireflies : MonoBehaviour {
    [SerializeField] private Sprite sprite;
    [SerializeField] private int count = 14;
    [SerializeField] private Rect area = new Rect(0f, 40f, 1920f, 240f);
    [SerializeField] private float size = 20f;
    [SerializeField] private float wander = 60f;
    [SerializeField] private float speed = 0.08f;
    [SerializeField] private float pixelSnap = 4f;
    [SerializeField] private int seed = 7;

    private RectTransform[] flies;
    private Image[] images;
    private Vector2[] origins;
    private float[] phases;

    private void Start() {
        if (sprite == null || count <= 0) return;
        var random = new System.Random(seed);
        flies = new RectTransform[count];
        images = new Image[count];
        origins = new Vector2[count];
        phases = new float[count];
        for (int i = 0; i < count; i++) {
            var go = new GameObject($"Firefly{i}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.sizeDelta = new Vector2(size, size);
            origins[i] = new Vector2(area.x + (float)random.NextDouble() * area.width, area.y + (float)random.NextDouble() * area.height);
            phases[i] = (float)random.NextDouble() * 100f;
            flies[i] = rect;
            images[i] = image;
        }
        Update();
    }

    private void Update() {
        if (flies == null) return;
        float t = Time.unscaledTime;
        for (int i = 0; i < flies.Length; i++) {
            float p = phases[i];
            var drift = new Vector2(Mathf.PerlinNoise(p, t * speed) - 0.5f, Mathf.PerlinNoise(t * speed, p) - 0.5f) * (2f * wander);
            Vector2 pos = origins[i] + drift;
            if (pixelSnap > 0f) pos = new Vector2(Mathf.Round(pos.x / pixelSnap), Mathf.Round(pos.y / pixelSnap)) * pixelSnap;
            flies[i].anchoredPosition = pos;
            // Pisca devagar e passa boa parte do tempo apagado, como vaga-lume de verdade.
            float glow = Mathf.Clamp01(Mathf.Sin(t * 1.3f + p * 7f) * 1.6f - 0.2f);
            images[i].color = new Color(1f, 1f, 1f, glow);
        }
    }
}
