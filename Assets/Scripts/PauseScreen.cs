using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Press Esc or P (or Start on a controller) to pause the game and show the word
/// "PAUSE" on screen. Press it again to carry on.
/// While paused, everything freezes: the player can't move, jump, attack or dash,
/// enemies stop, and (optionally) all sound pauses.
/// Put it on a Canvas in each level and drag the PAUSE text (a child of that Canvas)
/// into the Pause Screen field. The text hides itself when the level starts,
/// so leave it ticked in the editor.
/// </summary>
public class PauseScreen : MonoBehaviour
{
    [Tooltip("What shows while paused, such as your PAUSE text. Must be a CHILD of this object, not this object itself.")]
    [SerializeField] private GameObject pauseScreen;

    [Tooltip("Pause controls. Defaults to Esc, P and the Start button on a controller (any of them works).")]
    [SerializeField] private InputAction pauseInput = CreateDefaultPauseInput();

    [Tooltip("Freeze all sound (music, footsteps, enemies) while paused. Untick to keep it playing.")]
    [SerializeField] private bool pauseSound = true;

    private readonly List<InputAction> blockedActions = new List<InputAction>();
    private float timeScaleBeforePause = 1f;
    private bool paused;

    static InputAction CreateDefaultPauseInput()
    {
        InputAction action = new InputAction("Pause", InputActionType.Button);
        AddDefaultBindings(action);
        return action;
    }

    static void AddDefaultBindings(InputAction action)
    {
        action.AddBinding("<Keyboard>/escape");
        action.AddBinding("<Keyboard>/p");
        action.AddBinding("<Gamepad>/start");
    }

    void Awake()
    {
        if (pauseScreen == null)
        {
            Debug.LogWarning($"PauseScreen on '{name}': drag your PAUSE text into the Pause Screen field.");
        }
        else if (transform.IsChildOf(pauseScreen.transform))
        {
            // Hiding the object this script is on would switch the script off too
            Debug.LogWarning($"PauseScreen on '{name}': the Pause Screen field points at this object (or one of its parents). Drag in the PAUSE text that sits inside this Canvas instead.");
            pauseScreen = null;
        }

        if (pauseScreen != null) pauseScreen.SetActive(false);
    }

    void OnEnable()
    {
        // Safety net: if the Inspector copy of the action has no bindings, restore the defaults
        if (pauseInput.bindings.Count == 0) AddDefaultBindings(pauseInput);
        pauseInput.Enable();
    }

    void OnDisable()
    {
        // Never leave the game frozen if this object goes away (for example when the level changes)
        if (paused) Resume();
        pauseInput.Disable();
    }

    void Update()
    {
        if (!pauseInput.WasPressedThisFrame()) return;

        if (paused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (paused) return;
        paused = true;

        // Freezes movement, physics, animations, timers and cooldowns
        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;

        if (pauseSound) AudioListener.pause = true;

        // Switch off every control (move, jump, attack, dash) except the pause key and menu buttons
        blockedActions.Clear();
        foreach (InputAction action in InputSystem.ListEnabledActions())
        {
            if (action == pauseInput) continue;
            if (action.actionMap != null && action.actionMap.name == "UI") continue;
            blockedActions.Add(action);
        }
        foreach (InputAction action in blockedActions) action.Disable();

        if (pauseScreen != null) pauseScreen.SetActive(true);
    }

    public void Resume()
    {
        if (!paused) return;
        paused = false;

        Time.timeScale = timeScaleBeforePause > 0f ? timeScaleBeforePause : 1f;
        AudioListener.pause = false;

        foreach (InputAction action in blockedActions)
        {
            if (action != null) action.Enable();
        }
        blockedActions.Clear();

        if (pauseScreen != null) pauseScreen.SetActive(false);
    }
}
