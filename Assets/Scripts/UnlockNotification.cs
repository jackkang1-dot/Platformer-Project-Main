using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pops a message onto the screen for a few seconds, like "DASH UNLOCKED!",
/// then fades it away. Type the message straight into the TextMeshPro text.
/// Put this script on a Canvas and drag the message text (a child of that Canvas)
/// into the Notification field. The message hides itself when the level starts,
/// so leave it ticked in the editor.
/// It can show when the level starts, and/or when something calls Show()
/// (for example the Double Jump Pickup when it's collected).
/// </summary>
public class UnlockNotification : MonoBehaviour
{
    [Tooltip("The message to show: a TextMeshPro text (or a Panel with text inside). Must be a CHILD of this object, not this object itself.")]
    [SerializeField] private GameObject notification;

    [Header("When to show")]
    [Tooltip("Show the message automatically when the level starts. Untick if something else (like a pickup) shows it instead.")]
    [SerializeField] private bool showOnLevelStart = true;
    [Tooltip("Seconds after the level starts before the message appears.")]
    [SerializeField] private float startDelay = 1f;
    [Tooltip("Don't show it again every time the level restarts after the player dies. It still shows when the player first reaches the level.")]
    [SerializeField] private bool skipAfterRestart = true;

    [Header("Timing")]
    [Tooltip("Seconds the message stays fully visible.")]
    [SerializeField] private float showTime = 4f;
    [Tooltip("Seconds to fade in and out.")]
    [SerializeField] private float fadeTime = 0.5f;

    [Header("Sound (optional)")]
    [Tooltip("Plays when the message appears. Leave empty for no sound.")]
    [SerializeField] private AudioClip sound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    private CanvasGroup group;
    private Vector3 normalScale = Vector3.one;
    private AudioSource sfx;
    private Coroutine routine;

    // Remembers which scene came before this one, to tell a restart apart from arriving at a new level
    private static string previousScene;
    private static string currentScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void StartTrackingScenes()
    {
        previousScene = null;
        currentScene = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        previousScene = currentScene;
        currentScene = scene.name;
    }

    void Awake()
    {
        if (notification == null)
        {
            Debug.LogWarning($"UnlockNotification on '{name}': drag your message text into the Notification field.");
        }
        else if (transform.IsChildOf(notification.transform))
        {
            // Hiding the object this script is on would switch the script off too
            Debug.LogWarning($"UnlockNotification on '{name}': the Notification field points at this object (or one of its parents). Drag in the message text that sits inside this Canvas instead.");
            notification = null;
        }

        if (notification != null)
        {
            // A Canvas Group lets the whole message (text, and panel if there is one) fade together
            group = notification.GetComponent<CanvasGroup>();
            if (group == null) group = notification.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            normalScale = notification.transform.localScale;
            notification.SetActive(false);
        }

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f; // 2D: same volume wherever the camera is
    }

    void Start()
    {
        if (!showOnLevelStart) return;

        bool restarted = previousScene != null && previousScene == SceneManager.GetActiveScene().name;
        if (skipAfterRestart && restarted) return;

        Show(startDelay);
    }

    /// <summary>Shows the message right away.</summary>
    public void Show()
    {
        Show(0f);
    }

    /// <summary>Shows the message after a delay in seconds.</summary>
    public void Show(float delay)
    {
        if (notification == null) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Play(delay));
    }

    IEnumerator Play(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        notification.SetActive(true);
        if (sound != null) sfx.PlayOneShot(sound, soundVolume);

        // Fade in with a small "pop" from slightly bigger to normal size
        Transform t = notification.transform;
        for (float elapsed = 0f; elapsed < fadeTime; elapsed += Time.deltaTime)
        {
            float progress = elapsed / fadeTime;
            group.alpha = progress;
            t.localScale = normalScale * Mathf.Lerp(1.2f, 1f, progress);
            yield return null;
        }
        group.alpha = 1f;
        t.localScale = normalScale;

        // Uses game time, so the message waits while the game is paused
        yield return new WaitForSeconds(showTime);

        for (float elapsed = 0f; elapsed < fadeTime; elapsed += Time.deltaTime)
        {
            group.alpha = 1f - elapsed / fadeTime;
            yield return null;
        }

        notification.SetActive(false);
        routine = null;
    }
}
