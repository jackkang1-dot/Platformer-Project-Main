using UnityEngine;

/// <summary>
/// Makes a hazard (like rising pus or lava) move steadily upward.
/// Put it on the same object as the hazard's collider and KillZone script.
/// The level restarts on death, so the hazard starts over from the bottom each try.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class RisingHazard : MonoBehaviour
{
    [Header("Rising")]
    [Tooltip("How fast it rises, in units per second.")]
    [SerializeField] private float riseSpeed = 0.5f;
    [Tooltip("Seconds to wait after the level starts before it begins rising.")]
    [SerializeField] private float startDelay = 2f;
    [Tooltip("How far it rises before stopping, in units. Set to 0 to keep rising forever.")]
    [SerializeField] private float maxRiseDistance = 0f;

    private Rigidbody2D rb;
    private float startY;
    private float startTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Kinematic: moved by this script only, never pulled by gravity or pushed by the player
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    void Start()
    {
        startY = rb.position.y;
        startTime = Time.time + startDelay;
    }

    void FixedUpdate()
    {
        if (Time.time < startTime) return;

        float step = riseSpeed * Time.fixedDeltaTime;

        if (maxRiseDistance > 0f)
        {
            float remaining = (startY + maxRiseDistance) - rb.position.y;
            if (remaining <= 0f) return;
            step = Mathf.Min(step, remaining);
        }

        rb.MovePosition(rb.position + Vector2.up * step);
    }

    // In the Scene view, shows where the top of the hazard will stop (when Max Rise Distance is set)
    void OnDrawGizmosSelected()
    {
        if (maxRiseDistance <= 0f) return;

        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Bounds b = col.bounds;
        float rise = Application.isPlaying ? (startY + maxRiseDistance) - transform.position.y : maxRiseDistance;
        float topY = b.max.y + rise;

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(new Vector3(b.min.x, topY, 0f), new Vector3(b.max.x, topY, 0f));
    }
}
