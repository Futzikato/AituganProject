using System.Collections;
using UnityEngine;
using Aitugan.Core;

namespace Aitugan.UI
{
    /// <summary>
    /// Shown once between the title screen and Vignette 1. Tells the player
    /// up-front that the battle is real but the people they'll meet aren't.
    /// </summary>
    public class HistoricalNote : MonoBehaviour
    {
        bool _go;
        float _t;
        Texture2D _bg;
        GUIStyle _heading, _body, _hint;

        readonly string[] _paragraphs =
        {
            "The Battle of Orbulak (1643) is a real historical event.",
            "In the late summer of 1643, roughly six hundred Kazakhs under Jangir Khan held a narrow mountain pass against a Dzungar army many times their size, until reinforcements from Samarkand turned the field.",
            "Aitugan, her family, her companions, and every named figure you will meet through her eyes are fictional. The pass, the dates, the wider battle, and its outcome are not.",
            "This is a small story inside a large one.",
        };

        void OnGUI()
        {
            Ui.EnsureSkin();
            if (_heading == null)
            {
                _bg = ProcGfx.MakeRect(2, 2, ProcGfx.Hex("#0A0703"), ProcGfx.Hex("#0A0703")).texture;
                var f = Ui.Font;
                _heading = new GUIStyle()
                {
                    font = f,
                    fontSize = Ui.Sized(28),
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Italic,
                    normal = { textColor = new Color(0.92f, 0.82f, 0.55f) }
                };
                _body = new GUIStyle()
                {
                    font = f,
                    fontSize = Ui.Sized(18),
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    normal = { textColor = new Color(0.85f, 0.78f, 0.6f) }
                };
                _hint = new GUIStyle()
                {
                    font = f,
                    fontSize = Ui.Sized(13),
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.6f, 0.55f, 0.4f) }
                };
            }

            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _bg);

            float w = Mathf.Min(820f, Screen.width * 0.86f);
            float x = (Screen.width - w) / 2f;
            float y = Screen.height * 0.14f;

            // Heading + a generous gap below it.
            float headingH = _heading.fontSize * 1.6f;
            GUI.Label(new Rect(x, y, w, headingH), "a note before we begin", _heading);
            y += headingH + _body.fontSize * 1.2f;

            // Fade in paragraphs one at a time. Stack them using the actual
            // wrapped height of each paragraph plus a fixed gap, so the long
            // historical paragraph never overlaps the next line - especially
            // important with LegacyRuntime.ttf on WebGL which has taller
            // metrics than the OS fonts used on native builds.
            float gap = _body.fontSize * 0.9f;
            int reveal = Mathf.Clamp(Mathf.FloorToInt(_t * 0.55f), 1, _paragraphs.Length);
            for (int i = 0; i < reveal; i++)
            {
                float pAlpha = Mathf.Clamp01(_t * 0.55f - i);
                var c = _body.normal.textColor;
                _body.normal.textColor = new Color(c.r, c.g, c.b, pAlpha);

                var content = new GUIContent(_paragraphs[i]);
                float h = _body.CalcHeight(content, w);
                GUI.Label(new Rect(x, y, w, h), content, _body);

                _body.normal.textColor = c;
                y += h + gap;
            }

            if (reveal >= _paragraphs.Length && Mathf.Sin(_t * 3f) > 0)
            {
                bool touch = UnityEngine.InputSystem.Touchscreen.current != null;
                GUI.Label(new Rect(0, Screen.height - 60, Screen.width, 24),
                    touch ? "[ tap to begin ]" : "[ press any key to begin ]", _hint);
            }
        }

        public IEnumerator Run()
        {
            _t = 0f;
            // Unskippable until every paragraph has fully faded in.
            // OnGUI reveals at rate 0.55, so the last paragraph is fully
            // opaque at _t = paragraphCount / 0.55.
            float minRevealTime = _paragraphs.Length / 0.55f;
            while (!_go)
            {
                _t += Time.unscaledDeltaTime;
                if (_t >= minRevealTime && InputBus.I.AnyKeyPressed) _go = true;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.3f);
            Destroy(this);
        }
    }
}
