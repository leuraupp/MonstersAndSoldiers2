using System;
using System.Collections;
using DG.Tweening;
using MonstersAndSoldiers.Core;
using UnityEngine;

/// <summary>O que o personagem faz depois de atacar.</summary>
public enum AttackReaction { None, Hit, Death }

/// <summary>
/// Toca os estados "{Prefixo}_{Idle|Attack|Hit|Death}_{Direção}" do Animator.
/// A arte original (Tools/art) põe tudo num controller só; controllers antigos com "Walk" e um
/// controller de ataque separado continuam funcionando, e sem estado cai para o trigger de direção.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimationSwitcher : MonoBehaviour {
    [Header("Animation Controllers")]
    [SerializeField] private RuntimeAnimatorController walkController;
    [Tooltip("Opcional: controllers antigos tinham o ataque separado")]
    [SerializeField] private RuntimeAnimatorController attackController;

    [Header("Animation Settings")]
    [Tooltip("Duração do ataque quando não há clip de ataque para medir")]
    [SerializeField] private float attackAnimationDuration = 1.2f;
    [Tooltip("Pausa depois de cada clip, para a leitura do golpe")]
    [SerializeField] private float holdAfterClip = 0.25f;
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
        PlayIdle();
    }

    /// <summary>Toca o ataque e chama <paramref name="onComplete"/> uma única vez ao final.</summary>
    public void PlayAttack(bool success, Action onComplete) => PlayAttack(success, AttackReaction.None, onComplete);

    /// <summary>Ataque seguido de <paramref name="reaction"/>; quem morre fica no último quadro.</summary>
    public void PlayAttack(bool success, AttackReaction reaction, Action onComplete) {
        StopAttack();
        attackRoutine = StartCoroutine(AttackRoutine(success, reaction, onComplete));
    }

    private IEnumerator AttackRoutine(bool success, AttackReaction reaction, Action onComplete) {
        if (attackController != null) animator.runtimeAnimatorController = attackController;

        float duration = attackAnimationDuration;
        if (TryPlay("Attack", out float clipLength)) {
            duration = clipLength + holdAfterClip;
        } else {
            if (attackController != null) PlayState("Attack");
            // Sem clip de ataque: o "pulo" de escala é o único retorno visual.
            float scale = success ? attackSuccessScale : attackFailScale;
            scaleTween = transform.DOScale(originalScale * scale, attackAnimationDuration * 0.3f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.InOutQuad);
        }
        yield return new WaitForSeconds(duration);

        bool dead = false;
        if (reaction != AttackReaction.None) {
            if (animator.runtimeAnimatorController != walkController) animator.runtimeAnimatorController = walkController;
            if (TryPlay(reaction.ToString(), out float reactionLength)) {
                dead = reaction == AttackReaction.Death;
                yield return new WaitForSeconds(reactionLength + holdAfterClip);
            }
        }

        attackRoutine = null;
        scaleTween?.Kill();
        transform.localScale = originalScale;
        if (!dead) PlayIdle();
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

    private void PlayIdle() {
        if (animator == null || !gameObject.activeInHierarchy) return;
        if (animator.runtimeAnimatorController != walkController) {
            animator.runtimeAnimatorController = walkController;
        }
        if (TryPlay("Idle", out _) || TryPlay("Walk", out _)) return;
        PlayState("Walk");
    }

    /// <summary>Toca "{Prefix}_{action}_{Direction}" se existir e devolve a duração do clip.</summary>
    private bool TryPlay(string action, out float length) {
        length = 0f;
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller == null) return false;

        string state = $"{Prefix}_{action}_{Direction}";
        int hash = Animator.StringToHash(state);
        if (!animator.HasState(0, hash)) return false;

        animator.Play(hash, 0, 0f);
        foreach (AnimationClip clip in controller.animationClips) {
            if (clip.name == state) {
                length = clip.length;
                break;
            }
        }
        return true;
    }

    private void PlayState(string action) {
        if (animator.runtimeAnimatorController == null) return;
        if (TryPlay(action, out _)) return;

        // Fallback: os controllers antigos têm triggers Up/Down/Left/Right.
        foreach (FaceDirection d in (FaceDirection[])Enum.GetValues(typeof(FaceDirection))) {
            animator.ResetTrigger(d.ToString());
        }
        animator.SetTrigger(Direction.ToString());
    }
}
