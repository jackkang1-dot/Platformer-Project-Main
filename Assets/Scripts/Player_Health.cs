using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tracks player HP, updates an optional UI Slider health bar, handles taking
/// damage with a brief invincibility window (sprite flicker), and death.
/// On death the player loses control and the current level restarts.
/// Enemies call TakeDamage() on this component.
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
    [Tooltip("Seconds to wait after dying before the level restarts.")]
    public float restartDelay = 1.5f;

    private SpriteRenderer sr;

    void Awake()
    {
        // Searches this object and its children, in case the sprite lives on a child object
        sr = GetComponentInChildren<SpriteRenderer>();
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
            isInvincible = true;
            invincibilityTimer = invincibilityDuration;
        }
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

    void Die()
    {
        isDead = true;
        Debug.Log("Player has died.");

        if (sr != null) sr.enabled = true;

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
            Invoke(nameof(RestartLevel), restartDelay);
        }
    }

    void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}