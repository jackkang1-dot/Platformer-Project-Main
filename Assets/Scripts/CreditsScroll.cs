using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Scrolls a block of credits text up the screen like a movie credit reel.
/// Put it on the TextMeshPro text object (inside a Canvas) in the Credits scene.
/// It starts just below the screen, rolls up until the last line is gone,
/// then loads the main menu. Hold any key or the mouse button to speed it up;
/// press Esc to skip straight to the menu.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CreditsScroll : MonoBehaviour
{
    [Header("Scrolling")]
    [Tooltip("How fast the credits move up, in screen pixels per second (based on a 1080-tall screen).")]
    [SerializeField] private float scrollSpeed = 80f;
    [Tooltip("Hold any key or the mouse button to speed the credits up by this much.")]
    [SerializeField] private float fastForwardMultiplier = 4f;
    [Tooltip("Seconds to wait after the last line scrolls off the top before leaving.")]
    [SerializeField] private float endDelay = 2f;

    [Header("When finished")]
    [Tooltip("Scene to load when the credits end or the player presses Esc (exact name from the Scenes folder). Leave empty to quit the game instead.")]
    [SerializeField] private string sceneToLoad = "Main_Menu";

    private RectTransform rt;
    private float endY;
    private float endTimer = -1f;
    private bool leaving;

    void Start()
    {
        rt = (RectTransform)transform;
        RectTransform parent = rt.parent as RectTransform;
        float screenHeight = parent != null ? parent.rect.height : Screen.height;

        // Measure how tall all the credits text is at its current width
        float width = rt.rect.width;
        float textHeight = rt.rect.height;
        TMP_Text text = GetComponent<TMP_Text>();
        if (text != null)
        {
            textHeight = text.GetPreferredValues(text.text, width, 0f).y;
        }

        // Pin the credits to the bottom-centre of the screen, with their top edge just below it
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(width, textHeight);
        rt.anchoredPosition = Vector2.zero;

        // Finished once the bottom line has moved past the top of the screen
        endY = screenHeight + textHeight;
    }

    void Update()
    {
        if (leaving) return;

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Leave();
            return;
        }

        if (rt.anchoredPosition.y < endY)
        {
            bool fastForward = (keyboard != null && keyboard.anyKey.isPressed) ||
                               (mouse != null && mouse.leftButton.isPressed);

            float speed = scrollSpeed * (fastForward ? fastForwardMultiplier : 1f);
            rt.anchoredPosition += Vector2.up * speed * Time.deltaTime;
        }
        else
        {
            if (endTimer < 0f) endTimer = endDelay;

            endTimer -= Time.deltaTime;
            if (endTimer <= 0f) Leave();
        }
    }

    void Leave()
    {
        leaving = true;

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Application.Quit(); // only works in the built game, not in the editor
        }
    }
}
