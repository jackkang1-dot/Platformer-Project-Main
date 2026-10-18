using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Plays a cutscene made of still drawings with dialogue, one entry at a time.
/// Text types itself out letter by letter. The player clicks the Continue button,
/// or presses Space, Enter or A (controller), to finish the line or go to the next one.
/// Esc or Start (controller) skips the whole cutscene.
/// Fades through black whenever the drawing changes.
/// When it ends, it loads the next scene in the Scene List (or a scene you name).
/// Put it on the Canvas in a cutscene scene.
/// </summary>
public class Cutscene : MonoBehaviour
{
    [System.Serializable]
    public class Line
    {
        [Tooltip("Drawing to show. Leave empty to keep showing the previous drawing.")]
        public Sprite picture;
        [Tooltip("Who's talking. Leave empty for narration.")]
        public string speaker;
        [Tooltip("What they say. Leave empty if the dialogue is drawn into the picture, or to show just the drawing.")]
        [TextArea(2, 5)]
        public string text;
    }

    [Header("Story (one entry per drawing or line of dialogue, in order)")]
    [SerializeField] private List<Line> lines = new List<Line>();

    [Header("On-screen parts (drag in from the Canvas)")]
    [Tooltip("UI Image that shows the drawings. Tick Preserve Aspect on it so drawings aren't stretched.")]
    [SerializeField] private Image picture;
    [Tooltip("UI Button the player clicks to continue. Keep it directly under the Canvas, not inside the Dialogue Box, so it also shows on drawings without typed text.")]
    [SerializeField] private Button continueButton;
    [Tooltip("TextMeshPro text that shows the dialogue. Not needed if all the dialogue is drawn into the pictures.")]
    [SerializeField] private TMP_Text dialogueText;
    [Tooltip("Optional: TextMeshPro text for the speaker's name.")]
    [SerializeField] private TMP_Text speakerText;
    [Tooltip("Optional: the panel behind the dialogue. Hidden on entries with no text, so the drawing shows on its own.")]
    [SerializeField] private GameObject dialogueBox;
    [Tooltip("Optional: a small 'press to continue' hint, shown once a line has finished typing.")]
    [SerializeField] private GameObject continueHint;

    [Header("Typing")]
    [SerializeField] private float lettersPerSecond = 40f;
    [Tooltip("Optional: a short blip that plays as letters appear.")]
    [SerializeField] private AudioClip typeSound;
    [Range(0f, 1f)]
    [SerializeField] private float typeSoundVolume = 0.5f;

    [Header("Fades")]
    [Tooltip("Seconds to fade to and from black between drawings, and at the start and end.")]
    [SerializeField] private float fadeTime = 0.5f;

    [Header("When finished")]
    [Tooltip("Exact name of the scene to load after the last entry. Leave empty to load the next scene in the Scene List.")]
    [SerializeField] private string sceneToLoad = "";

    private Image fade;
    private AudioSource sfx;
    private bool leaving;
    private bool continueClicked;

    void Awake()
    {
        bool anyText = lines.Exists(l => !string.IsNullOrEmpty(l.text));
        if (anyText && dialogueText == null) Debug.LogWarning("Cutscene: some entries have text, but no Dialogue Text is assigned to show it.");

        if (GetComponentInParent<Canvas>() == null) Debug.LogWarning("Cutscene: put this script on the Canvas so it can fade the screen.");
        else CreateFade();

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
            // Keyboard and controller are handled by this script, so the button shouldn't be picked by them too
            continueButton.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f;

        // Start with an empty screen until the first entry appears
        if (picture != null) picture.enabled = false;
        if (dialogueBox != null) dialogueBox.SetActive(false);
        if (continueHint != null) continueHint.SetActive(false);
        if (speakerText != null) speakerText.text = "";
        if (dialogueText != null) dialogueText.text = "";
    }

    // A black full-screen image on top of everything, used for fading
    void CreateFade()
    {
        GameObject go = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        go.transform.SetAsLastSibling();

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fade = go.GetComponent<Image>();
        fade.color = Color.black;
        fade.raycastTarget = false;
    }

