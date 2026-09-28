using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Place at the end of a level. When the player touches this trigger zone,
/// the next level loads. Leave "Scene To Load" empty to go to the next scene
/// in the build list, or type a scene name to load a specific one
/// (for example a win screen after the final level).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelExit : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("Leave empty to load the next scene in the build list. Or type an exact scene name to load that one instead.")]
    [SerializeField] private string sceneToLoad = "";

    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Seconds to wait before loading (useful later for a fade-out or victory sound).")]
    [SerializeField] private float loadDelay = 0f;

    private bool triggered = false;

    // Runs when the component is first added: makes the collider a trigger automatically
    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // The player has more than one collider, so guard against loading twice
        if (triggered || !other.CompareTag(playerTag)) return;

        triggered = true;
        Invoke(nameof(LoadDestination), loadDelay);
    }

    void LoadDestination()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
            return;
        }

        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.Log("LevelExit: this is the last scene in the build list. Add a win/credits scene after it, or type a scene name in 'Scene To Load'.");
            triggered = false;
        }
    }

    // Shows the exit zone in the Scene view
    void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
    }
}