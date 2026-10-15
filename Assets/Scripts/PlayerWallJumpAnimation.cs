using UnityEngine;

/// <summary>
/// Plays the wall jump animation when the player jumps off a wall.
/// The game's wall "climbing" works because the Player script lets you jump again
/// whenever your feet touch a wall. This script spots those jumps (pressing jump
/// while touching a wall from the side with nothing underneath) and plays the
/// wall jump state from its first frame.
/// Put it on the Player (prefab). It works alongside the movement script without changing it.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerWallJumpAnimation : MonoBehaviour
{
    [Tooltip("Name of the wall jump state (the box) in the player's Animator, exactly as it appears there.")]
    [SerializeField] private string wallJumpStateName = "NewPlayerWallJump";

    private Rigidbody2D rb;
    private Player movement;
    private Collider2D feetCollider;
    private Animator anim;
    private int stateHash;
    private bool hasState;
    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<Player>();
        feetCollider = GetComponent<BoxCollider2D>();
        anim = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        // Check once that the Animator has the state, instead of filling the Console with warnings
        if (string.IsNullOrEmpty(wallJumpStateName) || anim == null || anim.runtimeAnimatorController == null) return;

        // Accept either the plain name or the full path ("Base Layer.Name")
        string[] names = { wallJumpStateName, "Base Layer." + wallJumpStateName };
        foreach (string stateName in names)
        {
            int hash = Animator.StringToHash(stateName);
            if (anim.HasState(0, hash))
            {
                stateHash = hash;
                hasState = true;
                return;
            }
        }

        Debug.LogWarning($"PlayerWallJumpAnimation: the player's Animator has no state named '{wallJumpStateName}'. Drag the wall jump clip into the Animator, then type the state's exact name into Wall Jump State Name.");
    }

    void Update()
    {
        // The movement script is switched off during a dash and after death
        if (!hasState || movement == null || !movement.enabled) return;
        if (!movement.JumpPressedThisFrame) return;

        // The Player script only jumps when the feet are touching something
        if (feetCollider == null || !feetCollider.IsTouchingLayers(movement.GroundLayer)) return;

        if (TouchingWallOnly())
        {
            anim.Play(stateHash, 0, 0f);
        }
    }

    // True when the player touches a wall at the side and isn't standing on anything
    bool TouchingWallOnly()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(movement.GroundLayer);

        int count = rb.GetContacts(filter, contacts);
        bool wall = false;

        for (int i = 0; i < count; i++)
        {
            Vector2 normal = contacts[i].normal;

            // Something flat above or below: this is a normal jump off the floor
            if (Mathf.Abs(normal.y) > 0.7f) return false;

            // Something upright at the side: a wall
            if (Mathf.Abs(normal.x) > 0.7f) wall = true;
        }

        return wall;
    }
}
