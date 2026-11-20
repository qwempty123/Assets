using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 3;

    [Header("Damage Flash")]
    [SerializeField] private float flashDuration = 0.12f;

    [Header("Blood Particles")]
    [SerializeField] private ParticleSystem bloodEffectPrefab;
    [SerializeField] private Vector2 bloodEffectOffset = new Vector2(0f, 0.2f);

    private int currentHealth;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private Coroutine flashCoroutine;

    private void Awake()
    {
        currentHealth = maxHealth;

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0)
            return;

        currentHealth -= damage;

        Debug.Log(
            $"{gameObject.name} получил {damage} урона. HP: {currentHealth}"
        );

        FlashWhite();
        SpawnBloodEffect();

        if (currentHealth <= 0)
            Die();
    }

    private void FlashWhite()
    {
        if (spriteRenderer == null)
            return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        spriteRenderer.color = originalColor;
        flashCoroutine = StartCoroutine(WhiteFlash());
    }

    private IEnumerator WhiteFlash()
    {
        spriteRenderer.color = Color.white;

        yield return new WaitForSeconds(flashDuration);

        spriteRenderer.color = originalColor;

        flashCoroutine = null;
    }

    private void SpawnBloodEffect()
    {
        if (bloodEffectPrefab == null)
            return;

        Vector3 spawnPosition = transform.position;

        if (spriteRenderer != null)
            spawnPosition = spriteRenderer.bounds.center;

        spawnPosition += (Vector3)bloodEffectOffset;

        ParticleSystem blood = Instantiate(
            bloodEffectPrefab,
            spawnPosition,
            Quaternion.identity
        );

        blood.Play();

        ParticleSystem.MainModule main = blood.main;

        float lifetime = main.duration + main.startLifetime.constantMax;

        Destroy(blood.gameObject, lifetime + 0.2f);
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} погиб!");

        Destroy(gameObject);
    }
}
