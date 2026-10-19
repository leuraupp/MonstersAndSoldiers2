using UnityEngine;

/// <summary>
/// Fundo da partida: o céu noturno do título, sempre cobrindo a visão da câmera ortográfica.
/// Só desenha; acompanha a câmera e a proporção da tela.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CameraBackdrop : MonoBehaviour {
    private SpriteRenderer spriteRenderer;
    private Camera cam;

    private void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate() {
        if (cam == null) cam = Camera.main;
        if (cam == null || !cam.orthographic || spriteRenderer.sprite == null) return;

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        Vector2 size = spriteRenderer.sprite.bounds.size;
        float scale = Mathf.Max(width / size.x, height / size.y);
        transform.localScale = new Vector3(scale, scale, 1f);

        Vector3 c = cam.transform.position;
        transform.position = new Vector3(c.x, c.y, transform.position.z);
    }
}
