using UnityEngine;

/// <summary>
/// Tracks player HP, handles taking damage with a brief invincibility window
/// (with a sprite flicker), and death. Called by enemies via TakeDamage().
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Invincibility")]
    [Tooltip("Brief invincibility after getting hit, so one collision doesn't drain multiple hits in a single frame.")]
    public float invincibilityDuration = 1f;
    private bool isInvincible = false;
    private float invincibilityTimer;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        currentHealth = maxHealth;
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
        if (isInvincible) return;

        currentHealth -= amount;
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

    void Die()
    {
        Debug.Log("Player has died.");
        // Add your game-over logic here, e.g. reload the current scene:
        // UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}