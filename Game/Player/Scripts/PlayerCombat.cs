
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

    [Header("Hitbox")]
    [SerializeField] private AttackHitbox attackHitbox;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private bool attackInProgress;
    private bool attackStateEntered;
    private bool comboQueued;
    private bool attack2Triggered;

    private bool lockedFacing;

    private int attackDirection;
    private int currentAttack;

    public bool IsAttacking => attackInProgress;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        HandleAttackInput();
        UpdateAttackState();

        if (attackInProgress)
            spriteRenderer.flipX = lockedFacing;
    }

    private void HandleAttackInput()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (!attackInProgress)
        {
            StartFirstAttack();
            return;
        }

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

        // Запоминаем направление персонажа.
        lockedFacing = spriteRenderer.flipX;
        attackDirection = lockedFacing ? -1 : 1;

        if (attackHitbox != null)
        {
            attackHitbox.DisableHitbox();
            attackHitbox.SetFacingDirection(attackDirection);
        }

        animator.ResetTrigger(attack1Trigger);
        animator.ResetTrigger(attack2Trigger);

        animator.SetTrigger(attack1Trigger);
    }

    private void UpdateAttackState()
    {
        if (!attackInProgress)
            return;

        AnimatorStateInfo state =
            animator.GetCurrentAnimatorStateInfo(0);

        bool inAttack1 = state.IsName(attack1StateName);
        bool inAttack2 = state.IsName(attack2StateName);

        if (inAttack1 || inAttack2)
            attackStateEntered = true;

        if (currentAttack == 1 &&
            inAttack1 &&
            comboQueued &&
            !attack2Triggered &&
            state.normalizedTime >= comboStartTime)
        {
            StartSecondAttack();
        }

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

        if (attackHitbox != null)
            attackHitbox.DisableHitbox();
    }

    // Эти методы вызываются Animation Events.

    public void EnableHitbox()
    {
        if (attackHitbox != null)
            attackHitbox.EnableHitbox();
    }

    public void DisableHitbox()
    {
        if (attackHitbox != null)
            attackHitbox.DisableHitbox();
    }
}
