
using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 3;

    [Header("Damage Flash")]
    [SerializeField] private float flashDuration = 0.12f;

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

    private void Die()
    {
        Debug.Log($"{gameObject.name} погиб!");

        Destroy(gameObject);
    }
}
