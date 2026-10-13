using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads a scene a few seconds after this object switches on.
/// Use it to go to the credits after the boss dies: put it on an empty object,
/// UNTICK that object so it starts switched off, and drag the object into the
/// boss's "Activate On Death" list.
/// </summary>
public class LoadSceneAfterDelay : MonoBehaviour
{
    [Tooltip("Exact name of the scene to load (as it appears in the Scenes folder).")]
    [SerializeField] private string sceneToLoad = "Credits";
    [Tooltip("Seconds to wait after this object switches on before loading.")]
    [SerializeField] private float delay = 3f;

    void OnEnable()
    {
        StartCoroutine(LoadAfterDelay());
    }

    IEnumerator LoadAfterDelay()
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneToLoad);
    }
}
