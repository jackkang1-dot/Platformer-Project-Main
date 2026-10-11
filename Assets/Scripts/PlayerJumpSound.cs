using UnityEngine;

/// <summary>
/// Plays a sound when the player jumps, including the double jump.
/// Works alongside the movement script without changing it: it waits for the
/// jump key, then plays the sound only if the player actually shoots upward,
/// so pressing jump when a jump isn't possible stays silent.
/// Put it on the Player (prefab) and drag the sound into Jump Sound.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerJumpSound : MonoBehaviour
{
    [Tooltip("Plays when the player jumps.")]
    [SerializeField] private AudioClip jumpSound;
    [Tooltip("Optional different sound for the mid-air double jump. Leave empty to use the Jump Sound for both.")]
    [SerializeField] private AudioClip doubleJumpSound;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private Rigidbody2D rb;
    private Player movement;
    private Collider2D feetCollider;
    private AudioSource sfx;

    private bool waitingForJump;
    private bool pressedOnGround;
    private float waitUntil;
    private float upwardSpeedAtPress;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<Player>();
        feetCollider = GetComponent<BoxCollider2D>();

        // Its own speaker for the jump sound
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f; // 2D: same volume wherever the camera is
    }

    void Update()
    {
        // The movement script is switched off while dashing and after death
        if (movement == null || !movement.enabled)
        {
            waitingForJump = false;
            return;
        }

        if (movement.JumpPressedThisFrame)
        {
            waitingForJump = true;
            waitUntil = Time.time + 0.15f;
            upwardSpeedAtPress = rb.linearVelocity.y;
            pressedOnGround = feetCollider != null && feetCollider.IsTouchingLayers(movement.GroundLayer);
        }

        if (!waitingForJump) return;

        // The jump happened: the player is now moving up faster than when the key was pressed
        float upwardSpeed = rb.linearVelocity.y;
        if (upwardSpeed > 1f && upwardSpeed > upwardSpeedAtPress + 0.5f)
        {
            AudioClip clip = (!pressedOnGround && doubleJumpSound != null) ? doubleJumpSound : jumpSound;
            if (clip != null) sfx.PlayOneShot(clip, volume);
            waitingForJump = false;
        }
        else if (Time.time > waitUntil)
        {
            waitingForJump = false; // the key was pressed but no jump happened
        }
    }
}
