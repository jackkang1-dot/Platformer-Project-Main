using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mid-level checkpoint. When the player touches it, it becomes the place they
/// come back to after dying, instead of the start of the level.
/// Put it on your checkpoint object, which needs a Collider2D with Is Trigger ticked.
/// The checkpoint is forgotten when the player leaves the level (next level,
/// main menu, credits), so replaying a level starts from the beginning.
/// It also remembers the double jump, so a player who had it at the checkpoint
/// still has it after respawning.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("Respawn")]
    [Tooltip("Where the player reappears, relative to this object. Shown as a green circle in the Scene view when the checkpoint is selected. Raise Y if the player appears stuck in the floor.")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 0.5f);
    [SerializeField] private string playerTag = "Player";

    [Header("When reached (all optional)")]
    [Tooltip("Sprite to switch to once the checkpoint is reached, such as a lit-up or raised-flag version. Leave empty to keep the same look.")]
    [SerializeField] private Sprite reachedSprite;
    [Tooltip("If the checkpoint is animated: name of a Trigger parameter in its Animator to fire when reached. Leave empty if it isn't animated.")]
    [SerializeField] private string reachedTrigger = "";
    [Tooltip("Plays when the checkpoint is reached for the first time.")]
    [SerializeField] private AudioClip reachedSound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;
    [Tooltip("On-screen message such as 'CHECKPOINT!': drag in a Canvas that has the Unlock Notification script (with its Show On Level Start unticked).")]
    [SerializeField] private UnlockNotification message;

    // Remembered between deaths. Static values survive the level reloading.
    private static string savedScene;
    private static Vector3 savedPosition;
    private static bool savedDoubleJump;

    private SpriteRenderer sr;
    private Animator anim;
    private AudioSource sfx;
    private bool reached;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetMemory()
    {
        // Start every play session with no checkpoint. Needed because this project's
        // Enter Play Mode setting doesn't reset static values on its own.
        savedScene = null;
        savedDoubleJump = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Loading a different scene means the player left the level, so forget the checkpoint.
        // Restarting the same level after dying keeps it.
        if (mode == LoadSceneMode.Single && scene.name != savedScene) savedScene = null;
    }

    Vector3 SpawnPosition => transform.position + (Vector3)spawnOffset;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f; // 2D: same volume wherever the camera is

        if (!GetComponent<Collider2D>().isTrigger)
        {
            Debug.LogWarning($"Checkpoint '{name}': tick Is Trigger on its collider, otherwise the player bumps into it instead of passing through.");
        }

        // After dying, the level reloads: if this is the saved checkpoint, put the player here.
        // Done in Awake so it happens before the camera and other scripts start.
        if (IsSavedCheckpoint())
        {
            reached = true;
            RespawnPlayerHere();
        }
    }

    void Start()
    {
        // Show the saved checkpoint as already reached (quietly: no sound or message)
        if (reached) ShowReachedLook();
    }

    bool IsSavedCheckpoint()
    {
        return savedScene == gameObject.scene.name && (savedPosition - SpawnPosition).sqrMagnitude < 0.01f;
    }

    void RespawnPlayerHere()
    {
        GameObject found = GameObject.FindGameObjectWithTag(playerTag);
        if (found == null)
        {
            Debug.LogWarning($"Checkpoint '{name}': couldn't find an object tagged '{playerTag}' to respawn.");
            return;
        }

        Rigidbody2D rb = found.GetComponentInParent<Rigidbody2D>();
        Transform player = rb != null ? rb.transform : found.transform;

        Vector3 spawn = SpawnPosition;
        spawn.z = player.position.z;
        player.position = spawn;

        if (rb != null)
        {
            rb.position = spawn;
            rb.linearVelocity = Vector2.zero;
        }

        if (savedDoubleJump)
        {
            PlayerDoubleJump doubleJump = player.GetComponent<PlayerDoubleJump>();
            if (doubleJump != null) doubleJump.Unlocked = true;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Use the object that owns the Rigidbody, in case the touching collider sits on a child
        GameObject target = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (!target.CompareTag(playerTag)) return;

        // Save this spot. Touching it again also updates whether the double jump is unlocked.
        savedScene = gameObject.scene.name;
        savedPosition = SpawnPosition;
        PlayerDoubleJump doubleJump = target.GetComponent<PlayerDoubleJump>();
        savedDoubleJump = doubleJump != null && doubleJump.Unlocked;

        if (reached) return;
        reached = true;

        ShowReachedLook();
        if (reachedSound != null) sfx.PlayOneShot(reachedSound, soundVolume);
        if (message != null) message.Show();
        Debug.Log($"Checkpoint reached: {name}");
    }

    void ShowReachedLook()
    {
        if (sr != null && reachedSprite != null) sr.sprite = reachedSprite;

        if (anim != null && anim.runtimeAnimatorController != null && !string.IsNullOrEmpty(reachedTrigger))
        {
            foreach (AnimatorControllerParameter p in anim.parameters)
            {
                if (p.name == reachedTrigger && p.type == AnimatorControllerParameterType.Trigger)
                {
                    anim.SetTrigger(reachedTrigger);
                    return;
                }
            }
            Debug.LogWarning($"Checkpoint '{name}': its Animator has no Trigger named '{reachedTrigger}'.");
        }
    }

    // Shows where the player will reappear
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + (Vector3)spawnOffset, 0.3f);
    }
}
