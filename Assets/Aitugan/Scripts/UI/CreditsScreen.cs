using System.Collections;
using UnityEngine;
using Aitugan.Core;

namespace Aitugan.UI
{
    public class CreditsScreen : MonoBehaviour
    {
        Texture2D _bg;
        GUIStyle _scroll, _title, _line;
        bool _initialized;

        void Init()
        {
            if (_initialized) return;
            _initialized = true;
            var f = Ui.Font;
            _bg = ProcGfx.MakeRect(2, 2, ProcGfx.Hex("#0A0703"), ProcGfx.Hex("#0A0703")).texture;
            _scroll = new GUIStyle() { font = f, fontSize = Ui.Sized(24), alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.9f, 0.78f, 0.5f) }, wordWrap = true };
            _title = new GUIStyle() { font = f, fontSize = Ui.Sized(36), alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.95f, 0.85f, 0.6f) } };
            _line = new GUIStyle() { font = f, fontSize = Ui.Sized(14), alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.7f, 0.65f, 0.5f) } };
        }

        void OnGUI()
        {
            Ui.EnsureSkin();
            Init();
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _bg);
        }

        public IEnumerator Run()
        {
            AudioBus.I.PlayDrone(98f);
            // Reset camera so the hook sprite is in view
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 0, -10);
                cam.orthographicSize = 5f;
                var follow = cam.GetComponent<Aitugan.Vignettes.CameraFollow>();
                if (follow != null) follow.enabled = false;
            }
            // Closing scroll
            yield return DialogueManager.I.ShowSequence("C-01", "C-02", "C-03", "C-04", "C-05");

            // Episode 2 hook image: Aitugan riding away (just sprite move)
            yield return ShowHookSequence();

            // Final title card
            yield return DialogueManager.I.Show("C-08");

            // Static credits frame
            float t = 0;
            while (t < 8f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            AudioBus.I.StopMusic();
        }

        IEnumerator ShowHookSequence()
        {
            // Spawn a small Aitugan sprite that drifts across the screen.
            var go = new GameObject("RidingAitugan");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProcGfx.MakeAitugan();
            sr.sortingOrder = 5;
            go.transform.localScale = new Vector3(2f, 2f, 1f);
            go.transform.position = new Vector3(-8f, -1f, 0);

            // bandage tint
            sr.color = new Color(0.85f, 0.85f, 0.85f);

            float t = 0;
            while (t < 6f)
            {
                t += Time.deltaTime;
                go.transform.position += new Vector3(Time.deltaTime * 0.7f, 0, 0);
                yield return null;
            }
            yield return DialogueManager.I.Show("C-06");
            yield return DialogueManager.I.Show("C-07");
            Destroy(go);
        }
    }
}
