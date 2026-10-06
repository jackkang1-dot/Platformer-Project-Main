using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Quick horizontal dash with a cooldown. Press the dash key to burst in the
/// direction you're holding (or facing). While dashing, this script briefly
/// takes over from the movement script so the two don't fight over speed.
/// Plays the "dash" trigger on the player's Animator.
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
    private bool unlocked;
    private bool isDashing;
    private float dashEndTime;
    private float nextDashTime;
    private float savedGravity;
    private float dashDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        movement = GetComponent<Player>();

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
        if (!isDashing) return;

        float y = ignoreGravityWhileDashing ? 0f : rb.linearVelocity.y;
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, y);
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
}
