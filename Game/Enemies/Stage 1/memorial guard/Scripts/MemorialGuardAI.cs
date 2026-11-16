
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
        Attack,
        AfterAttack
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

    [Header("Combat")]
    [SerializeField] private float attackRange = 1.35f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float attackFallbackDuration = 0.8f;
    [SerializeField] private float afterAttackFallbackDuration = 0.5f;
    [SerializeField] private float blockedChaseTimeout = 1.5f;

    [Header("Animator States")]
    [SerializeField] private string idleAnimation = "MG_idle";
    [SerializeField] private string patrolAnimation = "MG_Patrol";
    [SerializeField] private string attackAnimation = "MG_Attack1";
    [SerializeField] private string afterAttackAnimation = "MG_AfterAttack";

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D bodyCollider;

    private State currentState;

    private Vector2 startPosition;

    private int patrolDirection = 1;
    private int moveDirection;

    private float currentMoveSpeed;
    private float pauseTimer;
    private float attackTimer;
    private float nextAttackTime;
    private float blockedChaseTimer;
    private string currentAnimation;

    private bool isPatrolPaused;
    private bool hasTarget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<BoxCollider2D>();

        startPosition = transform.position;
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

            case State.Attack:
                UpdateAttack();
                break;

            case State.AfterAttack:
                UpdateAfterAttack();
                break;
        }
    }

    private void FixedUpdate()
    {
        // AI никогда не меняет вертикальную скорость.
        // Прыжков у этого врага нет.
        float horizontalVelocity = moveDirection * currentMoveSpeed;

        if (currentState == State.Attack ||
            currentState == State.AfterAttack)
        {
            horizontalVelocity = 0f;
        }

        rb.velocity = new Vector2(
            horizontalVelocity,
            rb.velocity.y
        );
    }

    // -------------------------
    // DETECTION
    // -------------------------

    private void UpdateTargetDetection()
    {
        if (player == null)
            return;

        if (currentState == State.Attack || currentState == State.AfterAttack)
        {
            return;
        }

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        float verticalDistance = Mathf.Abs(
            transform.position.y - player.position.y
        );

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
        {
            LoseTarget();
        }
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        if (visionBlockers.value == 0)
            return true;

        Vector2 origin = bodyCollider.bounds.center;
        Vector2 direction = (player.position - transform.position).normalized;

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            direction,
            distance,
            visionBlockers
        );

        return hit.collider == null;
    }

    private void LoseTarget()
    {
        hasTarget = false;
        blockedChaseTimer = 0f;

        patrolDirection =
            transform.position.x >= startPosition.x ? -1 : 1;

        EnterPatrolPause();
    }

    // -------------------------
    // PATROL
    // -------------------------

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

        float distanceFromStart =
            transform.position.x - startPosition.x;

        if ((patrolDirection > 0 &&
             distanceFromStart >= patrolDistance) ||
            (patrolDirection < 0 &&
             distanceFromStart <= -patrolDistance))
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

        pauseTimer = Random.Range(
            minPauseTime,
            maxPauseTime
        );

        EnterState(State.Patrol);
        PlayAnimation(idleAnimation);
    }

    // -------------------------
    // CHASE
    // -------------------------

    private void UpdateChase()
    {
        if (player == null)
        {
            LoseTarget();
            return;
        }

        float differenceX =
            player.position.x - transform.position.x;

        float distanceX = Mathf.Abs(differenceX);

        float distanceY = Mathf.Abs(
            player.position.y - transform.position.y
        );

        int direction = differenceX >= 0f ? 1 : -1;

        SetFacing(direction);

        // Игрок уже в радиусе атаки.
        if (distanceX <= attackRange &&
            distanceY <= verticalDetectionTolerance)
        {
            blockedChaseTimer = 0f;

            // Пока атака перезаряжается — стоим в Idle.
            PlayAnimation(idleAnimation);

            if (Time.time >= nextAttackTime)
            {
                EnterState(State.Attack);
            }

            return;
        }

        // Проверяем препятствия и пропасти.
        if (!CanMove(direction))
        {
            PlayAnimation(idleAnimation);

            blockedChaseTimer += Time.deltaTime;

            if (blockedChaseTimer >= blockedChaseTimeout)
            {
                LoseTarget();
            }

            return;
        }

        blockedChaseTimer = 0f;

        PlayAnimation(patrolAnimation);

        moveDirection = direction;
        currentMoveSpeed = chaseSpeed;
    }

    // -------------------------
    // ATTACK
    // -------------------------

    private void UpdateAttack()
    {
        attackTimer += Time.deltaTime;

        AnimatorStateInfo stateInfo =
            animator.GetCurrentAnimatorStateInfo(0);

        bool animationFinished =
            stateInfo.IsName(attackAnimation) &&
            stateInfo.normalizedTime >= 1f;

        // Запасной вариант, если Animator не смог
        // запустить нужное состояние.
        if (animationFinished ||
            attackTimer >= attackFallbackDuration)
        {
            nextAttackTime = Time.time + attackCooldown;

            EnterState(State.AfterAttack);
        }
    }

    // -------------------------
    // AFTER ATTACK
    // -------------------------

    private void UpdateAfterAttack()
    {
        attackTimer += Time.deltaTime;

        AnimatorStateInfo stateInfo =
            animator.GetCurrentAnimatorStateInfo(0);

        bool animationFinished =
            stateInfo.IsName(afterAttackAnimation) &&
            stateInfo.normalizedTime >= 1f;

        if (animationFinished ||
            attackTimer >= afterAttackFallbackDuration)
        {
            if (hasTarget)
                EnterState(State.Chase);
            else
                EnterPatrolPause();
        }
    }

    // -------------------------
    // STATE MANAGEMENT
    // -------------------------

    private void EnterState(State newState)
    {
        currentState = newState;

        attackTimer = 0f;
        moveDirection = 0;
        currentMoveSpeed = 0f;

        if (newState == State.Attack)
        {
            if (player != null)
            {
                int direction =
                    player.position.x >= transform.position.x ? 1 : -1;

                SetFacing(direction);
            }

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

    // -------------------------
    // MOVEMENT HELPERS
    // -------------------------

    private bool CanMove(int direction)
    {
        // Без назначенного Ground Layer движение запрещаем.
        if (groundLayer.value == 0)
        {
            Debug.LogWarning(
                $"{gameObject.name}: Ground Layer не назначен!"
            );

            return false;
        }

        Bounds bounds = bodyCollider.bounds;

        // 1. Проверяем землю впереди.
        float forwardOffset = 0.2f;

        float groundX = direction > 0
            ? bounds.max.x + forwardOffset
            : bounds.min.x - forwardOffset;

        Vector2 groundOrigin = new Vector2(
            groundX,
            bounds.min.y + 0.05f
        );

        RaycastHit2D groundHit = Physics2D.Raycast(
            groundOrigin,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        // Если земли нет — двигаться нельзя.
        if (groundHit.collider == null)
            return false;

        // 2. Проверяем стену впереди.
        float sideX = direction > 0
            ? bounds.max.x + 0.03f
            : bounds.min.x - 0.03f;

        Vector2 wallOrigin = new Vector2(
            sideX,
            bounds.center.y
        );

        RaycastHit2D wallHit = Physics2D.Raycast(
            wallOrigin,
            Vector2.right * direction,
            wallCheckDistance,
            groundLayer
        );

        return wallHit.collider == null;
    }

    private void SetFacing(int direction)
    {
        if (direction == 0)
            return;

        // Предполагается, что исходный спрайт смотрит вправо.
        spriteRenderer.flipX = direction < 0;
    }

    private void PlayAnimation(string animationName)
    {
        if (currentAnimation == animationName)
            return;

        int hash = Animator.StringToHash(animationName);

        if (!animator.HasState(0, hash))
        {
            Debug.LogWarning(
                $"У {gameObject.name} отсутствует состояние Animator: {animationName}"
            );

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

        Gizmos.color = Color.cyan;

        Vector3 center = Application.isPlaying
            ? (Vector3)startPosition
            : transform.position;

        Gizmos.DrawLine(
            center + Vector3.left * patrolDistance,
            center + Vector3.right * patrolDistance
        );
    }
}
