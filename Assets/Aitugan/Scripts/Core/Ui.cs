using UnityEngine;

namespace Aitugan.Core
{
    /// <summary>
    /// IMGUI helper. Two concerns:
    /// 1. Provide a screen-height-driven Scale factor so font sizes look right
    ///    on both desktop and high-DPI iPhone screens.
    /// 2. Provide a high-quality dynamic OS font so IMGUI text is anti-aliased
    ///    (Unity's default GUI skin uses an unhinted bitmap font that looks
    ///    awful at any DPI - that's what was making the phone look bad).
    /// </summary>
    public static class Ui
    {
        public static float Scale => Mathf.Clamp(Screen.height / 720f, 1f, 2.5f);
        public static int Sized(int basePt) => Mathf.RoundToInt(basePt * Scale);
        public static float Px(float baseUnits) => baseUnits * Scale;

        static Font _font;
        public static Font Font
        {
            get
            {
                if (_font != null) return _font;

#if UNITY_WEBGL && !UNITY_EDITOR
                // WebGL: Font.CreateDynamicFontFromOSFont silently returns a
                // font with NO glyphs (the host browser doesn't expose system
                // fonts to the player), so every IMGUI label renders blank.
                // Use Unity's built-in runtime font instead - it ships inside
                // the build and always has Latin glyphs.
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                     ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
#else
                // Native platforms - prefer the host OS font for crisper text.
                _font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Helvetica Neue", "Helvetica", "Arial", "Verdana", "sans-serif" },
                    32);
                if (_font == null)
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
                return _font;
            }
        }

        static bool _skinPatched;
        /// <summary>Call at the top of any OnGUI() to ensure crisp anti-aliased text.</summary>
        public static void EnsureSkin()
        {
            if (_skinPatched && GUI.skin.font == _font) return;
            var f = Font;
            GUI.skin.font = f;
            GUI.skin.label.font = f;
            GUI.skin.box.font = f;
            GUI.skin.button.font = f;
            GUI.skin.textField.font = f;
            GUI.skin.textArea.font = f;
            _skinPatched = true;
        }
    }
}
