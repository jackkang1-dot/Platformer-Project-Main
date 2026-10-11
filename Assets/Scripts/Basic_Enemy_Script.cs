using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Classic "Goomba-style" patrol enemy for 2D platformers.
/// Walks back and forth within a set patrol range, turns around at walls/ledges,
/// and can be defeated by stomping on it from above or by the player's attack.
/// Damages the player on side contact.
/// Regular enemies keep Max Health at 1 (any hit defeats them). A boss gets more
/// health, an optional health bar, and can add the EnemyShooter script to shoot.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class GoombaEnemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public bool startMovingRight = false;

    [Header("Art")]
    [Tooltip("Tick this if the enemy's artwork faces RIGHT in the image file. Untick it if the artwork faces LEFT.")]
    public bool artFacesRight = true;

    [Header("Patrol Range")]
    [Tooltip("How far (in world units) the enemy walks from its starting spot in each direction before turning around. Set to 0 for no limit.")]
    public float patrolRange = 3f;

    [Header("Ground & Wall Detection (optional)")]
    [Tooltip("Which layers count as 'ground' for wall/ledge checks.")]
    public LayerMask groundLayer;
    [Tooltip("Empty child object at the enemy's feet, offset slightly to one side (its side flips automatically).")]
    public Transform groundCheck;
    [Tooltip("Empty child object at the enemy's front, roughly chest height, offset to one side (its side flips automatically).")]
    public Transform wallCheck;
    public float groundCheckDistance = 0.5f;
    public float wallCheckDistance = 0.2f;

    [Header("Health")]
    [Tooltip("Hits needed to defeat this enemy. Leave at 1 for regular enemies. Give a boss more.")]
    public int maxHealth = 1;
    [Tooltip("After taking a hit, ignore further hits for this many seconds, so one spear swing or stomp only counts once.")]
    public float hurtCooldown = 0.2f;
    [Tooltip("Color the enemy flashes when it takes a hit but survives.")]
    public Color hitFlashColor = Color.red;
    [Tooltip("Optional UI Slider to show this enemy's health (for a boss). Leave empty for regular enemies.")]
    public Slider healthBar;

    [Header("Stomp Settings")]
    public string playerTag = "Player";
    [Tooltip("Untick to make this enemy hurt the player even when landed on from above (e.g. a spiky boss).")]
    public bool canBeStomped = true;
    [Tooltip("Damage a stomp deals to this enemy.")]
    public int stompDamage = 1;
    [Tooltip("How much upward bounce the player gets after stomping this enemy.")]
    public float stompBounceForce = 8f;
    [Tooltip("How high the player's feet must be to count as a stomp, as a fraction of this enemy's height (0 = its bottom, 1 = its top). Lower = more forgiving.")]
    [Range(0f, 1f)]
    public float stompZone = 0.5f;

    [Header("Damage")]
    public int damageToPlayer = 1;

    [Header("Death")]
    [Tooltip("Name of the Trigger parameter in the enemy's Animator that plays the death animation (case-sensitive).")]
    public string deathTrigger = "die";
    [Tooltip("Seconds to wait before removing the enemy. Match this to the length of the death animation.")]
    public float deathDuration = 0.5f;
    [Tooltip("Optional objects to switch ON when this enemy is defeated, such as a level exit or a 'You Win' message. Untick them in the scene so they start hidden.")]
    public GameObject[] activateOnDeath;

    [Header("Sounds (optional)")]
    [Tooltip("Plays while the enemy walks. Can be a looping sound or a single footstep. Leave empty for no sound.")]
    public AudioClip moveSound;
    [Tooltip("Tick if Move Sound is one continuous loop. Untick if it's a single footstep, which then repeats every Step Interval.")]
    public bool moveSoundLoops = true;
    [Tooltip("Seconds between footsteps (only used when Move Sound Loops is off).")]
    public float stepInterval = 0.4f;
    [Tooltip("Plays when the enemy is defeated. Leave empty for no sound.")]
    public AudioClip deathSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;
    [Tooltip("How far from the camera (in units) this enemy can be heard. Close enemies play at full volume and fade out with distance, so off-screen enemies stay quiet.")]
    public float hearingDistance = 10f;

    private AudioSource moveSource;
    private Transform listener;
    private float nextStepTime;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private SpriteRenderer sr;
    private Animator anim;
    private Color normalColor = Color.white;
    private bool movingRight;
    private bool isDead = false;
    private float startX;
    private int currentHealth;
    private float nextHurtTime;

    // Lets other scripts (like EnemyShooter) know when this enemy has been defeated
    public bool IsDead => isDead;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        // Searches this object and its children, in case the art lives on a child object
        sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();

        if (sr != null) normalColor = sr.color;

        // This enemy's own speaker for its movement sound
        moveSource = gameObject.AddComponent<AudioSource>();
        moveSource.playOnAwake = false;
        moveSource.spatialBlend = 0f;
    }

    void Start()
    {
        startX = transform.position.x;
        movingRight = startMovingRight;
        currentHealth = Mathf.Max(1, maxHealth);
        UpdateHealthBar();
        UpdateFacing();

        // The Main Camera carries the "ears" (Audio Listener), so distance is measured from it
        if (Camera.main != null) listener = Camera.main.transform;
    }

    void Update()
    {
        UpdateMoveSound();
    }

    // ---------- Sounds ----------

    void UpdateMoveSound()
    {
        if (moveSound == null || moveSource == null) return;

        bool walking = !isDead && Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        float volume = soundVolume * DistanceVolume();
        moveSource.panStereo = DistancePan();

        if (moveSoundLoops)
        {
            if (walking && volume > 0.01f)
            {
                moveSource.volume = volume;
                if (!moveSource.isPlaying)
                {
                    moveSource.clip = moveSound;
                    moveSource.loop = true;
                    // Start at a random point so a group of enemies doesn't sound perfectly in sync
                    moveSource.time = Random.Range(0f, moveSound.length);
                    moveSource.Play();
                }
            }
            else if (moveSource.isPlaying)
            {
                moveSource.Stop();
            }
        }
        else if (walking && volume > 0.01f && Time.time >= nextStepTime)
        {
            moveSource.pitch = 1f + Random.Range(-0.08f, 0.08f);
            moveSource.PlayOneShot(moveSound, volume);
            nextStepTime = Time.time + stepInterval;
        }
    }

    // 1 when the enemy is near the camera, fading to 0 at Hearing Distance
    float DistanceVolume()
    {
        if (listener == null || hearingDistance <= 0f) return 1f;

        float distance = Vector2.Distance(listener.position, transform.position);
        return Mathf.InverseLerp(hearingDistance, hearingDistance * 0.3f, distance);
    }

    // Slightly louder in the left or right speaker depending on which side of the screen the enemy is on
    float DistancePan()
    {
        if (listener == null || hearingDistance <= 0f) return 0f;

        return Mathf.Clamp((transform.position.x - listener.position.x) / hearingDistance, -1f, 1f) * 0.5f;
    }

    void PlayDeathSound()
    {
        if (deathSound == null) return;

        // Played from a separate temporary object, because this enemy is removed
        // shortly after dying and would cut the sound off
        GameObject soundObject = new GameObject(name + " Death Sound");
        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.panStereo = DistancePan();
        source.PlayOneShot(deathSound, soundVolume * DistanceVolume());
        Destroy(soundObject, deathSound.length + 0.1f);
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
            // Flip the art only when it's pointing the opposite way from where we're walking
            sr.flipX = artFacesRight ? !movingRight : movingRight;
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

        Rigidbody2D playerRb = collision.rigidbody;

        if (canBeStomped && IsStomp(collision, playerRb))
        {
            if (playerRb != null)
            {
                Vector2 v = playerRb.linearVelocity;
                v.y = stompBounceForce;
                playerRb.linearVelocity = v;
            }

            TakeDamage(stompDamage);
        }
        else
        {
            DamagePlayer(collision.gameObject);
        }
    }

    bool IsStomp(Collision2D collision, Rigidbody2D playerRb)
    {
        // 1) A contact pointing mostly straight down means the player landed on top of us
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f) return true;
        }

        // 2) Forgiving check for landings on our edge (a rounded capsule touches at an angle).
        //    Counts as a stomp if the player isn't rising and their feet are above the stomp zone.
        bool playerRising = playerRb != null && playerRb.linearVelocity.y > 0.5f;
        if (playerRising || bodyCollider == null) return false;

        Bounds enemy = bodyCollider.bounds;
        float stompLine = enemy.min.y + enemy.size.y * stompZone;
        float playerFeet = collision.collider.bounds.min.y;

        return playerFeet >= stompLine;
    }

    void DamagePlayer(GameObject player)
    {
        Debug.Log($"{gameObject.name} hit the player for {damageToPlayer} damage!");
        player.GetComponent<PlayerHealth>()?.TakeDamage(damageToPlayer);
    }

    // Called by PlayerAttack, stomps, or anything else that can hurt this enemy.
    public void TakeDamage(int amount)
    {
        if (isDead) return;
        if (Time.time < nextHurtTime) return; // still recovering from the last hit

        currentHealth -= Mathf.Max(1, amount);
        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        nextHurtTime = Time.time + hurtCooldown;
        Debug.Log($"{gameObject.name} took a hit. Health: {currentHealth}/{maxHealth}");

        if (sr != null) StartCoroutine(HitFlash());
    }

    IEnumerator HitFlash()
    {
        sr.color = hitFlashColor;
        yield return new WaitForSeconds(0.1f);
        if (sr != null) sr.color = normalColor;
    }

    void UpdateHealthBar()
    {
        if (healthBar == null) return;

        healthBar.maxValue = Mathf.Max(1, maxHealth);
        healthBar.value = Mathf.Max(0, currentHealth);
    }

    void Die()
    {
        isDead = true;

        // Freeze the body in place: no gravity, no collisions, so it can't hurt the
        // player or fall through the floor while the death animation plays
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        if (sr != null) sr.color = normalColor;

        if (moveSource != null) moveSource.Stop();
        PlayDeathSound();

        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.SetTrigger(deathTrigger);
        }
        else
        {
            // No Animator set up: fall back to a simple squash effect
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 0.3f, transform.localScale.z);
        }

        if (healthBar != null) healthBar.gameObject.SetActive(false);

        if (activateOnDeath != null)
        {
            foreach (GameObject obj in activateOnDeath)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        Debug.Log($"{gameObject.name} was defeated!");
        Destroy(gameObject, deathDuration);
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
