using System.Collections;
using UnityEngine;

public enum State {
    Walk,
    Attack
}

public class AnimationSwitcher : MonoBehaviour
{
    [Header("Animation Controllers")]
    [SerializeField] private RuntimeAnimatorController walkController;
    [SerializeField] private RuntimeAnimatorController attackController;

    private Animator animator;
    private State currentState;
    private string currentCellType;
    private string currentDirection;

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

    public void AnimationPlay(string animationType, string cellType, string direction) {
        Debug.Log(cellType + "_" + animationType + "_" + direction);
        currentCellType = cellType;
        currentDirection = direction;
        if (animationType == State.Attack.ToString()) {
            SetAttackController();
            StartCoroutine(OnAttackFinished());
        } else if (animationType == State.Walk.ToString()) {
            SetWalkController();
        }

        animator.Play(currentCellType + "_" + currentState.ToString() + "_" + currentDirection, 0, 0f);
    }

    IEnumerator OnAttackFinished() {
        yield return new WaitForSeconds(0.8f);
        AnimationPlay("Walk", currentCellType, currentDirection);
    }
}
