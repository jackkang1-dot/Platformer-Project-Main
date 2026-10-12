using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple melee attack: press the attack key to damage every enemy inside a
/// small circle in front of the player. Works with the new Input System.
/// Includes Console debug messages so you can see exactly where an attack fails.
/// Optional attack sound.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackRange = 0.6f;
    [SerializeField] private float attackCooldown = 0.4f;

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
    private Animator anim;
    private AudioSource sfx;

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
    }

    void Update()
    {
        if (!attackInput.WasPressedThisFrame()) return;

        Log("Attack key pressed.");

        if (Time.time < nextAttackTime)
        {
            Log("Attack is on cooldown.");
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        PlayAttackSound();
        PlayAttackAnimation();
        Attack();
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

    void Attack()
    {
        if (attackPoint == null)
        {
            Debug.LogWarning("PlayerAttack: no Attack Point assigned. Drag your AttackPoint object into the Attack Point field.");
            return;
        }

        if (enemyLayers.value == 0)
        {
            Debug.LogWarning("PlayerAttack: Enemy Layers is set to Nothing. Set it to your Enemy layer.");
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        Log($"Attack found {hits.Length} collider(s) in range.");

        foreach (Collider2D hit in hits)
        {
            // Searches the hit object and its parents, in case the collider is on a child
            GoombaEnemy enemy = hit.GetComponentInParent<GoombaEnemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(attackDamage);
                Log($"Hit {enemy.name}.");
            }
            else
            {
                Log($"Found {hit.name}, but neither it nor its parents have a GoombaEnemy script.");
            }
        }
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
