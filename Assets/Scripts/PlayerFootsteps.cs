using UnityEngine;

/// <summary>
/// Plays running sounds while the player runs along the ground.
/// Works with either a looping "running" sound or a single footstep sound
/// that repeats at a steady pace. Goes quiet while jumping, dashing or dead.
/// Put it on the Player (prefab) and drag the sound into Run Sound.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerFootsteps : MonoBehaviour
{
    [Tooltip("The running or footstep sound.")]
    [SerializeField] private AudioClip runSound;
    [Tooltip("Tick if the sound is one continuous running loop. Untick if it's a single footstep, which then repeats every Step Interval.")]
    [SerializeField] private bool isLoopingSound = false;
    [Tooltip("Seconds between footsteps (only used when Is Looping Sound is off). Lower = faster steps.")]
    [SerializeField] private float stepInterval = 0.3f;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.6f;
    [Tooltip("Small random pitch change on each footstep so they don't all sound identical. 0 = off.")]
    [Range(0f, 0.3f)]
    [SerializeField] private float pitchVariation = 0.1f;
    [Tooltip("How fast the player must be moving sideways to count as running.")]
    [SerializeField] private float minRunSpeed = 0.1f;

    private Rigidbody2D rb;
    private Player movement;
    private Collider2D feetCollider;
    private AudioSource source;
    private float nextStepTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<Player>();
        feetCollider = GetComponent<BoxCollider2D>();

        // Its own speaker, so stopping footsteps never cuts off other player sounds
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D: same volume wherever the camera is
    }

    void Update()
    {
        if (runSound == null) return;

        bool running = IsRunning();

        if (isLoopingSound)
        {
            if (running && !source.isPlaying)
            {
                source.clip = runSound;
                source.loop = true;
                source.volume = volume;
                source.pitch = 1f;
                source.Play();
            }
            else if (!running && source.isPlaying)
            {
                source.Stop();
            }
        }
        else if (running && Time.time >= nextStepTime)
        {
            source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            source.PlayOneShot(runSound, volume);
            nextStepTime = Time.time + stepInterval;
        }
    }

    bool IsRunning()
    {
        // The movement script is switched off while dashing and after death
        if (movement == null || !movement.enabled) return false;

        bool grounded = feetCollider != null && feetCollider.IsTouchingLayers(movement.GroundLayer);
        return grounded && Mathf.Abs(rb.linearVelocity.x) > minRunSpeed;
    }

    void OnDisable()
    {
        if (source != null) source.Stop();
    }
}
