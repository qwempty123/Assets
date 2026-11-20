using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class MemorialGuardAI : MonoBehaviour
{
    private enum State
    {
        Patrol,
        Chase,
        BackDash,
        Attack,
        LongAttack,
        AfterAttack
    }

    private enum BackDashPurpose
    {
        Dodge,
        PrepareLongAttack
    }

    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float loseTargetRange = 7f;
    [SerializeField] private float verticalDetectionTolerance = 1.25f;
    [SerializeField] private LayerMask visionBlockers;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 0.8f;
    [SerializeField] private float chaseSpeed = 1.4f;
    [SerializeField] private float patrolDistance = 2.5f;
    [SerializeField] private float groundCheckDistance = 0.8f;
    [SerializeField] private float wallCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Patrol Pause")]
    [SerializeField] private float minPauseTime = 0.8f;
    [SerializeField] private float maxPauseTime = 2f;

    [Header("Melee Attack")]
    [SerializeField] private float attackRange = 1.35f;
    [SerializeField] private int meleeDamage = 1;
    [SerializeField] private Vector2 meleeHitboxSize = new Vector2(1.1f, 0.9f);
    [SerializeField] private float meleeHitboxForwardOffset = 0.65f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float attackFallbackDuration = 0.8f;

    [Header("Back Dash")]
    [SerializeField] private float backDashSpeed = 5f;
    [SerializeField] private float backDashMoveDuration = 0.24f;
    [SerializeField] private float backDashAnimationDuration = 0.7f;
    [SerializeField] private float backDashCooldown = 2.5f;

    [Header("Dodge")]
    [SerializeField, Range(0f, 1f)] private float dodgeChance = 0.45f;
    [SerializeField] private float dodgeDetectionRange = 2.2f;

    [Header("Long Attack")]
    [SerializeField] private float longAttackMinRange = 1.8f;
    [SerializeField] private float longAttackMaxRange = 3.6f;
    [SerializeField] private float longAttackCooldown = 4.5f;
    [SerializeField] private float longAttackAnimationDuration = 0.72f;
    [SerializeField] private float longAttackLungeSpeed = 5.5f;
    [SerializeField] private float longAttackLungeDuration = 0.25f;
    [SerializeField] private int longAttackDamage = 2;
    [SerializeField] private Vector2 longAttackHitboxSize = new Vector2(1.5f, 1.1f);
    [SerializeField] private float longAttackHitboxForwardOffset = 0.9f;

    [Header("Recovery")]
    [SerializeField] private float afterAttackFallbackDuration = 0.5f;
    [SerializeField] private float blockedChaseTimeout = 1.5f;

    [Header("Animator States")]
    [SerializeField] private string idleAnimation = "MG_idle";
    [SerializeField] private string patrolAnimation = "MG_Patrol";
    [SerializeField] private string backDashAnimation = "MG_BackDash";
    [SerializeField] private string attackAnimation = "MG_Attack1";
    [SerializeField] private string longAttackAnimation = "MG_LongAttack";
    [SerializeField] private string afterAttackAnimation = "MG_afterAttack";

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D bodyCollider;
    private PlayerCombat playerCombat;

    private State currentState;
    private BackDashPurpose backDashPurpose;

    private readonly HashSet<PlayerHealth> hitPlayers = new HashSet<PlayerHealth>();

    private Vector2 startPosition;
    private int patrolDirection = 1;
    private int moveDirection;
    private int facingDirection = 1;

    private float currentMoveSpeed;
    private float pauseTimer;
    private float stateTimer;
    private float nextAttackTime;
    private float nextBackDashTime;
    private float nextLongAttackTime;
    private float nextDodgeTime;
    private float blockedChaseTimer;
    private float backDashMoveEndTime;
    private float longAttackMoveEndTime;

    private bool isPatrolPaused;
    private bool hasTarget;
    private bool longAttackHitRegistered;
    private string currentAnimation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<BoxCollider2D>();
        startPosition = transform.position;

        if (player != null)
            playerCombat = player.GetComponent<PlayerCombat>();
    }

    private void Start()
    {
        EnterPatrolPause();
    }

    private void Update()
    {
        moveDirection = 0;
        currentMoveSpeed = 0f;

        UpdateTargetDetection();

        switch (currentState)
        {
            case State.Patrol:
                UpdatePatrol();
                break;
            case State.Chase:
                UpdateChase();
                break;
            case State.BackDash:
                UpdateBackDash();
                break;
            case State.Attack:
                UpdateAttack();
                break;
            case State.LongAttack:
                UpdateLongAttack();
                break;
            case State.AfterAttack:
                UpdateAfterAttack();
                break;
        }
    }

    private void FixedUpdate()
    {
        float horizontalVelocity = moveDirection * currentMoveSpeed;

        if (currentState == State.BackDash && Time.time < backDashMoveEndTime)
        {
            if (CanMove(-facingDirection))
                horizontalVelocity = -facingDirection * backDashSpeed;
        }
        else if (currentState == State.LongAttack && Time.time < longAttackMoveEndTime)
        {
            if (CanMove(facingDirection))
                horizontalVelocity = facingDirection * longAttackLungeSpeed;
        }

        rb.velocity = new Vector2(horizontalVelocity, rb.velocity.y);
    }

    private void UpdateTargetDetection()
    {
        if (player == null)
            return;

        if (currentState == State.Attack ||
            currentState == State.LongAttack ||
            currentState == State.BackDash ||
            currentState == State.AfterAttack)
            return;

        float distance = Vector2.Distance(transform.position, player.position);
        float verticalDistance = Mathf.Abs(transform.position.y - player.position.y);

        if (!hasTarget)
        {
            if (distance <= detectionRange &&
                verticalDistance <= verticalDetectionTolerance &&
                CanSeePlayer())
            {
                hasTarget = true;
                blockedChaseTimer = 0f;
                EnterState(State.Chase);
            }

            return;
        }

        if (distance > loseTargetRange ||
            verticalDistance > verticalDetectionTolerance * 1.5f)
            LoseTarget();
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        if (visionBlockers.value == 0)
            return true;

        Vector2 origin = bodyCollider.bounds.center;
        Vector2 direction = ((Vector2)player.position - origin).normalized;
        float distance = Vector2.Distance(origin, player.position);

        return Physics2D.Raycast(origin, direction, distance, visionBlockers).collider == null;
    }

    private void LoseTarget()
    {
        hasTarget = false;
        blockedChaseTimer = 0f;
        patrolDirection = transform.position.x >= startPosition.x ? -1 : 1;
        EnterPatrolPause();
    }

    private void UpdatePatrol()
    {
        if (isPatrolPaused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                isPatrolPaused = false;
                PlayAnimation(patrolAnimation);
            }
            return;
        }

        float distanceFromStart = transform.position.x - startPosition.x;

        if ((patrolDirection > 0 && distanceFromStart >= patrolDistance) ||
            (patrolDirection < 0 && distanceFromStart <= -patrolDistance))
        {
            patrolDirection *= -1;
            EnterPatrolPause();
            return;
        }

        if (!CanMove(patrolDirection))
        {
            patrolDirection *= -1;
            EnterPatrolPause();
            return;
        }

        SetFacing(patrolDirection);
        moveDirection = patrolDirection;
        currentMoveSpeed = patrolSpeed;
    }

    private void EnterPatrolPause()
    {
        isPatrolPaused = true;
        pauseTimer = Random.Range(minPauseTime, maxPauseTime);
        EnterState(State.Patrol);
        PlayAnimation(idleAnimation);
    }

    private void UpdateChase()
    {
        if (player == null)
        {
            LoseTarget();
            return;
        }

        float differenceX = player.position.x - transform.position.x;
        float distanceX = Mathf.Abs(differenceX);
        float distanceY = Mathf.Abs(player.position.y - transform.position.y);
        int direction = differenceX >= 0f ? 1 : -1;

        SetFacing(direction);

        if (playerCombat == null)
            playerCombat = player.GetComponent<PlayerCombat>();

        // Уворот от атаки игрока. Перезарядка не позволяет уклоняться
        // несколько раз в течение одной и той же атаки.
        if (playerCombat != null &&
            playerCombat.IsAttacking &&
            distanceX <= dodgeDetectionRange &&
            Time.time >= nextDodgeTime &&
            Random.value <= dodgeChance)
        {
            StartBackDash(BackDashPurpose.Dodge);
            return;
        }

        if (distanceX <= attackRange && distanceY <= verticalDetectionTolerance)
        {
            blockedChaseTimer = 0f;
            PlayAnimation(idleAnimation);

            if (Time.time >= nextAttackTime)
                EnterState(State.Attack);

            return;
        }

        if (distanceX >= longAttackMinRange &&
            distanceX <= longAttackMaxRange &&
            distanceY <= verticalDetectionTolerance &&
            Time.time >= nextLongAttackTime &&
            Time.time >= nextBackDashTime)
        {
            StartBackDash(BackDashPurpose.PrepareLongAttack);
            return;
        }

        if (!CanMove(direction))
        {
            PlayAnimation(idleAnimation);
            blockedChaseTimer += Time.deltaTime;

            if (blockedChaseTimer >= blockedChaseTimeout)
                LoseTarget();

            return;
        }

        blockedChaseTimer = 0f;
        PlayAnimation(patrolAnimation);
        moveDirection = direction;
        currentMoveSpeed = chaseSpeed;
    }

    private void StartBackDash(BackDashPurpose purpose)
    {
        if (player == null)
            return;

        int directionToPlayer = player.position.x >= transform.position.x ? 1 : -1;
        SetFacing(directionToPlayer);

        int retreatDirection = -directionToPlayer;

        // Не начинаем рывок, если позади стена или пропасть.
        if (!CanMove(retreatDirection))
            return;

        backDashPurpose = purpose;
        stateTimer = 0f;
        backDashMoveEndTime = Time.time + backDashMoveDuration;
        nextBackDashTime = Time.time + backDashCooldown;

        if (purpose == BackDashPurpose.Dodge)
            nextDodgeTime = Time.time + backDashCooldown;

        EnterState(State.BackDash);
        PlayAnimation(backDashAnimation);
    }

    private void UpdateBackDash()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer < backDashAnimationDuration)
            return;

        if (backDashPurpose == BackDashPurpose.PrepareLongAttack && player != null)
        {
            // Перед выпадом снова фиксируем направление на игрока.
            int direction = player.position.x >= transform.position.x ? 1 : -1;
            SetFacing(direction);

            if (CanMove(direction))
            {
                stateTimer = 0f;
                longAttackMoveEndTime = Time.time + longAttackLungeDuration;
                longAttackHitRegistered = false;
                hitPlayers.Clear();
                nextLongAttackTime = Time.time + longAttackCooldown;
                EnterState(State.LongAttack);
                PlayAnimation(longAttackAnimation);
                return;
            }
        }

        EnterState(State.AfterAttack);
    }

    private void UpdateAttack()
    {
        stateTimer += Time.deltaTime;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        bool finished = info.IsName(attackAnimation) && info.normalizedTime >= 1f;

        if (finished || stateTimer >= attackFallbackDuration)
        {
            nextAttackTime = Time.time + attackCooldown;
            EnterState(State.AfterAttack);
        }
    }

    private void UpdateLongAttack()
    {
        stateTimer += Time.deltaTime;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        bool finished = info.IsName(longAttackAnimation) && info.normalizedTime >= 1f;

        if (finished || stateTimer >= longAttackAnimationDuration)
        {
            nextAttackTime = Time.time + attackCooldown;
            EnterState(State.AfterAttack);
        }
    }

    private void UpdateAfterAttack()
    {
        stateTimer += Time.deltaTime;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        bool finished = info.IsName(afterAttackAnimation) && info.normalizedTime >= 1f;

        if (finished || stateTimer >= afterAttackFallbackDuration)
        {
            if (hasTarget)
                EnterState(State.Chase);
            else
                EnterPatrolPause();
        }
    }

    private void EnterState(State newState)
    {
        currentState = newState;
        stateTimer = 0f;
        moveDirection = 0;
        currentMoveSpeed = 0f;

        if (newState == State.Attack && player != null)
        {
            SetFacing(player.position.x >= transform.position.x ? 1 : -1);
            hitPlayers.Clear();
            PlayAnimation(attackAnimation);
        }
        else if (newState == State.AfterAttack)
        {
            PlayAnimation(afterAttackAnimation);
        }
        else if (newState == State.Chase)
        {
            PlayAnimation(patrolAnimation);
        }
    }

    // Добавить Animation Event в MG_Attack1 на кадр контакта оружия.
    public void AnimationEvent_MeleeHit()
    {
        DealDamageInFront(meleeDamage, meleeHitboxSize, meleeHitboxForwardOffset);
    }

    // Добавить Animation Event в MG_LongAttack на кадр контакта во время выпада.
    public void AnimationEvent_LongAttackHit()
    {
        if (currentState != State.LongAttack || longAttackHitRegistered)
            return;

        longAttackHitRegistered = true;
        DealDamageInFront(longAttackDamage, longAttackHitboxSize, longAttackHitboxForwardOffset);
    }

    private void DealDamageInFront(int damage, Vector2 hitboxSize, float forwardOffset)
    {
        Vector2 center = bodyCollider.bounds.center;
        center.x += facingDirection * forwardOffset;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, hitboxSize, 0f);

        foreach (Collider2D hit in hits)
        {
            PlayerHealth health = hit.GetComponentInParent<PlayerHealth>();
            if (health == null || !hitPlayers.Add(health))
                continue;

            health.TakeDamage(damage);
        }
    }

    private bool CanMove(int direction)
    {
        if (direction == 0)
            return false;

        if (groundLayer.value == 0)
        {
            Debug.LogWarning($"{gameObject.name}: Ground Layer не назначен!");
            return false;
        }

        Bounds bounds = bodyCollider.bounds;

        float groundX = direction > 0
            ? bounds.max.x + 0.2f
            : bounds.min.x - 0.2f;

        Vector2 groundOrigin = new Vector2(groundX, bounds.min.y + 0.05f);
        RaycastHit2D groundHit = Physics2D.Raycast(
            groundOrigin, Vector2.down, groundCheckDistance, groundLayer);

        if (groundHit.collider == null)
            return false;

        float sideX = direction > 0
            ? bounds.max.x + 0.03f
            : bounds.min.x - 0.03f;

        Vector2 wallOrigin = new Vector2(sideX, bounds.center.y);
        RaycastHit2D wallHit = Physics2D.Raycast(
            wallOrigin, Vector2.right * direction, wallCheckDistance, groundLayer);

        return wallHit.collider == null;
    }

    private void SetFacing(int direction)
    {
        if (direction == 0)
            return;

        facingDirection = direction;
        spriteRenderer.flipX = direction < 0;
    }

    private void PlayAnimation(string animationName)
    {
        if (currentAnimation == animationName)
            return;

        int hash = Animator.StringToHash(animationName);

        if (!animator.HasState(0, hash))
        {
            Debug.LogWarning($"{gameObject.name}: Animator state not found: {animationName}");
            return;
        }

        currentAnimation = animationName;
        animator.Play(hash, 0, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 center = Application.isPlaying ? (Vector3)startPosition : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            center + Vector3.left * patrolDistance,
            center + Vector3.right * patrolDistance);
    }
}