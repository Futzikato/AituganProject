using UnityEngine;
using Aitugan.UI;

namespace Aitugan.Core
{
    /// <summary>
    /// Entry point. Spawns all persistent singletons the first time any scene
    /// loads. The default SampleScene contains nothing - we provide everything
    /// at runtime so the project is playable on first import.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (GameState.I != null) return; // already initialized

            // ---- Runtime quality + power settings ----
            // Target a steady 60 fps; if the device can't hold it, the engine
            // will degrade gracefully to ~30 fps which still feels smooth.
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0; // pixel-art friendly, faster

            // Run physics at 60 Hz instead of Unity's default 50 Hz so player
            // velocity samples line up with the render frame - this kills the
            // micro-stutter that made Aitugan feel laggy.
            Time.fixedDeltaTime = 1f / 60f;
            Time.maximumDeltaTime = 1f / 15f;

            // Mobile / WebGL friendliness: cap pixel light count, skip the
            // soft particles pass, and never let the screen dim mid-play.
            QualitySettings.pixelLightCount = 0;
            QualitySettings.softParticles = false;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.shadows = ShadowQuality.Disable;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            var go = new GameObject("[Aitugan]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<GameState>();
            go.AddComponent<DialogueManager>();
            go.AddComponent<TouchControls>();
            go.AddComponent<InputBus>();
            go.AddComponent<AudioBus>();

            // SceneFlow drives the actual gameplay state machine.
            var flow = new GameObject("SceneFlow");
            Object.DontDestroyOnLoad(flow);
            flow.AddComponent<SceneFlow>();

            // The dialogue/HUD UI lives on its own DontDestroyOnLoad object
            // because it must persist across vignette transitions.
            var ui = new GameObject("UI");
            Object.DontDestroyOnLoad(ui);
            ui.AddComponent<DialogueBox>();
            ui.AddComponent<Hud>();

            // Ensure a camera and audio listener exist with sensible defaults.
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.backgroundColor = ProcGfx.Hex("#1A1208");
            cam.clearFlags = CameraClearFlags.SolidColor;
            Object.DontDestroyOnLoad(cam.gameObject);

            Debug.Log("[Aitugan] Bootstrap complete.");
        }
    }
}
