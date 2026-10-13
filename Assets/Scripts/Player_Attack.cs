using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple melee attack: press the attack key to damage every enemy inside a
/// small circle in front of the player. Works with the new Input System.
/// Each swing keeps hitting for a short moment (Swing Time), so enemies that walk
/// into it still get hit, and an enemy that bumps into the player from the front
/// mid-swing gets hit instead of hurting the player.
/// Includes Console debug messages so you can see exactly where an attack fails.
/// Optional attack sound.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackRange = 0.6f;
    [SerializeField] private float attackCooldown = 0.4f;
    [Tooltip("How long each swing keeps hitting, in seconds. Enemies that walk into the swing during this time still get hit, and enemies bumping into the player from the front get hit instead of hurting the player. Roughly match the attack animation.")]
    [SerializeField] private float swingTime = 0.25f;

    [Header("Targeting")]
    [Tooltip("Empty child object placed in front of the player. It flips with the player automatically.")]
    [SerializeField] private Transform attackPoint;
    [Tooltip("Only colliders on these layers can be hit (set this to your Enemy layer).")]
    [SerializeField] private LayerMask enemyLayers;

    [Header("Input")]
    [Tooltip("Attack controls. Defaults to Left Mouse Button, Q and J (any of them works). Add, remove or change bindings here in the Inspector.")]
    [SerializeField] private InputAction attackInput = CreateDefaultAttackInput();

    static InputAction CreateDefaultAttackInput()
    {
        InputAction action = new InputAction("Attack", InputActionType.Button);
        AddDefaultBindings(action);
        return action;
    }

    static void AddDefaultBindings(InputAction action)
    {
        action.AddBinding("<Mouse>/leftButton");
        action.AddBinding("<Keyboard>/q");
        action.AddBinding("<Keyboard>/j");
    }

    [Header("Animation")]
    [Tooltip("Name of the Trigger parameter in the player's Animator that plays the attack animation (case-sensitive).")]
    [SerializeField] private string attackTrigger = "attack";

    [Header("Sound (optional)")]
    [Tooltip("Plays every time the player attacks. Leave empty for no sound.")]
    [SerializeField] private AudioClip attackSound;
    [Range(0f, 1f)]
    [SerializeField] private float attackSoundVolume = 1f;

    [Header("Debugging")]
    [Tooltip("Prints attack messages to the Console. Turn off when everything works.")]
    [SerializeField] private bool debugLogs = true;

    private float nextAttackTime;
    private float swingEndTime = -1f;
    private Animator anim;
    private AudioSource sfx;

    // Enemies already hit by the current swing (each enemy is hit once per swing)
    private readonly HashSet<GoombaEnemy> hitThisSwing = new HashSet<GoombaEnemy>();

    bool Swinging => Time.time < swingEndTime;

    void Awake()
    {
        // Searches this object and its children, in case the Animator sits on a child object
        anim = GetComponentInChildren<Animator>();

        // Its own speaker for the attack sound
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f; // 2D: same volume wherever the camera is
    }

    void OnEnable()
    {
        // Safety net: if the Inspector copy of the action has no bindings, restore the defaults
        if (attackInput.bindings.Count == 0)
        {
            AddDefaultBindings(attackInput);
            Log("Attack Input had no bindings, so Left Mouse Button, Q and J were added automatically.");
        }

        attackInput.Enable();
    }

    void OnDisable()
    {
        attackInput.Disable();
        swingEndTime = -1f; // a switched-off attack (for example after dying) stops swinging
    }

    void Update()
    {
        if (attackInput.WasPressedThisFrame())
        {
            TryStartSwing();
        }
        else if (Swinging)
        {
            // Keep hitting anything that walks into the swing
            HitEnemiesInRange(false);
        }
    }

    void TryStartSwing()
    {
        Log("Attack key pressed.");

        if (Time.time < nextAttackTime)
        {
            Log("Attack is on cooldown.");
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        swingEndTime = Time.time + swingTime;
        hitThisSwing.Clear();

        PlayAttackSound();
        PlayAttackAnimation();
        HitEnemiesInRange(true);
    }

    void PlayAttackSound()
    {
        if (attackSound != null && sfx != null) sfx.PlayOneShot(attackSound, attackSoundVolume);
    }

    void PlayAttackAnimation()
    {
        if (anim == null || anim.runtimeAnimatorController == null || string.IsNullOrEmpty(attackTrigger)) return;

        anim.SetTrigger(attackTrigger);
    }

    // firstCheck is true on the frame the attack key is pressed, so the debug messages only print once per swing
    void HitEnemiesInRange(bool firstCheck)
    {
        if (attackPoint == null)
        {
            if (firstCheck) Debug.LogWarning("PlayerAttack: no Attack Point assigned. Drag your AttackPoint object into the Attack Point field.");
            return;
        }

        if (enemyLayers.value == 0)
        {
            if (firstCheck) Debug.LogWarning("PlayerAttack: Enemy Layers is set to Nothing. Set it to your Enemy layer.");
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        if (firstCheck) Log($"Attack found {hits.Length} collider(s) in range.");

        foreach (Collider2D hit in hits)
        {
            // Searches the hit object and its parents, in case the collider is on a child
            GoombaEnemy enemy = hit.GetComponentInParent<GoombaEnemy>();

            if (enemy != null)
            {
                HitEnemy(enemy);
            }
            else if (firstCheck)
            {
                Log($"Found {hit.name}, but neither it nor its parents have a GoombaEnemy script.");
            }
        }
    }

    void HitEnemy(GoombaEnemy enemy)
    {
        // Each enemy only takes one hit per swing
        if (!hitThisSwing.Add(enemy)) return;

        enemy.TakeDamage(attackDamage);
        Log($"Hit {enemy.name}.");
    }

    /// <summary>
    /// Called by an enemy when it bumps into the player. If the player is mid-swing
    /// and facing that enemy, the enemy takes the hit and the player is NOT hurt.
    /// Returns true when the swing handled the bump.
    /// </summary>
    public bool TryHitOnContact(GoombaEnemy enemy)
    {
        if (!enabled || !Swinging || enemy == null) return false;

        // Only enemies in front of the player (on the attack point's side) count
        float facing = attackPoint != null
            ? Mathf.Sign(attackPoint.position.x - transform.position.x)
            : Mathf.Sign(transform.localScale.x);
        float enemySide = enemy.transform.position.x - transform.position.x;
        if (enemySide * facing < 0f) return false;

        HitEnemy(enemy);
        return true;
    }

    void Log(string message)
    {
        if (debugLogs) Debug.Log("PlayerAttack: " + message);
    }

    // Shows the attack range in the Scene view for easy tuning
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
