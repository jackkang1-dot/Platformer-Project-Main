using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// "YOU DIED" screen. When the player dies (enemies, water, pus, pits), it waits a
/// moment so the death animation can play, fades in your YOU DIED art, holds it,
/// then restarts the level. Checkpoints still work, since it's the same level reloading.
/// The player can press Space, Enter, click, or A / Start on a controller to restart sooner.
/// Put it on a Canvas in each level and drag the YOU DIED art (a child of that Canvas)
/// into the Death Screen field. The art hides itself when the level starts,
/// so leave it ticked in the editor.
/// </summary>
public class DeathScreen : MonoBehaviour
{
    [Tooltip("The YOU DIED art: an Image (or a Panel holding the background and text). Must be a CHILD of this object.")]
    [SerializeField] private GameObject deathScreen;

    [Header("Timing")]
    [Tooltip("Seconds after dying before the screen appears, so the death animation can be seen.")]
    [SerializeField] private float delayBeforeShowing = 1f;
    [Tooltip("Seconds to fade the screen in.")]
    [SerializeField] private float fadeTime = 0.5f;
    [Tooltip("Seconds the screen stays up before the level restarts.")]
    [SerializeField] private float showTime = 2.5f;
    [Tooltip("The player can't skip until the screen has been up this long, so mashing jump while dying doesn't skip it instantly.")]
    [SerializeField] private float minimumShowTime = 1f;

    [Header("Sound (optional)")]
    [Tooltip("Plays when the screen appears. Leave empty for no sound.")]
    [SerializeField] private AudioClip sound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    private static DeathScreen current;

    private CanvasGroup group;
    private AudioSource sfx;
    private bool playing;

    /// <summary>
    /// Called by PlayerHealth when the player dies. Returns true if a YOU DIED screen
    /// is in this level and will restart it; false if there isn't one.
    /// </summary>
    public static bool Show()
    {
        if (current == null || current.deathScreen == null) return false;
        if (!current.playing) current.StartCoroutine(current.Play());
        return true;
    }

    void Awake()
    {
        if (deathScreen == null)
        {
            Debug.LogWarning($"DeathScreen on '{name}': drag your YOU DIED art into the Death Screen field.");
        }
        else if (transform.IsChildOf(deathScreen.transform))
        {
            // Hiding the object this script is on would switch the script off too
            Debug.LogWarning($"DeathScreen on '{name}': the Death Screen field points at this object (or one of its parents). Drag in the YOU DIED art that sits inside this Canvas instead.");
            deathScreen = null;
        }

        if (deathScreen != null)
        {
            // A Canvas Group lets the background and text fade together
            group = deathScreen.GetComponent<CanvasGroup>();
            if (group == null) group = deathScreen.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            deathScreen.SetActive(false);
        }

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f;
    }

    void OnEnable()
    {
        current = this;
    }

    void OnDisable()
    {
        if (current == this) current = null;
    }

    IEnumerator Play()
    {
        playing = true;

        // Uses real time, so it still works if the game happens to be paused
        yield return new WaitForSecondsRealtime(delayBeforeShowing);

        deathScreen.SetActive(true);
        if (sound != null) sfx.PlayOneShot(sound, soundVolume);

        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            group.alpha = t / fadeTime;
            yield return null;
        }
        group.alpha = 1f;

        for (float t = 0f; t < showTime; t += Time.unscaledDeltaTime)
        {
            if (t >= minimumShowTime && RestartPressed()) break;
            yield return null;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    static bool RestartPressed()
    {
        Keyboard k = Keyboard.current;
        Mouse m = Mouse.current;
        Gamepad g = Gamepad.current;

        return (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) ||
               (m != null && m.leftButton.wasPressedThisFrame) ||
               (g != null && (g.buttonSouth.wasPressedThisFrame || g.startButton.wasPressedThisFrame));
    }
}
