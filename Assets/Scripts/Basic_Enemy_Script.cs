using UnityEngine;

/// <summary>
/// Classic "Goomba-style" patrol enemy for 2D platformers.
/// Walks back and forth within a set patrol range, turns around at walls/ledges,
/// and can be defeated by stomping on it from above or by the player's attack.
/// Damages the player on side contact.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class GoombaEnemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public bool startMovingRight = false;

    [Header("Patrol Range")]
    [Tooltip("How far (in world units) the enemy walks from its starting spot in each direction before turning around. Set to 0 for no limit.")]
    public float patrolRange = 3f;

    [Header("Ground & Wall Detection")]
    [Tooltip("Which layers count as 'ground' for wall/ledge checks.")]
    public LayerMask groundLayer;
    [Tooltip("Empty child object at the enemy's feet, offset slightly to one side (its side flips automatically).")]
    public Transform groundCheck;
    [Tooltip("Empty child object at the enemy's front, roughly chest height, offset to one side (its side flips automatically).")]
    public Transform wallCheck;
    public float groundCheckDistance = 0.5f;
    public float wallCheckDistance = 0.2f;

    [Header("Stomp Settings")]
    public string playerTag = "Player";
    [Tooltip("How much upward bounce the player gets after stomping this enemy.")]
    public float stompBounceForce = 8f;

    [Header("Damage")]
    public int damageToPlayer = 1;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool movingRight;
    private bool isDead = false;
    private float startX;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        startX = transform.position.x;
        movingRight = startMovingRight;
        UpdateFacing();
    }

    void FixedUpdate()
    {
        if (isDead) return;

        CheckTurnConditions();

        float direction = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    void CheckTurnConditions()
    {
        bool wallAhead = false;
        bool groundAhead = true; // assume ground exists unless a check says otherwise
        bool rangeLimitReached = false;

        // Patrol range: turn around once we've walked far enough from the starting spot
        if (patrolRange > 0f)
        {
            float distanceFromStart = transform.position.x - startX;
            rangeLimitReached = movingRight ? distanceFromStart >= patrolRange
                                            : distanceFromStart <= -patrolRange;
        }

        if (wallCheck != null)
        {
            Vector2 dir = movingRight ? Vector2.right : Vector2.left;
            wallAhead = Physics2D.Raycast(wallCheck.position, dir, wallCheckDistance, groundLayer);
        }

        if (groundCheck != null)
        {
            groundAhead = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
        }

        if (wallAhead || !groundAhead || rangeLimitReached)
        {
            Flip();
        }
    }

    void Flip()
    {
        movingRight = !movingRight;
        UpdateFacing();
    }

    void UpdateFacing()
    {
        if (sr != null)
        {
            sr.flipX = !movingRight; // assumes the sprite's default artwork faces right
        }

        // Keep the check objects on the side the enemy is walking toward
        float sign = movingRight ? 1f : -1f;
        MirrorCheckX(groundCheck, sign);
        MirrorCheckX(wallCheck, sign);
    }

    void MirrorCheckX(Transform check, float sign)
    {
        if (check == null) return;

        Vector3 p = check.localPosition;
        p.x = Mathf.Abs(p.x) * sign;
        check.localPosition = p;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (!collision.gameObject.CompareTag(playerTag)) return;

        bool stomped = false;

        // A contact normal pointing mostly downward means the player landed on top of us
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f)
            {
                stomped = true;
                break;
            }
        }

        if (stomped)
        {
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 v = playerRb.linearVelocity;
                v.y = stompBounceForce;
                playerRb.linearVelocity = v;
            }

            Die();
        }
        else
        {
            DamagePlayer(collision.gameObject);
        }
    }

    void DamagePlayer(GameObject player)
    {
        Debug.Log($"{gameObject.name} hit the player for {damageToPlayer} damage!");
        player.GetComponent<PlayerHealth>()?.TakeDamage(damageToPlayer);
    }

    // Called by PlayerAttack (or anything else that can hurt this enemy).
    public void TakeDamage(int amount)
    {
        if (isDead) return;
        Die(); // any hit defeats it
    }

    void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Simple squash effect before removal
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 0.3f, transform.localScale.z);

        Debug.Log($"{gameObject.name} was defeated!");
        Destroy(gameObject, 0.3f);
    }

    // Draws the check rays and the patrol range in the Scene view for easy tuning
    void OnDrawGizmosSelected()
    {
        bool facingRight = Application.isPlaying ? movingRight : startMovingRight;

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckDistance);
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.red;
            Vector3 dir = facingRight ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + dir * wallCheckDistance);
        }

        if (patrolRange > 0f)
        {
            float centerX = Application.isPlaying ? startX : transform.position.x;
            Vector3 leftEnd = new Vector3(centerX - patrolRange, transform.position.y, transform.position.z);
            Vector3 rightEnd = new Vector3(centerX + patrolRange, transform.position.y, transform.position.z);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(leftEnd, rightEnd);
            Gizmos.DrawWireSphere(leftEnd, 0.15f);
            Gizmos.DrawWireSphere(rightEnd, 0.15f);
        }
    }
}