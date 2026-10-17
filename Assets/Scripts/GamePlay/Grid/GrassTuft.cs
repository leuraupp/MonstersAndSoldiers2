using UnityEngine;

/// <summary>
/// Tufo de grama da casa marcada com o botão direito: brota uma vez ao aparecer e depois balança ao vento.
/// Cada casa começa o balanço numa fase diferente, para o tabuleiro não balançar em uníssono.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GrassTuft : MonoBehaviour {
    [SerializeField] private Sprite[] growFrames;
    [SerializeField] private Sprite[] swayFrames;
    [SerializeField] private float growFps = 16f;
    [SerializeField] private float swayFps = 6f;

    private SpriteRenderer spriteRenderer;
    private float startTime;
    private int swayOffset;

    private void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable() {
        startTime = Time.time;
        Vector3 p = transform.position;
        swayOffset = Mathf.Abs(Mathf.RoundToInt(p.x * 3f + p.y * 5f));
        Update();
    }

    private void Update() {
        float elapsed = Time.time - startTime;
        int growCount = growFrames?.Length ?? 0;
        int grown = (int)(elapsed * growFps);
        if (grown < growCount) {
            spriteRenderer.sprite = growFrames[grown];
            return;
        }
        if (swayFrames == null || swayFrames.Length == 0) return;
        float swayTime = elapsed - growCount / growFps;
        spriteRenderer.sprite = swayFrames[(swayOffset + (int)(swayTime * swayFps)) % swayFrames.Length];
    }
}
