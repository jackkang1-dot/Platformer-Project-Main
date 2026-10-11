using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Quick horizontal dash with a cooldown. Press the dash key to burst in the
/// direction you're holding (or facing). While dashing, this script briefly
/// takes over from the movement script so the two don't fight over speed.
/// Plays the "dash" trigger on the player's Animator.
/// The dash is also an attack: enemies the player dashes into take damage,
/// and the player passes through them instead of taking contact damage.
/// The dash only works from the level set in "Unlocked From Scene Index" onward.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    [Header("Unlock")]
    [Tooltip("The first level where the player can dash, using its number in File > Build Profiles > Scene List (the first scene is 0). Every level after it has the dash too. Set to 0 to allow dashing in every level.")]
    [SerializeField] private int unlockedFromSceneIndex = 1;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 12f;
    [Tooltip("How long the dash lasts, in seconds.")]
    [SerializeField] private float dashDuration = 0.2f;
    [Tooltip("Seconds before you can dash again (counted from the start of a dash).")]
    [SerializeField] private float dashCooldown = 0.6f;
    [Tooltip("Allow dashing while in the air.")]
    [SerializeField] private bool allowAirDash = true;
    [Tooltip("Turn off gravity during the dash so it goes straight across.")]
    [SerializeField] private bool ignoreGravityWhileDashing = true;

    [Header("Dash Attack")]
    [Tooltip("Enemies the player dashes into take damage.")]
    [SerializeField] private bool dashIsAttack = true;
    [Tooltip("Damage dealt to each enemy hit by a dash. (Regular enemies die from any hit.)")]
    [SerializeField] private int dashDamage = 1;
    [Tooltip("How far in front of the player's body the dash reaches, in units. Shown as an orange box in the Scene view when the Player is selected.")]
    [SerializeField] private float dashHitReach = 0.4f;
    [Tooltip("Let the player pass through enemies the dash hits (like a boss that survives the hit) instead of bumping into them and taking contact damage.")]
    [SerializeField] private bool passThroughHitEnemies = true;

    [Header("Ground Check (only used when Allow Air Dash is off)")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("The small feet BoxCollider2D. Leave empty to use the first BoxCollider2D on the player.")]
    [SerializeField] private Collider2D feetCollider;

    [Header("Input")]
    [Tooltip("Defaults to Left Shift. Change the binding here in the Inspector if you like.")]
    [SerializeField] private InputAction dashAction = new InputAction("Dash", InputActionType.Button, "<Keyboard>/leftShift");

    [Header("Animation")]
    [Tooltip("Name of the Trigger parameter in the player's Animator that plays the dash animation (case-sensitive).")]
    [SerializeField] private string dashTrigger = "dash";

    private Rigidbody2D rb;
    private Animator anim;
    private Player movement;
    private Collider2D[] ownColliders;
    private bool unlocked;
    private bool isDashing;
    private float dashEndTime;
    private float nextDashTime;
    private float savedGravity;
    private float dashDirection;

    // Enemies already hit during the current dash (each enemy is hit once per dash)
    private readonly HashSet<GoombaEnemy> hitThisDash = new HashSet<GoombaEnemy>();
    // Player/enemy collider pairs set to pass through each other, restored once they're apart
    private readonly List<KeyValuePair<Collider2D, Collider2D>> ignoredPairs = new List<KeyValuePair<Collider2D, Collider2D>>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        movement = GetComponent<Player>();
        ownColliders = GetComponentsInChildren<Collider2D>();

        if (feetCollider == null) feetCollider = GetComponent<BoxCollider2D>();
    }

    void Start()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (sceneIndex < 0)
        {
            Debug.LogWarning("PlayerDash: this level isn't in File > Build Profiles > Scene List, so it can't tell which level this is. Add the scene to the list.");
        }
        else
        {
            unlocked = sceneIndex >= unlockedFromSceneIndex;
        }
    }

    void OnEnable()
    {
        // Safety net: if the Inspector copy of the action has no binding, restore Left Shift
        if (dashAction.bindings.Count == 0) dashAction.AddBinding("<Keyboard>/leftShift");
        dashAction.Enable();
    }

    void OnDisable()
    {
        dashAction.Disable();

        // If something switches this script off mid-dash (like the player dying),
        // put gravity back but leave the movement script alone.
        if (isDashing)
        {
            isDashing = false;
            rb.gravityScale = savedGravity;
        }

        RestoreAllCollisions();
    }

    void Update()
    {
        if (!unlocked) return;

        if (isDashing)
        {
            if (Time.time >= dashEndTime) EndDash();
            return;
        }

        if (dashAction.WasPressedThisFrame() && Time.time >= nextDashTime && CanDashNow())
        {
            StartDash();
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            float y = ignoreGravityWhileDashing ? 0f : rb.linearVelocity.y;
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, y);

            if (dashIsAttack) DashHitCheck();
        }
        else if (ignoredPairs.Count > 0)
        {
            RestoreCollisionsWhenClear();
        }
    }

    bool CanDashNow()
    {
        if (allowAirDash) return true;
        if (feetCollider == null || groundLayer.value == 0) return true;

        return feetCollider.IsTouchingLayers(groundLayer);
    }

    void StartDash()
    {
        // Dash toward the held direction, or the way the player is facing if no key is held
        float input = movement != null ? movement.MoveInput.x : 0f;
        dashDirection = Mathf.Abs(input) > 0.1f ? Mathf.Sign(input) : Mathf.Sign(transform.localScale.x);

        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;
        hitThisDash.Clear();

        // Pause the movement script so it doesn't slow the dash down
        if (movement != null) movement.enabled = false;

        savedGravity = rb.gravityScale;
        if (ignoreGravityWhileDashing) rb.gravityScale = 0f;

        if (anim != null && anim.runtimeAnimatorController != null && !string.IsNullOrEmpty(dashTrigger))
        {
            anim.SetTrigger(dashTrigger);
        }
    }

    void EndDash()
    {
        isDashing = false;
        rb.gravityScale = savedGravity;

        if (movement != null) movement.enabled = true;
    }

    // ---------- Dash attack ----------

    void DashHitCheck()
    {
        GetHitBox(dashDirection, out Vector2 center, out Vector2 size);

        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, 0f))
        {
            // (Defeated enemies stop taking part in physics, so they never show up here)
            GoombaEnemy enemy = hit.GetComponentInParent<GoombaEnemy>();
            if (enemy == null || hitThisDash.Contains(enemy)) continue;

            hitThisDash.Add(enemy);

            // Pass through it so bumping into it doesn't count as the enemy touching the player
            if (passThroughHitEnemies) IgnoreEnemy(enemy);

            enemy.TakeDamage(dashDamage);
        }
    }

    // The area the dash hits: the player's body, stretched forward by Dash Hit Reach
    void GetHitBox(float direction, out Vector2 center, out Vector2 size)
    {
        Bounds body = GetBodyBounds();
        size = new Vector2(body.size.x + dashHitReach, body.size.y);
        center = (Vector2)body.center + new Vector2(direction * dashHitReach * 0.5f, 0f);
    }

    Bounds GetBodyBounds()
    {
        Collider2D[] colliders = ownColliders != null ? ownColliders : GetComponentsInChildren<Collider2D>();
        Bounds bounds = new Bounds(transform.position, Vector3.one);
        bool found = false;

        foreach (Collider2D col in colliders)
        {
            // Solid body colliders only (skips the feet check trigger)
            if (col == null || col.isTrigger || !col.enabled) continue;

            if (!found)
            {
                bounds = col.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(col.bounds);
            }
        }

        return bounds;
    }

    void IgnoreEnemy(GoombaEnemy enemy)
    {
        foreach (Collider2D enemyCol in enemy.GetComponentsInChildren<Collider2D>())
        {
            foreach (Collider2D own in ownColliders)
            {
                if (own == null || enemyCol == null) continue;

                Physics2D.IgnoreCollision(own, enemyCol, true);
                ignoredPairs.Add(new KeyValuePair<Collider2D, Collider2D>(own, enemyCol));
            }
        }
    }

    // After the dash, let the player and enemy bump into each other again once they're no longer overlapping
    void RestoreCollisionsWhenClear()
    {
        for (int i = ignoredPairs.Count - 1; i >= 0; i--)
        {
            Collider2D own = ignoredPairs[i].Key;
            Collider2D other = ignoredPairs[i].Value;

            if (own == null || other == null)
            {
                ignoredPairs.RemoveAt(i); // the enemy was removed
                continue;
            }

            if (own.Distance(other).isOverlapped) continue; // still inside it, wait

            Physics2D.IgnoreCollision(own, other, false);
            ignoredPairs.RemoveAt(i);
        }
    }

    void RestoreAllCollisions()
    {
        foreach (KeyValuePair<Collider2D, Collider2D> pair in ignoredPairs)
        {
            if (pair.Key != null && pair.Value != null)
            {
                Physics2D.IgnoreCollision(pair.Key, pair.Value, false);
            }
        }

        ignoredPairs.Clear();
    }

    // Shows the dash attack's reach in the Scene view when the Player is selected
    void OnDrawGizmosSelected()
    {
        if (!dashIsAttack) return;

        float direction = Application.isPlaying && isDashing ? dashDirection : Mathf.Sign(transform.localScale.x);
        GetHitBox(direction, out Vector2 center, out Vector2 size);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}
