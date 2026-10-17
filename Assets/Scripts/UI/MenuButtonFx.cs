using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Leve "pulo" de escala nos botões do menu ao passar o mouse, selecionar e clicar.</summary>
public class MenuButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler,
    IPointerDownHandler, IPointerUpHandler {
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float duration = 0.12f;

    private bool hovered;
    // Só a escala: DOKill no transform mataria também a subida do MenuIntro e o botão ficaria fora do lugar.
    private Tween scaleTween;
    private bool selected;

    private void OnDisable() {
        scaleTween?.Kill();
        transform.localScale = Vector3.one;
        hovered = selected = false;
    }

    public void OnPointerEnter(PointerEventData eventData) { hovered = true; Refresh(); }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; Refresh(); }
    public void OnSelect(BaseEventData eventData) { selected = true; Refresh(); }
    public void OnDeselect(BaseEventData eventData) { selected = false; Refresh(); }
    public void OnPointerDown(PointerEventData eventData) { ScaleTo(0.97f); }
    public void OnPointerUp(PointerEventData eventData) { Refresh(); }

    private void Refresh() {
        ScaleTo(hovered || selected ? hoverScale : 1f);
    }

    private void ScaleTo(float scale) {
        scaleTween?.Kill();
        scaleTween = transform.DOScale(scale, duration).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
    }
}
