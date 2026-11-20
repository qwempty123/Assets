
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 5;

    [Header("Damage Protection")]
    [SerializeField] private float invulnerabilityDuration = 0.75f;
    [SerializeField] private float flashInterval = 0.08f;

    [Header("Death")]
    [SerializeField] private string deathAnimationName = "Player_Death";
    [SerializeField] private float deathAnimationDuration = 1.2f;
    [SerializeField] private float respawnDelay = 3f;

    [Header("Detached Head")]
    [SerializeField] private Transform deadPointHead;
    [SerializeField] private DetachedHead headPrefab;

    private int currentHealth;
    private bool isInvulnerable;
    private bool isDead;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Animator animator;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool startFlipX;

    private Coroutine flashCoroutine;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public bool IsInvulnerable => isInvulnerable;

    private void Awake()
    {
        currentHealth = maxHealth;

        // Запоминаем начальную позицию игрока.
        startPosition = transform.position;
        startRotation = transform.rotation;

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            startFlipX = spriteRenderer.flipX;
        }

        animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || isDead || isInvulnerable)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        Debug.Log($"{gameObject.name} получил {damage} урона. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(DamageProtection());
    }

    private IEnumerator DamageProtection()
    {
        isInvulnerable = true;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(DamageFlash());

        yield return new WaitForSeconds(invulnerabilityDuration);

        isInvulnerable = false;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    private IEnumerator DamageFlash()
    {
        if (spriteRenderer == null)
            yield break;

        float elapsed = 0f;
        bool white = true;

        while (elapsed < invulnerabilityDuration)
        {
            spriteRenderer.color = white ? Color.white : originalColor;
            white = !white;

            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        spriteRenderer.color = originalColor;
        flashCoroutine = null;
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        isInvulnerable = true;

        // Отключаем управление.
        PlayerMovement movement = GetComponent<PlayerMovement>();

        if (movement != null)
        {
            movement.CancelDash();
            movement.enabled = false;
        }

        // Отключаем боевую систему.
        PlayerCombat combat = GetComponent<PlayerCombat>();

        if (combat != null)
        {
            combat.DisableHitbox();
            combat.enabled = false;
        }

        // Останавливаем физику игрока.
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb != null)
            rb.velocity = Vector2.zero;

        // Отделяем голову.
        SpawnHead();

        // Запускаем анимацию смерти.
        if (animator != null)
            animator.Play(deathAnimationName, 0, 0f);

        // Запускаем таймер респавна.
        StartCoroutine(RespawnAfterDelay());

        Debug.Log($"{gameObject.name} погиб. Респавн через {respawnDelay} секунд.");
    }

    private void SpawnHead()
    {
        if (headPrefab == null)
        {
            Debug.LogWarning("PlayerHealth: Head Prefab не назначен!");
            return;
        }

        if (deadPointHead == null)
        {
            Debug.LogWarning("PlayerHealth: Dead Point Head не назначен!");
            return;
        }

        bool playerFacesRight = spriteRenderer == null || !spriteRenderer.flipX;

        DetachedHead head = Instantiate(
            headPrefab,
            deadPointHead.position,
            deadPointHead.rotation
        );

        head.Launch(playerFacesRight);
    }

    private IEnumerator RespawnAfterDelay()
    {
        // Ждём заданное время после смерти.
        yield return new WaitForSeconds(respawnDelay);

        Respawn();
    }

    private void Respawn()
    {
        // Возвращаем игрока в начальную позицию.
        transform.position = startPosition;
        transform.rotation = startRotation;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = startPosition;
        }

        // Восстанавливаем здоровье.
        currentHealth = maxHealth;

        // Сбрасываем состояние смерти и неуязвимости.
        isDead = false;
        isInvulnerable = false;

        // Возвращаем внешний вид.
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            spriteRenderer.flipX = startFlipX;
        }

        // Сбрасываем Animator в начальное состояние.
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        // Включаем управление.
        PlayerMovement movement = GetComponent<PlayerMovement>();

        if (movement != null)
            movement.enabled = true;

        // Включаем боевую систему.
        PlayerCombat combat = GetComponent<PlayerCombat>();

        if (combat != null)
            combat.enabled = true;

        Debug.Log($"Игрок возродился. HP: {currentHealth}/{maxHealth}");
    }
}
