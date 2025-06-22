using System.Collections;
using UnityEngine;
using System;
using DG.Tweening;

public enum State {
    Walk,
    Attack
}

public class AnimationSwitcher : MonoBehaviour
{
    public event Action OnAttackAnimationComplete;

    [Header("Animation Controllers")]
    [SerializeField] private RuntimeAnimatorController walkController;
    [SerializeField] private RuntimeAnimatorController attackController;

    [Header("Animation Settings")]
    [SerializeField] private float attackAnimationDuration = 1.2f;
    [SerializeField] private float attackSuccessScale = 1.2f;
    [SerializeField] private float attackFailScale = 0.8f;

    private Animator animator;
    private State currentState;
    private string currentCellType;
    private string currentDirection;
    private bool isAttackSuccessful;

    void Awake() {
        animator = GetComponent<Animator>();
        SetWalkController(); // Estado inicial
    }

    private void SetState(State state) {
        currentState = state;
    }

    public State GetCurrentState() {
        return currentState;
    }

    public void SetWalkController() {
        animator.runtimeAnimatorController = walkController;
        SetState(State.Walk);
    }

    public void SetAttackController() {
        animator.runtimeAnimatorController = attackController;
        SetState(State.Attack);
    }

    public void AnimationPlay(string animationType, string cellType, string direction, bool success = true) {
        Debug.Log(cellType + "_" + animationType + "_" + direction);
        currentCellType = cellType;
        currentDirection = direction;
        isAttackSuccessful = success;

        if (animationType == State.Attack.ToString()) {
            SetAttackController();
            StartCoroutine(OnAttackFinished());
        } else if (animationType == State.Walk.ToString()) {
            SetWalkController();
        }

        animator.Play(currentCellType + "_" + currentState.ToString() + "_" + currentDirection, 0, 0f);
    }

    IEnumerator OnAttackFinished() {
        // Efeito visual baseado no sucesso/falha
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = isAttackSuccessful ? 
            originalScale * attackSuccessScale : 
            originalScale * attackFailScale;

        // Anima a escala
        transform.DOScale(targetScale, attackAnimationDuration * 0.3f)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.InOutQuad);

        yield return new WaitForSeconds(attackAnimationDuration);
        
        // Notifica que a animação terminou
        OnAttackAnimationComplete?.Invoke();
        
        // Volta para a animação de walk
        AnimationPlay("Walk", currentCellType, currentDirection);
        transform.localScale = originalScale;
    }
}
