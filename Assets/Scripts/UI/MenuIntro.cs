using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Entrada do menu principal: a tela clareia, o logo cai e quica, os botões sobem um de cada vez
/// e depois o logo fica flutuando. Tudo em tempo real (independe de Time.timeScale).
/// </summary>
public class MenuIntro : MonoBehaviour {
    [SerializeField] private RectTransform logo;
    [SerializeField] private RectTransform[] buttons;
    [SerializeField] private Image fade;
    [SerializeField] private float logoDrop = 700f;
    [SerializeField] private float floatAmount = 8f;
    [SerializeField] private float buttonRise = 40f;

    private void Start() {
        if (fade != null) {
            fade.gameObject.SetActive(true);
            fade.raycastTarget = false;
            Color c = fade.color;
            fade.color = new Color(c.r, c.g, c.b, 1f);
            fade.DOFade(0f, 0.6f).SetUpdate(true).SetLink(fade.gameObject)
                .OnComplete(() => fade.gameObject.SetActive(false));
        }

        if (logo != null) {
            Vector2 home = logo.anchoredPosition;
            logo.anchoredPosition = home + Vector2.up * logoDrop;
            DOTween.Sequence()
                .AppendInterval(0.15f)
                .Append(logo.DOAnchorPos(home, 1.1f).SetEase(Ease.OutBounce))
                .OnComplete(() => logo.DOAnchorPosY(home.y + floatAmount, 1.8f).SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(logo.gameObject))
                .SetUpdate(true).SetLink(logo.gameObject);
        }

        if (buttons == null) return;
        for (int i = 0; i < buttons.Length; i++) {
            RectTransform button = buttons[i];
            if (button == null) continue;
            if (!button.TryGetComponent(out CanvasGroup group)) group = button.gameObject.AddComponent<CanvasGroup>();
            Vector2 home = button.anchoredPosition;
            button.anchoredPosition = home + Vector2.down * buttonRise;
            group.alpha = 0f;
            group.interactable = false;
            float delay = 0.9f + i * 0.15f;
            button.DOAnchorPos(home, 0.45f).SetEase(Ease.OutBack).SetDelay(delay).SetUpdate(true).SetLink(button.gameObject);
            group.DOFade(1f, 0.3f).SetDelay(delay).SetUpdate(true).SetLink(button.gameObject)
                .OnComplete(() => group.interactable = true);
        }
    }
}
