using UnityEngine;
using UnityEngine.Playables; // Required for Timeline

public class CutsceneManager : MonoBehaviour
{
    [Header("Timeline Reference")]
    [Tooltip("Drag your Timeline Director (PlayableDirector component) here.")]
    public PlayableDirector introDirector;

    [Header("Player Controller Reference")]
    public FPSGestureController fpsController; // Your player movement script

    void Start()
    {
        if (introDirector != null)
        {
            // 1. Immediately disable player controls and lock/hide cursor on scene load
            if (fpsController != null)
            {
                fpsController.enabled = false;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // 2. Subscribe to the Timeline's stopped event so we know exactly when it finishes
            introDirector.stopped += OnIntroCutsceneFinished;

            // 3. Play the Timeline cutscene
            introDirector.Play();
            Debug.Log("Intro Timeline cutscene started.");
        }
        else
        {
            Debug.LogWarning("No PlayableDirector assigned to CutsceneManager! Enabling player controls immediately.");
            EnablePlayerControls();
        }
    }

    // This runs automatically the exact millisecond the Timeline finishes playing
    void OnIntroCutsceneFinished(PlayableDirector director)
    {
        // Unsubscribe to prevent memory leaks
        introDirector.stopped -= OnIntroCutsceneFinished;

        EnablePlayerControls();
        Debug.Log("Intro Timeline cutscene finished. Player controls restored.");
    }

    void EnablePlayerControls()
    {
        if (fpsController != null)
        {
            fpsController.enabled = true;
        }
        // Ensure cursor is locked back into FPS mode
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}