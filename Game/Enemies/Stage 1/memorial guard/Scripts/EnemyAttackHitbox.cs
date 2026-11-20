
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class EnemyAttackHitbox : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int damage = 1;

    private BoxCollider2D hitbox;

    private readonly HashSet<PlayerHealth> hitPlayers =
        new HashSet<PlayerHealth>();

    private Vector2 initialOffset;

    private void Awake()
    {
        hitbox = GetComponent<BoxCollider2D>();

        initialOffset = hitbox.offset;

        hitbox.isTrigger = true;
        hitbox.enabled = false;
    }

    public void SetFacingDirection(int direction)
    {
        if (hitbox == null || direction == 0)
            return;

        hitbox.offset = new Vector2(
            Mathf.Abs(initialOffset.x) * direction,
            initialOffset.y
        );
    }

    public void EnableHitbox()
    {
        if (hitbox == null)
            return;

        hitPlayers.Clear();
        hitbox.enabled = true;
    }

    public void DisableHitbox()
    {
        if (hitbox != null)
            hitbox.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (!hitPlayers.Add(playerHealth))
            return;

        playerHealth.TakeDamage(damage);
    }

    private void OnDisable()
    {
        if (hitbox != null)
            hitbox.enabled = false;

        hitPlayers.Clear();
    }
}
