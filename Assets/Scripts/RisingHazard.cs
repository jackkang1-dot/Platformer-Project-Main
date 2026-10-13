using UnityEngine;

/// <summary>
/// Makes a hazard (like rising pus or lava) move steadily upward.
/// Put it on the same object as the hazard's collider and KillZone script.
/// By default it waits until the player first moves or jumps before rising,
/// so players can read any on-screen messages first.
/// The level restarts on death, so the hazard starts over from the bottom each try.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class RisingHazard : MonoBehaviour
{
    [Header("Rising")]
    [Tooltip("How fast it rises, in units per second.")]
    [SerializeField] private float riseSpeed = 0.5f;
    [Tooltip("How far it rises before stopping, in units. Set to 0 to keep rising forever.")]
    [SerializeField] private float maxRiseDistance = 0f;

    [Header("Start")]
    [Tooltip("Don't start rising until the player first moves, jumps or dashes, giving them time to read any messages.")]
    [SerializeField] private bool waitForPlayerToMove = true;
    [Tooltip("Seconds to wait before rising. When Wait For Player To Move is ticked, this counts from the player's first move; otherwise from the level start.")]
    [SerializeField] private float startDelay = 2f;
    [SerializeField] private string playerTag = "Player";

    private Rigidbody2D rb;
    private float startY;
    private float startTime;
    private bool started;

    private Player playerMovement;
    private Transform player;
    private float playerStartX;

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

        if (waitForPlayerToMove) FindPlayer();

        // Not waiting (or no player found): count the delay from the level start, like before
        if (player == null) Begin();
    }

    void FindPlayer()
    {
        GameObject found = GameObject.FindGameObjectWithTag(playerTag);
        if (found == null)
        {
            Debug.LogWarning($"RisingHazard on '{name}': couldn't find an object tagged '{playerTag}', so it starts rising without waiting.");
            return;
        }

        playerMovement = found.GetComponentInParent<Player>();
        player = playerMovement != null ? playerMovement.transform : found.transform;
        playerStartX = player.position.x;
    }

    void Update()
    {
        if (started || player == null) return;

        if (PlayerHasMoved()) Begin();
    }

    bool PlayerHasMoved()
    {
        // Pressing a move direction or jump counts straight away
        if (playerMovement != null)
        {
            if (playerMovement.MoveInput.sqrMagnitude > 0.01f || playerMovement.JumpPressedThisFrame) return true;
        }

        // Anything else that shifts the player sideways (like a dash) counts too
        return Mathf.Abs(player.position.x - playerStartX) > 0.25f;
    }

    void Begin()
    {
        started = true;
        startTime = Time.time + startDelay;
    }

    void FixedUpdate()
    {
        if (!started || Time.time < startTime) return;

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
