using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main menu Start button: loads level 1 (scene 1 in the Scene List) when clicked,
/// or when A or Start is pressed on a controller.
/// Put it on the Start Button itself. It also keeps the button selected, so it
/// shows as highlighted and Enter on the keyboard presses it too.
/// </summary>
public class Menu : MonoBehaviour
{
    private Button button;
    private bool loading;

    void Awake()
    {
        button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(StartGame);
    }

    void Start()
    {
        SelectThisButton();
    }

    void Update()
    {
        // A or Start on a controller presses the button directly, whatever the menu's input settings are
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null && (gamepad.buttonSouth.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame))
        {
            if (button == null || button.interactable) StartGame();
            return;
        }

        // If clicking empty space cleared the selection, select the button again
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
        {
            SelectThisButton();
        }
    }

    void SelectThisButton()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void StartGame()
    {
        // Only load once, even if the click and the controller both fire in the same moment
        if (loading) return;
        loading = true;

        Debug.Log("Start button pressed - loading scene 1");
        SceneManager.LoadScene(1);
    }
}
