using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Aitugan.Core
{
    [System.Serializable]
    public class DialogueBubble
    {
        public string id;
        public string type; // "T" thought, "F" fragment, "O" found object, "X" closing scroll
        public string text;
    }

    [System.Serializable]
    public class DialogueData
    {
        public DialogueBubble[] bubbles;
    }

    /// <summary>
    /// Loads dialogue.json from Resources, exposes by id, and serves the
    /// currently active bubble to the DialogueBox UI. Vignettes can `Play(id)`
    /// or `await Play(id)` (via coroutine) to show one bubble at a time.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager I { get; private set; }

        Dictionary<string, DialogueBubble> _byId;
        public DialogueBubble Current { get; private set; }
        public bool IsShowing => Current != null;

        bool _advanceRequested;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            LoadDialogue();
        }

        void LoadDialogue()
        {
            var ta = Resources.Load<TextAsset>("dialogue");
            if (ta == null)
            {
                Debug.LogError("[Aitugan] dialogue.json not found in Resources/");
                _byId = new Dictionary<string, DialogueBubble>();
                return;
            }
            var data = JsonUtility.FromJson<DialogueData>(ta.text);
            _byId = new Dictionary<string, DialogueBubble>();
            foreach (var b in data.bubbles) _byId[b.id] = b;
            Debug.Log($"[Aitugan] Loaded {_byId.Count} dialogue bubbles.");
        }

        public DialogueBubble Get(string id)
        {
            if (_byId == null || !_byId.TryGetValue(id, out var b))
            {
                Debug.LogWarning($"[Aitugan] Missing bubble id: {id}");
                return null;
            }
            return b;
        }

        public void RequestAdvance() => _advanceRequested = true;

        /// <summary>Show a bubble and yield until the player advances it.</summary>
        public IEnumerator Show(string id, float minTime = 0.35f)
        {
            var b = Get(id);
            if (b == null) yield break;
            Current = b;
            _advanceRequested = false;
            float t = 0f;
            while (t < minTime) { t += Time.unscaledDeltaTime; yield return null; }
            while (!_advanceRequested) yield return null;
            Current = null;
            // small gap so the next bubble doesn't open mid-input
            yield return new WaitForSecondsRealtime(0.06f);
        }

        public IEnumerator ShowSequence(params string[] ids)
        {
            foreach (var id in ids) yield return Show(id);
        }
    }
}
