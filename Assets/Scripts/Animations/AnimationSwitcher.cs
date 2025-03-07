using UnityEngine;

public class AnimationSwitcher : MonoBehaviour
{
    [Header("Animation Controllers")]
    [SerializeField] private RuntimeAnimatorController walkController;
    [SerializeField] private RuntimeAnimatorController attackController;

    private Animator animator;

    void Awake() {
        animator = GetComponent<Animator>();
        SetAttackController(); // Estado inicial
    }

    public void SetWalkController() {
        animator.runtimeAnimatorController = walkController;
    }

    public void SetAttackController() {
        animator.runtimeAnimatorController = attackController;
    }

    public void OnAttackFinished() {
        SetWalkController();
    }
}
