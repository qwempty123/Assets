using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class AttackHitbox : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int damage = 1;

    private BoxCollider2D hitbox;
    private HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();

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
        hitbox.offset = new Vector2(
            Mathf.Abs(initialOffset.x) * direction,
            initialOffset.y
        );
    }

    public void EnableHitbox()
    {
        hitEnemies.Clear();
        hitbox.enabled = true;
    }

    public void DisableHitbox()
    {
        hitbox.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();

        if (enemy == null)
            return;

        if (!hitEnemies.Add(enemy))
            return;

        enemy.TakeDamage(damage);
    }
}