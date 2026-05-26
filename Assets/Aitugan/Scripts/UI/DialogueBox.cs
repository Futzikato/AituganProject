using UnityEngine;
using Aitugan.Core;

namespace Aitugan.UI
{
    /// <summary>
    /// IMGUI dialogue box with an oyu-ornek-styled border. Uses three palette
    /// variants depending on bubble type. Reads from DialogueManager.Current
    /// and asks it to advance when the player presses interact.
    /// </summary>
    public class DialogueBox : MonoBehaviour
    {
        Texture2D _borderT, _borderF, _borderO, _borderX;
        Texture2D _portrait;
        GUIStyle _bgT, _bgF, _bgO, _bgX, _label, _portraitStyle, _idStyle;
        bool _initialized = false;
        DialogueBubble _last;
        float _typeT;

        void EnsureInit()
        {
            if (_initialized) return;
            _initialized = true;
            _borderT = ProcGfx.MakeOyuBorder(64, ProcGfx.Hex("#1B140C"), ProcGfx.Hex("#C7A266"), ProcGfx.Hex("#8C6624"));
            _borderF = ProcGfx.MakeOyuBorder(64, ProcGfx.Hex("#0E1525"), ProcGfx.Hex("#7090C0"), ProcGfx.Hex("#3C5278"));
            _borderO = ProcGfx.MakeOyuBorder(64, ProcGfx.Hex("#241A0E"), ProcGfx.Hex("#A88848"), ProcGfx.Hex("#705228"));
            _borderX = ProcGfx.MakeOyuBorder(64, ProcGfx.Hex("#000000"), ProcGfx.Hex("#A07028"), ProcGfx.Hex("#583812"));
            _portrait = MakePortraitTex();

            var f = Ui.Font;
            _bgT = MakeBoxStyle(_borderT);
            _bgF = MakeBoxStyle(_borderF);
            _bgO = MakeBoxStyle(_borderO);
            _bgX = MakeBoxStyle(_borderX);

            _label = new GUIStyle(GUI.skin.label)
            {
                font = f,
                wordWrap = true,
                fontSize = Ui.Sized(16),
                alignment = TextAnchor.UpperLeft,
                richText = false,
                normal = { textColor = new Color(0.95f, 0.92f, 0.83f) }
            };
            _portraitStyle = new GUIStyle();
            _idStyle = new GUIStyle(GUI.skin.label) { font = f, fontSize = Ui.Sized(10), alignment = TextAnchor.UpperRight, normal = { textColor = new Color(0.7f, 0.6f, 0.4f, 0.8f) } };
        }

        GUIStyle MakeBoxStyle(Texture2D bg)
        {
            var s = new GUIStyle(GUI.skin.box);
            s.font = Ui.Font;
            s.normal.background = bg;
            s.border = new RectOffset(8, 8, 8, 8);
            s.padding = new RectOffset(20, 20, 16, 16);
            s.alignment = TextAnchor.UpperLeft;
            return s;
        }

        Texture2D MakePortraitTex()
        {
            // Tiny rendering of Aitugan facing forward as a portrait.
            var sprite = ProcGfx.MakeAitugan();
            return sprite.texture;
        }

        void Update()
        {
            // EnsureInit must run inside OnGUI (touches GUI.skin); skip here.
            var dm = DialogueManager.I;
            if (dm == null) return;
            if (dm.Current != null && dm.Current != _last)
            {
                _last = dm.Current;
                _typeT = 0f;
                AudioBus.I.Blip(dm.Current.type);
            }
            if (dm.Current == null) _last = null;
            if (dm.Current != null) _typeT += Time.unscaledDeltaTime * 50f;

            if (dm.IsShowing && InputBus.I != null && InputBus.I.InteractPressed)
                dm.RequestAdvance();
        }

        void OnGUI()
        {
            Ui.EnsureSkin();
            EnsureInit();
            var dm = DialogueManager.I;
            if (dm == null || dm.Current == null) return;
            var b = dm.Current;

            float w = Mathf.Min(Ui.Px(880f), Screen.width * 0.92f);
            float h = b.type == "X" ? Ui.Px(140f) : Ui.Px(200f);
            float x = (Screen.width - w) / 2f;
            float y = b.type == "X" ? (Screen.height - h) / 2f : Screen.height - h - 24f;
            var rect = new Rect(x, y, w, h);

            GUIStyle bg = b.type switch { "F" => _bgF, "O" => _bgO, "X" => _bgX, _ => _bgT };
            GUI.Box(rect, GUIContent.none, bg);

            // ID corner label
            GUI.Label(new Rect(rect.x + rect.width - 80, rect.y + 6, 70, 18), b.id, _idStyle);

            // Portrait (T type only; not for fragments/objects/scroll)
            float padX = 24f;
            float textX = rect.x + padX;
            if (b.type == "T")
            {
                float ps = 96f;
                GUI.DrawTexture(new Rect(rect.x + 16, rect.y + 16, ps, ps), _portrait, ScaleMode.ScaleToFit);
                textX = rect.x + 16 + ps + 16;
            }

            // Reveal text typewriter-style
            int reveal = Mathf.Clamp(Mathf.FloorToInt(_typeT), 0, b.text.Length);
            string shown = b.text.Substring(0, reveal);

            var labelStyle = _label;
            if (b.type == "F") labelStyle.fontStyle = FontStyle.Italic;
            else if (b.type == "X") { labelStyle.alignment = TextAnchor.MiddleCenter; labelStyle.fontSize = Ui.Sized(22); }
            else { labelStyle.fontStyle = FontStyle.Normal; labelStyle.alignment = TextAnchor.UpperLeft; labelStyle.fontSize = Ui.Sized(16); }

            GUI.Label(new Rect(textX, rect.y + 18, rect.x + rect.width - textX - 24, rect.height - 36), shown, labelStyle);

            // "advance" prompt
            if (reveal >= b.text.Length)
            {
                bool touch = UnityEngine.InputSystem.Touchscreen.current != null;
                var promptStyle = new GUIStyle(_label) { fontSize = Ui.Sized(11), alignment = TextAnchor.LowerRight, normal = { textColor = new Color(0.7f, 0.6f, 0.4f) } };
                GUI.Label(new Rect(rect.x + 8, rect.y + rect.height - 22, rect.width - 16, 18),
                    touch ? "[ tap to continue ]" : "[E / Space]", promptStyle);
            }
        }
    }
}
