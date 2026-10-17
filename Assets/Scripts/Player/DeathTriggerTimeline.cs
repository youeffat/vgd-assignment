using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class DeathTriggerTimeline : MonoBehaviour
{
    [Header("Timeline Settings")]
    [Tooltip("Drag your Death PlayableDirector (Timeline) here.")]
    public PlayableDirector deathTimeline;

    [Header("Player Reference")]
    [Tooltip("Drag your player object with the FPSGestureController script here.")]
    public FPSGestureController fpsController;

    [Header("Trigger Settings")]
    [Tooltip("The tag your player object uses.")]
    public string playerTag = "Player";

    private bool isTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        // Ensure it's only triggered once and only by the player
        if (isTriggered) return;

        if (other.CompareTag(playerTag) || other.GetComponent<FPSGestureController>() != null)
        {
            isTriggered = true;

            // 1. Disable player movement, look, and spellcasting controls
            if (fpsController != null)
            {
                fpsController.enabled = false;
            }
            
            // Unlock and show cursor during the death sequence (or lock it if preferred)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 2. Play the death Timeline cutscene if assigned
            if (deathTimeline != null)
            {
                deathTimeline.stopped += OnDeathCutsceneFinished;
                deathTimeline.Play();
                Debug.Log("Death trigger activated: Playing death timeline.");
            }
            else
            {
                Debug.LogWarning("DeathTimeline is not assigned! Resetting scene immediately.");
                ResetScene();
            }

            // Disable this trigger collider so it can't fire twice
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    // This runs automatically the exact moment the Death Timeline finishes playing
    void OnDeathCutsceneFinished(PlayableDirector director)
    {
        deathTimeline.stopped -= OnDeathCutsceneFinished;
        ResetScene();
    }

    void ResetScene()
    {
        Debug.Log("Reloading scene...");
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}