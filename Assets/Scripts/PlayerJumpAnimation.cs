using UnityEngine;

/// <summary>
/// Tells the player's Animator when the player is in the air, so the jump
/// animation plays while jumping or falling and stops on landing.
/// It sets a Bool parameter (default name "jumping") every frame.
/// Works alongside the movement script without changing it.
/// Put it on the Player (prefab).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerJumpAnimation : MonoBehaviour
{
    [Tooltip("Name of the Bool parameter in the player's Animator (case-sensitive).")]
    [SerializeField] private string jumpingBool = "jumping";
    [Tooltip("When walking off a ledge, how long the player must be falling before the jump animation plays. Stops it flickering over small bumps and tile seams.")]
    [SerializeField] private float fallDelay = 0.1f;

    private Rigidbody2D rb;
    private Player movement;
    private Collider2D feetCollider;
    private Animator anim;
    private bool hasParameter;
    private float airTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<Player>();
        feetCollider = GetComponent<BoxCollider2D>();
        anim = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        // Check once that the Animator has the parameter, instead of filling the Console with warnings
        if (anim == null || anim.runtimeAnimatorController == null) return;

        foreach (AnimatorControllerParameter p in anim.parameters)
        {
            if (p.name == jumpingBool && p.type == AnimatorControllerParameterType.Bool) hasParameter = true;
        }

        if (!hasParameter)
        {
            Debug.LogWarning($"PlayerJumpAnimation: the player's Animator has no Bool parameter named '{jumpingBool}'. Add it in the Animator window's Parameters tab.");
        }
    }

    void Update()
    {
        if (!hasParameter || movement == null) return;

        bool grounded = feetCollider != null && feetCollider.IsTouchingLayers(movement.GroundLayer);
        airTime = grounded ? 0f : airTime + Time.deltaTime;

        // Jumping up counts right away; falling off a ledge counts after a short moment
        bool inAir = !grounded && (rb.linearVelocity.y > 0.1f || airTime >= fallDelay);
        anim.SetBool(jumpingBool, inAir);
    }
}
