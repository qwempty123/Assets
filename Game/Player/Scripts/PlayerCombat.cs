
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private string attack1Trigger = "Attack1";
    [SerializeField] private string attack2Trigger = "Attack2";

    [Header("Combo Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float comboStartTime = 0.75f;

    [Header("Animator States")]
    [SerializeField] private string attack1StateName = "Attack1";
    [SerializeField] private string attack2StateName = "Attack2";

    private Animator animator;

    private bool attackInProgress;
    private bool attackStateEntered;
    private bool comboQueued;
    private bool attack2Triggered;

    private int currentAttack;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        HandleAttackInput();
        UpdateAttackState();
    }

    private void HandleAttackInput()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        // Начинаем новую комбинацию.
        if (!attackInProgress)
        {
            StartFirstAttack();
            return;
        }

        // Во время первой атаки разрешаем поставить вторую в очередь.
        if (currentAttack == 1 && !attack2Triggered)
        {
            comboQueued = true;
        }
    }

    private void StartFirstAttack()
    {
        attackInProgress = true;
        attackStateEntered = false;

        comboQueued = false;
        attack2Triggered = false;

        currentAttack = 1;

        animator.ResetTrigger(attack1Trigger);
        animator.ResetTrigger(attack2Trigger);

        animator.SetTrigger(attack1Trigger);
    }

    private void UpdateAttackState()
    {
        if (!attackInProgress)
            return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        bool inAttack1 = state.IsName(attack1StateName);
        bool inAttack2 = state.IsName(attack2StateName);

        // Ждём, пока Animator действительно войдёт в атаку.
        if (inAttack1 || inAttack2)
        {
            attackStateEntered = true;
        }

        // Проверяем возможность перехода ко второй атаке.
        if (currentAttack == 1 &&
            inAttack1 &&
            comboQueued &&
            !attack2Triggered &&
            state.normalizedTime >= comboStartTime)
        {
            StartSecondAttack();
        }

        // Завершаем комбинацию, когда Animator вернулся к обычному состоянию.
        if (attackStateEntered &&
            !animator.IsInTransition(0) &&
            !inAttack1 &&
            !inAttack2)
        {
            ResetAttack();
        }
    }

    private void StartSecondAttack()
    {
        attack2Triggered = true;
        comboQueued = false;

        currentAttack = 2;

        animator.SetTrigger(attack2Trigger);
    }

    private void ResetAttack()
    {
        attackInProgress = false;
        attackStateEntered = false;

        comboQueued = false;
        attack2Triggered = false;

        currentAttack = 0;
    }
}
