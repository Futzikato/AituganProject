using System.Collections;
using UnityEngine;
using Aitugan.Core;

namespace Aitugan.UI
{
    public class TitleScreen : MonoBehaviour
    {
        bool _go;
        float _t;
        GUIStyle _title, _sub, _hint;
        Texture2D _bg;

        void OnGUI()
        {
            Ui.EnsureSkin();
            if (_title == null)
            {
                var f = Ui.Font;
                _title = new GUIStyle() { font = f, fontSize = Ui.Sized(64), alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.92f, 0.82f, 0.55f) } };
                _sub = new GUIStyle() { font = f, fontSize = Ui.Sized(22), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic, normal = { textColor = new Color(0.8f, 0.7f, 0.5f) } };
                _hint = new GUIStyle() { font = f, fontSize = Ui.Sized(14), alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.6f, 0.55f, 0.4f) } };
                _bg = ProcGfx.MakeRect(2, 2, ProcGfx.Hex("#0E0904"), ProcGfx.Hex("#0E0904")).texture;
            }
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _bg);

            // Stacked layout with explicit gaps based on actual font sizes so
            // the lines never overlap regardless of the active font's metrics
            // (LegacyRuntime.ttf used on WebGL is taller than Helvetica Neue).
            float titleSize = Ui.Sized(64);
            float subSize   = Ui.Sized(22);
            float titleY = Screen.height * 0.28f;
            float sub1Y  = titleY + titleSize * 1.35f;       // gap below title
            float sub2Y  = sub1Y  + subSize   * 2.0f;        // gap between sublines

            GUI.Label(new Rect(0, titleY, Screen.width, titleSize * 1.4f), "AITUGAN", _title);
            GUI.Label(new Rect(0, sub1Y,  Screen.width, subSize   * 1.6f), "a steppe chronicle", _sub);
            GUI.Label(new Rect(0, sub2Y,  Screen.width, subSize   * 1.6f), "Episode 1: Orbulak", _sub);
            bool touch = UnityEngine.InputSystem.Touchscreen.current != null;
            if (Mathf.Sin(_t * 3f) > 0)
                GUI.Label(new Rect(0, Screen.height * 0.78f, Screen.width, 30),
                    touch ? "[ tap to begin ]" : "[ press any key ]", _hint);

            // Controls hint
            var ctrls = new GUIStyle(_hint) { fontSize = Ui.Sized(11), alignment = TextAnchor.LowerCenter };
            if (touch)
            {
                GUI.Label(new Rect(0, Screen.height - 60, Screen.width, 20), "left thumb  move    FIRE  fire bow    BLADE  kinzhal    DODGE  dodge    HEAL  saumal    STONE  throw stone    SWAP  arrow type", ctrls);
                GUI.Label(new Rect(0, Screen.height - 40, Screen.width, 20), "tap anywhere to advance dialogue", ctrls);
            }
            else
            {
                GUI.Label(new Rect(0, Screen.height - 60, Screen.width, 20), "WASD move    LMB / J  fire bow    K  kinzhal    Shift  dodge    Q  saumal    R  throw stone    Tab  swap arrow", ctrls);
                GUI.Label(new Rect(0, Screen.height - 40, Screen.width, 20), "E / Space / Enter  advance dialogue", ctrls);
            }
        }

        public IEnumerator Run()
        {
            // crossfade in dombra drone for atmosphere
            AudioBus.I.PlayDrone(110f);
            while (!_go)
            {
                _t += Time.unscaledDeltaTime;
                if (InputBus.I.AnyKeyPressed) _go = true;
                yield return null;
            }
            AudioBus.I.StopMusic();
            yield return new WaitForSecondsRealtime(0.4f);
            Destroy(this);
        }
    }
}
