using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tracks player HP, updates an optional UI Slider health bar, handles taking
/// damage with a brief invincibility window (sprite flicker), and death.
/// On death the player loses control and the current level restarts.
/// Enemies call TakeDamage() on this component. Optional hurt and death sounds.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 3;
    private int currentHealth;
    private bool isDead = false;

    [Header("UI")]
    [Tooltip("Drag a UI Slider here to use it as the health bar (optional).")]
    public Slider healthBar;

    [Header("Invincibility")]
    [Tooltip("Brief invincibility after getting hit, so one collision doesn't drain multiple hits in a single frame.")]
    public float invincibilityDuration = 1f;
    private bool isInvincible = false;
    private float invincibilityTimer;

    [Header("Death")]
    [Tooltip("Reload the current level after the player dies.")]
    public bool restartLevelOnDeath = true;
    [Tooltip("Seconds to wait after dying before the level restarts. Keep it at least as long as the death animation and Death Sound, or they get cut off. Not used in levels with a YOU DIED screen (Death Screen script), which sets its own timing.")]
    public float restartDelay = 1.5f;
    [Tooltip("Name of the Trigger parameter in the player's Animator that plays the death animation (case-sensitive). Leave empty if there's no death animation.")]
    public string deathTrigger = "die";

    [Header("Sounds (optional)")]
    [Tooltip("Plays when the player takes damage but survives. Leave empty for no sound.")]
    public AudioClip hurtSound;
    [Tooltip("Plays when the player dies (enemies, water, pus, pits). Leave empty for no sound.")]
    public AudioClip deathSound;
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    private SpriteRenderer sr;
    private Animator anim;
    private AudioSource sfx;

    void Awake()
    {
        // Searches this object and its children, in case the sprite or Animator lives on a child object
        sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();

        // Its own speaker for hurt/death sounds
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f; // 2D: same volume wherever the camera is
    }

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    void Update()
    {
        if (!isInvincible) return;

        invincibilityTimer -= Time.deltaTime;

        // Simple flicker effect while invincible
        if (sr != null)
        {
            sr.enabled = !sr.enabled;
        }

        if (invincibilityTimer <= 0f)
        {
            isInvincible = false;
            if (sr != null) sr.enabled = true;
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead || isInvincible) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0);
        UpdateHealthBar();
        Debug.Log($"Player took {amount} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            PlaySound(hurtSound);
            isInvincible = true;
            invincibilityTimer = invincibilityDuration;
        }
    }

    // Instant death from hazards like water, spikes or pits. Ignores invincibility.
    public void Kill()
    {
        if (isDead) return;

        currentHealth = 0;
        UpdateHealthBar();
        Die();
    }

    // Optional: call this from health pickups
    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        if (healthBar == null) return;

        healthBar.maxValue = maxHealth;
        healthBar.value = currentHealth;
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && sfx != null) sfx.PlayOneShot(clip, soundVolume);
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Player has died.");

        // Stop any hit-flicker so the death animation stays visible
        isInvincible = false;
        if (sr != null) sr.enabled = true;

        PlayDeathAnimation();
        PlaySound(deathSound);

        // Take away control: switch off every other script on the player (movement, attack, etc.)
        foreach (MonoBehaviour script in GetComponents<MonoBehaviour>())
        {
            if (script != this) script.enabled = false;
        }

        // Stop sideways sliding, but let gravity keep working
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        if (restartLevelOnDeath)
        {
            // If the level has a YOU DIED screen, it shows itself and restarts the level;
            // otherwise restart after Restart Delay
            if (!DeathScreen.Show()) Invoke(nameof(RestartLevel), restartDelay);
        }
    }

    void PlayDeathAnimation()
    {
        if (anim == null || anim.runtimeAnimatorController == null || string.IsNullOrEmpty(deathTrigger)) return;

        bool hasDeathTrigger = false;

        foreach (AnimatorControllerParameter p in anim.parameters)
        {
            // Clear any attack/dash triggers still waiting, so they can't interrupt the death animation
            if (p.type == AnimatorControllerParameterType.Trigger && p.name != deathTrigger) anim.ResetTrigger(p.name);

            // Stop the running animation from being requested
            if (p.type == AnimatorControllerParameterType.Bool) anim.SetBool(p.name, false);

            if (p.name == deathTrigger && p.type == AnimatorControllerParameterType.Trigger) hasDeathTrigger = true;
        }

        if (hasDeathTrigger)
        {
            anim.SetTrigger(deathTrigger);
        }
        else
        {
            Debug.LogWarning($"PlayerHealth: the player's Animator has no Trigger called '{deathTrigger}'. Add it in the Animator's Parameters tab, or clear the Death Trigger field.");
        }
    }

    void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
