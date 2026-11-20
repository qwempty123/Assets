
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DetachedHead : MonoBehaviour
{
    [Header("Launch Settings")]
    [SerializeField] private float horizontalSpeed = 2f;
    [SerializeField] private float upwardSpeed = 2f;
    [SerializeField] private float rotationSpeed = 360f;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 8f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// Вызывает вылет головы в направлении, противоположном взгляду игрока.
    /// playerFacesRight — true, если игрок смотрел вправо.
    /// </summary>
    public void Launch(bool playerFacesRight)
    {
        // Голова смотрит туда же, куда смотрел игрок.
        if (spriteRenderer != null)
            spriteRenderer.flipX = !playerFacesRight;

        // Направление вылета противоположно взгляду игрока.
        float direction = playerFacesRight ? -1f : 1f;

        rb.velocity = new Vector2(
            direction * horizontalSpeed,
            upwardSpeed
        );

        // Вращение зависит от направления вылета.
        rb.angularVelocity = -direction * rotationSpeed;

        Destroy(gameObject, lifetime);
    }
}
