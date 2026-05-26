using System.Collections;
using UnityEngine;
using Aitugan.Vignettes;
using Aitugan.UI;

namespace Aitugan.Core
{
    /// <summary>
    /// The vignette state machine. Owns the "scene root" GameObject under
    /// which every vignette spawns its world. Transitioning to a new vignette
    /// destroys the previous root, builds a new one, and runs that vignette's
    /// coroutine to completion. Saves between transitions.
    /// </summary>
    public class SceneFlow : MonoBehaviour
    {
        public static SceneFlow I { get; private set; }
        public GameObject SceneRoot { get; private set; }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        IEnumerator Start()
        {
            yield return null; // let other singletons settle
            yield return RunCreatorSplash();
            yield return RunTitle();
            yield return RunHistoricalNote();
            yield return RunVignette<V1Yurt>(1);
            yield return RunVignette<V2Trench>(2);
            yield return RunVignette<V3Ridge>(3);
            yield return RunVignette<V4Ravine>(4);
            yield return RunVignette<V5Beacon>(5);
            yield return RunCredits();
        }

        IEnumerator RunCreatorSplash()
        {
            BuildRoot("CreatorSplash");
            var cs = SceneRoot.AddComponent<CreatorSplash>();
            yield return cs.Run();
        }

        IEnumerator RunTitle()
        {
            BuildRoot("Title");
            var ts = SceneRoot.AddComponent<TitleScreen>();
            yield return ts.Run();
        }

        IEnumerator RunHistoricalNote()
        {
            BuildRoot("HistoricalNote");
            var hn = SceneRoot.AddComponent<HistoricalNote>();
            yield return hn.Run();
        }

        IEnumerator RunCredits()
        {
            BuildRoot("Credits");
            var cs = SceneRoot.AddComponent<CreditsScreen>();
            yield return cs.Run();
            // loop back to title
            yield return RunTitle();
            // After title we just stop - main flow already finished. The user
            // can start a new run by reloading. (Hard scope cap is 30 minutes.)
        }

        IEnumerator RunVignette<T>(int index) where T : VignetteBase, new()
        {
            GameState.I.currentVignette = index;
            BuildRoot("V" + index);
            // Spawn vignette as a component on the root
            var v = SceneRoot.AddComponent(typeof(T)) as VignetteBase;
            yield return v.Run();
        }

        void BuildRoot(string label)
        {
            if (SceneRoot != null) Destroy(SceneRoot);
            SceneRoot = new GameObject("SceneRoot_" + label);
        }
    }
}
