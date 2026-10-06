using UnityEngine;

/// <summary>
/// Instantly kills the player on contact. Put it on any hazard that has a
/// collider: water, lava, spikes, or an invisible box below the level for pits.
/// Works whether the hazard's collider is a trigger or solid.
/// </summary>
public class KillZone : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Also defeat enemies that touch this zone.")]
    [SerializeField] private bool killEnemies = false;

    [Header("Sinking (for water)")]
    [Tooltip("Make the player sink slowly after dying here instead of dropping at full speed.")]
    [SerializeField] private bool sinkSlowly = true;
    [Tooltip("How thick the liquid feels. Higher = slower sinking.")]
    [SerializeField] private float sinkDrag = 6f;
    [Tooltip("Gravity while sinking. Lower = slower sinking.")]
    [SerializeField] private float sinkGravity = 0.5f;

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleContact(other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleContact(collision.collider);
    }

    void HandleContact(Collider2D other)
    {
        // Use the object that owns the Rigidbody, in case the touching collider sits on a child
        GameObject target = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;

        if (target.CompareTag(playerTag))
        {
            PlayerHealth health = target.GetComponent<PlayerHealth>();
            if (health != null) health.Kill();

            if (sinkSlowly)
            {
                Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = new Vector2(0f, Mathf.Min(rb.linearVelocity.y, 0f) * 0.2f);
                    rb.linearDamping = sinkDrag;
                    rb.gravityScale = sinkGravity;
                }
            }
            return;
        }

        if (killEnemies)
        {
            GoombaEnemy enemy = target.GetComponent<GoombaEnemy>();
            if (enemy != null) enemy.TakeDamage(1);
        }
    }
}