    void OnContinueClicked()
    {
        continueClicked = true;

        // Unselect the button so Space/Enter don't also "click" it, which would skip two lines at once
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    void Start()
    {
        StartCoroutine(PlayAll());
    }

    void Update()
    {
        if (!leaving && SkipPressed())
        {
            StopAllCoroutines();
            StartCoroutine(Finish());
        }
    }

    IEnumerator PlayAll()
    {
        if (lines.Count == 0) Debug.LogWarning("Cutscene: the Lines list is empty, so there's nothing to show.");

        for (int i = 0; i < lines.Count; i++)
        {
            Line line = lines[i];
            // A drawing left in the Image for previewing in the editor still counts as new the first time
            bool newPicture = picture != null && line.picture != null && (!picture.enabled || line.picture != picture.sprite);

            if (newPicture)
            {
                // Fade to black, swap the drawing, fade back in
                if (dialogueBox != null) dialogueBox.SetActive(false);
                yield return Fade(1f);
                picture.sprite = line.picture;
                picture.enabled = true;
                yield return Fade(0f);
            }
            else if (i == 0)
            {
                yield return Fade(0f);
            }

            yield return TypeLine(line);

            if (continueHint != null) continueHint.SetActive(true);
            yield return WaitForAdvance();
            if (continueHint != null) continueHint.SetActive(false);
        }

        yield return Finish();
    }

    IEnumerator TypeLine(Line line)
    {
        bool hasText = !string.IsNullOrEmpty(line.text) && dialogueText != null;
        if (dialogueBox != null) dialogueBox.SetActive(hasText);

        if (speakerText != null)
        {
            speakerText.text = line.speaker;
            speakerText.gameObject.SetActive(hasText && !string.IsNullOrEmpty(line.speaker));
        }

        if (!hasText)
        {
            if (dialogueText != null) dialogueText.text = "";
            yield break;
        }

        dialogueText.text = line.text;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();
        int total = dialogueText.textInfo.characterCount;

        // Wait a frame so the press that opened this line doesn't also skip its typing,
        // and forget any click made during the fade
        yield return null;
        continueClicked = false;

        float typed = 0f;
        int shown = 0;
        while (shown < total)
        {
            // Pressing during typing shows the whole line at once
            if (AdvancePressed()) break;

            typed += lettersPerSecond * Time.unscaledDeltaTime;
            int now = Mathf.Min(total, Mathf.FloorToInt(typed));
            if (now != shown)
            {
                dialogueText.maxVisibleCharacters = now;
                PlayTypeSound(now - 1);
                shown = now;
            }
            yield return null;
        }

        dialogueText.maxVisibleCharacters = total;
    }

    void PlayTypeSound(int charIndex)
    {
        if (typeSound == null || charIndex % 2 != 0) return;

        char c = dialogueText.textInfo.characterInfo[charIndex].character;
        if (!char.IsWhiteSpace(c)) sfx.PlayOneShot(typeSound, typeSoundVolume);
    }

    IEnumerator WaitForAdvance()
    {
        // Wait a frame so the press that finished the typing doesn't also skip to the next entry
        yield return null;
        continueClicked = false;
        while (!AdvancePressed()) yield return null;
    }

    IEnumerator Finish()
    {
        leaving = true;
        if (continueHint != null) continueHint.SetActive(false);
        if (continueButton != null) continueButton.interactable = false;
        yield return Fade(1f);

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
            yield break;
        }

        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(next);
        }
        else
        {
            Debug.LogWarning("Cutscene: there's no scene after this one in the Scene List. Add one, or type a name into Scene To Load.");
        }
    }

    // Fades the black overlay to the target alpha (0 = clear, 1 = fully black)
    IEnumerator Fade(float target)
    {
        if (fade == null) yield break;

        float start = fade.color.a;
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            SetFadeAlpha(Mathf.Lerp(start, target, t / fadeTime));
            yield return null;
        }
        SetFadeAlpha(target);
    }

    void SetFadeAlpha(float a)
    {
        Color c = fade.color;
        c.a = a;
        fade.color = c;
    }

    // True once per press of Continue, Space, Enter or A (or a left click anywhere if there's no Continue button)
    bool AdvancePressed()
    {
        Keyboard k = Keyboard.current;
        Mouse m = Mouse.current;
        Gamepad g = Gamepad.current;

        bool pressed = continueClicked ||
                       (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)) ||
                       (g != null && g.buttonSouth.wasPressedThisFrame) ||
                       (continueButton == null && m != null && m.leftButton.wasPressedThisFrame);

        continueClicked = false;
        return pressed;
    }

    static bool SkipPressed()
    {
        Keyboard k = Keyboard.current;
        Gamepad g = Gamepad.current;

        return (k != null && k.escapeKey.wasPressedThisFrame) ||
               (g != null && g.startButton.wasPressedThisFrame);
    }
}
