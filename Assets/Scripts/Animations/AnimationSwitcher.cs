using System;
using System.Collections;
using DG.Tweening;
using MonstersAndSoldiers.Core;
using UnityEngine;

/// <summary>
/// Troca entre o controller de walk e o de attack de um personagem e toca o estado
/// "{Prefixo}_{Walk|Attack}_{Direção}". Se o estado não existir, cai para o trigger de direção.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimationSwitcher : MonoBehaviour {
    [Header("Animation Controllers")]
    [SerializeField] private RuntimeAnimatorController walkController;
    [SerializeField] private RuntimeAnimatorController attackController;

    [Header("Animation Settings")]
    [SerializeField] private float attackAnimationDuration = 1.2f;
    [SerializeField] private float attackSuccessScale = 1.2f;
    [SerializeField] private float attackFailScale = 0.8f;

    private Animator animator;
    private Vector3 originalScale;
    private Coroutine attackRoutine;
    private Tween scaleTween;

    public string Prefix { get; set; }
    public FaceDirection Direction { get; private set; } = FaceDirection.Down;

    private void Awake() {
        animator = GetComponent<Animator>();
        originalScale = transform.localScale;
        // Personagens sem controllers configurados usam o que já está no Animator.
        if (walkController == null) walkController = animator.runtimeAnimatorController;
    }

    private void OnDisable() {
        StopAttack();
    }

    private void OnDestroy() {
        scaleTween?.Kill();
    }

    public void Face(FaceDirection direction) {
        Direction = direction;
        if (attackRoutine != null) return; // a direção é aplicada quando o ataque terminar
        PlayWalk();
    }

    /// <summary>Toca o ataque e chama <paramref name="onComplete"/> uma única vez ao final.</summary>
    public void PlayAttack(bool success, Action onComplete) {
        StopAttack();
        attackRoutine = StartCoroutine(AttackRoutine(success, onComplete));
    }

    private IEnumerator AttackRoutine(bool success, Action onComplete) {
        if (attackController != null) {
            animator.runtimeAnimatorController = attackController;
            PlayState("Attack");
        }

        float scale = success ? attackSuccessScale : attackFailScale;
        scaleTween = transform.DOScale(originalScale * scale, attackAnimationDuration * 0.3f)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.InOutQuad);

        yield return new WaitForSeconds(attackAnimationDuration);

        attackRoutine = null;
        transform.localScale = originalScale;
        PlayWalk();
        onComplete?.Invoke();
    }

    private void StopAttack() {
        if (attackRoutine != null) {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        scaleTween?.Kill();
        if (animator != null) transform.localScale = originalScale;
    }

    private void PlayWalk() {
        if (animator == null || !gameObject.activeInHierarchy) return;
        if (animator.runtimeAnimatorController != walkController) {
            animator.runtimeAnimatorController = walkController;
        }
        PlayState("Walk");
    }

    private void PlayState(string action) {
        if (animator.runtimeAnimatorController == null) return;

        int hash = Animator.StringToHash($"{Prefix}_{action}_{Direction}");
        if (animator.HasState(0, hash)) {
            animator.Play(hash, 0, 0f);
            return;
        }

        // Fallback: os controllers têm triggers Up/Down/Left/Right.
        foreach (FaceDirection d in (FaceDirection[])Enum.GetValues(typeof(FaceDirection))) {
            animator.ResetTrigger(d.ToString());
        }
        animator.SetTrigger(Direction.ToString());
    }
}
