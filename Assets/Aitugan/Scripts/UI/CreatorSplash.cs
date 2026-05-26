using System.Collections;
using UnityEngine;
using Aitugan.Core;

namespace Aitugan.UI
{
    /// <summary>
    /// Brief studio splash shown before the title. Displays
    /// "Katsumi Game" on a black field, fades in, holds, and fades out.
    /// Player can tap / press a key to skip.
    /// </summary>
    public class CreatorSplash : MonoBehaviour
    {
        GUIStyle _logo, _sub;
        Texture2D _bg;
        float _alpha = 0f;
        bool _done = false;

        void OnGUI()
        {
            Ui.EnsureSkin();
            if (_logo == null)
            {
                var f = Ui.Font;
                _logo = new GUIStyle()
                {
                    font = f,
                    fontSize = Ui.Sized(56),
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.95f, 0.85f, 0.6f, _alpha) }
                };
                _sub = new GUIStyle()
                {
                    font = f,
                    fontSize = Ui.Sized(16),
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Italic,
                    normal = { textColor = new Color(0.7f, 0.62f, 0.45f, _alpha) }
                };
                _bg = ProcGfx.MakeRect(2, 2, ProcGfx.Hex("#000000"), ProcGfx.Hex("#000000")).texture;
            }

            // Solid black background
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _bg);

            // Animate the alpha of the logo + tagline
            _logo.normal.textColor = new Color(0.95f, 0.85f, 0.6f, _alpha);
            _sub.normal.textColor  = new Color(0.7f, 0.62f, 0.45f, _alpha * 0.9f);

            // Vertical layout: logo top, then a clear gap, then the "presents"
            // tagline. Spacing scales with font size so it stays correct on any
            // resolution (LegacyRuntime.ttf on WebGL has taller metrics than
            // Helvetica Neue, which is why the old fixed offsets overlapped).
            float logoSize = Ui.Sized(56);
            float subSize  = Ui.Sized(16);
            float logoY = Screen.height * 0.42f;
            float gap   = logoSize * 0.55f;                 // breathing room
            float subY  = logoY + logoSize + gap;

            GUI.Label(new Rect(0, logoY, Screen.width, logoSize * 1.4f), "KATSUMI GAME", _logo);
            GUI.Label(new Rect(0, subY,  Screen.width, subSize  * 1.6f), "presents", _sub);
        }

        public IEnumerator Run()
        {
            const float fadeIn = 0.8f;
            const float hold   = 1.4f;
            const float fadeOut = 0.8f;

            float t = 0f;
            // Fade in
            while (t < fadeIn && !_done)
            {
                t += Time.unscaledDeltaTime;
                _alpha = Mathf.Clamp01(t / fadeIn);
                if (InputBus.I != null && InputBus.I.AnyKeyPressed) _done = true;
                yield return null;
            }
            _alpha = 1f;

            // Hold
            t = 0f;
            while (t < hold && !_done)
            {
                t += Time.unscaledDeltaTime;
                if (InputBus.I != null && InputBus.I.AnyKeyPressed) _done = true;
                yield return null;
            }

            // Fade out
            t = 0f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                _alpha = 1f - Mathf.Clamp01(t / fadeOut);
                yield return null;
            }

            Destroy(this);
        }
    }
}
