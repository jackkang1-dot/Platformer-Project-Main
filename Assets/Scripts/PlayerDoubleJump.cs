using UnityEngine;

/// <summary>
/// Adds extra jumps in mid-air, using the same jump key as the normal jump.
/// Works alongside the Player movement script, which still handles the first jump.
/// Add it to the Player prefab with "Unlocked" OFF, then tick "Unlocked" on the
/// Player in the levels where the double jump should be available.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDoubleJump : MonoBehaviour
{
    [Header("Unlock")]
    [Tooltip("Tick this on the Player in levels where the double jump is available.")]
    [SerializeField] private bool unlocked = false;

    [Header("Double Jump")]
    [Tooltip("Upward speed of the mid-air jump. (The normal jump uses Jump Speed on the Player script.)")]
    [SerializeField] private float doubleJumpSpeed = 7f;
    [Tooltip("How many extra jumps the player gets before landing again.")]
    [SerializeField] private int extraJumps = 1;
    [Tooltip("Grace period after leaving the ground where the normal jump still applies. Keep it the same as Coyote Time on the Player script.")]
    [SerializeField] private float coyoteTime = 0.1f;

    [Header("Animation (optional)")]
    [Tooltip("Name of a Trigger in the Animator to play when double jumping. Leave empty if there's no animation for it.")]
    [SerializeField] private string doubleJumpTrigger = "";

    private Rigidbody2D rb;
    private Player movement;
    private Collider2D feetCollider;
    private Animator anim;
    private int jumpsLeft;
    private float lastGroundedTime = -100f;

    // Lets other scripts (like a pickup) unlock the double jump during a level
    public bool Unlocked
    {
        get { return unlocked; }
        set { unlocked = value; }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<Player>();
        feetCollider = GetComponent<BoxCollider2D>();
        anim = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        jumpsLeft = extraJumps;
    }

    void Update()
    {
        // The movement script is switched off during a dash and after death, so pause too
        if (movement == null || !movement.enabled) return;

        bool grounded = feetCollider != null && feetCollider.IsTouchingLayers(movement.GroundLayer);

        if (grounded)
        {
            jumpsLeft = extraJumps;
            lastGroundedTime = Time.time;
            return; // the normal jump handles jumping off the ground
        }

        if (!unlocked || jumpsLeft <= 0) return;

        // Just after leaving a ledge, the normal jump (coyote time) still applies
        if (Time.time - lastGroundedTime <= coyoteTime) return;

        if (!movement.JumpPressedThisFrame) return;

        jumpsLeft--;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpSpeed);

        if (anim != null && anim.runtimeAnimatorController != null && !string.IsNullOrEmpty(doubleJumpTrigger))
        {
            anim.SetTrigger(doubleJumpTrigger);
        }
    }
}
