using DG.Tweening;
using UnityEngine;

/// <summary>
/// "Juice" do ataque (Fase 4): tremida do tabuleiro e poeira de pixels quando um monstro morre.
/// PROVISÓRIO: quadrados gerados por código, até existir arte de partículas na paleta final.
/// </summary>
public static class HitFx {
    private static readonly Color[] DustColors = {
        new Color32(0xC8, 0xB0, 0x8A, 0xFF),
        new Color32(0x9C, 0x84, 0x64, 0xFF),
        new Color32(0xF4, 0xE9, 0xD0, 0xFF)
    };

    private static Sprite pixel;

    /// <summary>Tremida curta no tabuleiro atual. Mais forte no acerto, mais fraca no erro.</summary>
    public static void Shake(bool strong) {
        GridController controller = GridController.Instance;
        if (controller == null || controller.CurrentGrid == null || controller.IsTransitioning) return;

        Transform grid = controller.CurrentGrid.transform;
        grid.DOComplete(); // não acumula com uma tremida anterior
        grid.DOShakePosition(strong ? 0.25f : 0.15f, strong ? 0.12f : 0.05f, 20, 90f, false, true)
            .SetLink(grid.gameObject);
    }

    /// <summary>Explosão de poeira em <paramref name="position"/>; <paramref name="size"/> é o tamanho do personagem.</summary>
    public static void Dust(Vector3 position, float size, int sortingLayerId, int sortingOrder) {
        float px = Mathf.Max(0.02f, size * 0.08f);
        for (int i = 0; i < 14; i++) {
            var go = new GameObject("DustPixel");
            go.transform.position = position;
            go.transform.localScale = Vector3.one * px;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Pixel;
            sr.color = DustColors[Random.Range(0, DustColors.Length)];
            sr.sortingLayerID = sortingLayerId;
            sr.sortingOrder = sortingOrder + 1;

            Vector2 dir = Random.insideUnitCircle.normalized;
            float distance = size * Random.Range(0.3f, 0.6f);
            float time = Random.Range(0.35f, 0.6f);
            Vector3 target = position + (Vector3)(dir * distance) + Vector3.up * size * 0.15f;

            DOTween.Sequence()
                .Join(go.transform.DOMove(target, time).SetEase(Ease.OutQuad))
                .Join(sr.DOFade(0f, time).SetEase(Ease.InQuad))
                .SetLink(go)
                .OnComplete(() => Object.Destroy(go));
        }
    }

    private static Sprite Pixel {
        get {
            if (pixel != null) return pixel;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) {
                filterMode = FilterMode.Point,
                name = "DustPixel"
            };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return pixel;
        }
    }
}
