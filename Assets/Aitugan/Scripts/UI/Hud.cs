using UnityEngine;
using Aitugan.Core;

namespace Aitugan.UI
{
    /// <summary>Minimal heads-up display: HP pips, arrow count, tumar, vignette title.</summary>
    public class Hud : MonoBehaviour
    {
        Texture2D _pip, _pipEmpty, _arrow, _tumar, _tumarUsed;
        GUIStyle _label, _vignetteLabel;
        bool _initialized;
        public string vignetteTitle = "";

        public static Hud I { get; private set; }
        void Awake() { I = this; }

        void Init()
        {
            if (_initialized) return;
            _initialized = true;
            _pip      = ProcGfx.MakeRect(16, 16, ProcGfx.Hex("#C03030"), ProcGfx.Hex("#000000")).texture;
            _pipEmpty = ProcGfx.MakeRect(16, 16, ProcGfx.Hex("#1F1410"), ProcGfx.Hex("#3A2418")).texture;
            _arrow    = ProcGfx.MakeArrow(ProcGfx.Hex("#C0C0C0"), ProcGfx.Hex("#FFFFFF")).texture;
            _tumar    = ProcGfx.MakeCircle(8, ProcGfx.Hex("#D4C070"), ProcGfx.Hex("#7A5C20")).texture;
            _tumarUsed= ProcGfx.MakeCircle(8, ProcGfx.Hex("#3A2A18"), ProcGfx.Hex("#1A1108")).texture;

            var f = Ui.Font;
            _label = new GUIStyle() { font = f, fontSize = Ui.Sized(14), normal = { textColor = new Color(0.95f, 0.92f, 0.83f) } };
            _vignetteLabel = new GUIStyle() { font = f, fontSize = Ui.Sized(18), fontStyle = FontStyle.Italic, normal = { textColor = new Color(0.85f, 0.78f, 0.55f, 0.7f) } };
        }

        void OnGUI()
        {
            Ui.EnsureSkin();
            Init();
            var gs = GameState.I;
            if (gs == null) return;
            if (gs.currentVignette < 1 || gs.currentVignette > 5) return;
            float s = Ui.Scale;
            float pad = 20f * s;

            // Vignette title in upper-left
            if (!string.IsNullOrEmpty(vignetteTitle))
                GUI.Label(new Rect(pad, 16f * s, 600f * s, 30f * s), vignetteTitle, _vignetteLabel);

            // HP pips
            float pipY = 50f * s, pipSize = 18f * s, pipStep = 22f * s;
            for (int i = 0; i < gs.hpMax; i++)
                GUI.DrawTexture(new Rect(pad + i * pipStep, pipY, pipSize, pipSize), i < gs.hp ? _pip : _pipEmpty);

            // Arrow count
            GUI.DrawTexture(new Rect(pad, 76f * s, 24f * s, 8f * s), _arrow, ScaleMode.StretchToFill);
            GUI.Label(new Rect(pad + 28f * s, 70f * s, 80f * s, 22f * s), gs.arrows.ToString(), _label);
            if (gs.hasWindArrows)
            {
                GUI.Label(new Rect(pad + 60f * s, 70f * s, 100f * s, 22f * s), $"W:{gs.windArrows}", _label);
            }

            // Saumal
            GUI.Label(new Rect(pad, 92f * s, 220f * s, 22f * s), $"Saumal: {gs.saumalFlasks}  [Q]", _label);

            // Tumar
            if (gs.hasTumar)
            {
                GUI.DrawTexture(new Rect(pad, 116f * s, 18f * s, 18f * s), gs.tumarUsed ? _tumarUsed : _tumar);
                GUI.Label(new Rect(pad + 24f * s, 116f * s, 220f * s, 22f * s), gs.tumarUsed ? "tumar (spent)" : "tumar (mother)", _label);
            }

            // Throwing stones (V3)
            if (gs.throwingStones > 0)
                GUI.Label(new Rect(pad, 140f * s, 220f * s, 22f * s), $"Stones: {gs.throwingStones}  [R]", _label);
        }
    }
}
