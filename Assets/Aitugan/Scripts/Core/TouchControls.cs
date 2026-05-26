using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Aitugan.Core
{
    /// <summary>
    /// On-screen mobile controls. Renders a virtual joystick on the lower-left
    /// and action buttons on the lower-right, then exposes resolved input state
    /// for InputBus to consume. Falls back to inert state on platforms without
    /// a Touchscreen (so desktop keyboard/mouse keeps working).
    ///
    /// Hit-testing is done in IMGUI (Y-down) coordinates - we flip touch
    /// positions once when reading from Touchscreen.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        public static TouchControls I { get; private set; }

        public bool HasTouch => Touchscreen.current != null;

        // Resolved inputs (one frame's worth)
        public Vector2 Move { get; private set; }
        public bool FireHeld { get; private set; }
        public bool FirePressedThisFrame { get; private set; }
        public bool KinzhalPressed { get; private set; }
        public bool DodgePressed { get; private set; }
        public bool SaumalPressed { get; private set; }
        public bool ThrowPressed { get; private set; }
        public bool SwapPressed { get; private set; }
        public bool AdvancePressed { get; private set; }
        public bool AnyTouchPressedThisFrame { get; private set; }

        // Whether to draw controls right now (gameplay vignettes only)
        public bool ShowControls = true;

        // ---- Joystick state ----
        int _joyTouchId = -1;
        Vector2 _joyOrigin;
        Vector2 _joyCurrent;
        bool _joyActive;

        // ---- Button regions (Y-down GUI space) ----
        struct Btn { public Rect rect; public int touchId; public bool pressed; public bool wasPressedThisFrame; public string label; }
        Btn _fire, _kinzhal, _dodge, _saumal, _throwBtn, _swap;

        Texture2D _solid;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            _solid = ProcGfx.MakeRect(2, 2, Color.white, Color.white).texture;

            // Hide on non-mobile builds. Unity's WebGL backend exposes a virtual
            // Touchscreen device in desktop browsers, so the existing
            // "Touchscreen.current == null" guard is not enough on web - the
            // joystick + action buttons would otherwise render on top of the
            // game in every desktop browser.
            if (!IsTouchPlatform())
            {
                ShowControls = false;
                enabled = false; // stops Update / OnGUI from firing at all
            }
        }

        static bool IsTouchPlatform()
        {
#if UNITY_IOS || UNITY_ANDROID
            return true;
#elif UNITY_WEBGL && !UNITY_EDITOR
            // Heuristic: only treat the player as touch-capable if the device
            // reports itself as a handheld. Desktop browsers report Desktop /
            // Unknown even when Touchscreen.current is non-null.
            return SystemInfo.deviceType == DeviceType.Handheld;
#else
            return false;
#endif
        }

        // -- Layout (re-computed every frame to handle rotation/resize) --
        void Layout()
        {
            float w = Screen.width, h = Screen.height;
            float unit = Mathf.Min(w, h);
            // Apple HIG min tap-target = ~44pt = ~88px @ 2x or ~132px @ 3x.
            // We use 11% of the short side which gives 129px on iPhone 14 landscape.
            float r = unit * 0.11f;

            // Right-side action cluster
            float rx = w - r * 1.4f;
            float ry = h - r * 1.4f;
            _fire    = new Btn { rect = R(rx, ry, r * 1.30f), label = "FIRE" };
            _kinzhal = new Btn { rect = R(rx - r * 2.4f, ry, r * 1.00f), label = "BLADE" };
            _dodge   = new Btn { rect = R(rx, ry - r * 2.4f, r * 1.00f), label = "DODGE" };
            _saumal  = new Btn { rect = R(rx - r * 2.4f, ry - r * 2.4f, r * 1.00f), label = "HEAL" };
            _throwBtn= new Btn { rect = R(rx - r * 4.6f, ry - r * 0.6f, r * 0.85f), label = "STONE" };
            _swap    = new Btn { rect = R(w - r * 1.1f, r * 1.1f, r * 0.85f), label = "SWAP" };
        }

        static Rect R(float cx, float cy, float radius)
            => new Rect(cx - radius, cy - radius, radius * 2f, radius * 2f);

        void Update()
        {
            // Reset per-frame edge events
            FirePressedThisFrame = false;
            KinzhalPressed = false;
            DodgePressed = false;
            SaumalPressed = false;
            ThrowPressed = false;
            SwapPressed = false;
            AdvancePressed = false;
            AnyTouchPressedThisFrame = false;

            Layout();

            if (Touchscreen.current == null)
            {
                Move = Vector2.zero;
                FireHeld = false;
                _joyActive = false;
                return;
            }

            // Track touches
            ResetButtonPressFlags();

            var touches = Touchscreen.current.touches;
            int touchCount = touches.Count;

            bool joyStillTracked = false;
            bool sawAnyPress = false;

            for (int i = 0; i < touchCount; i++)
            {
                var t = touches[i];
                if (!t.press.isPressed) continue;
                int id = t.touchId.ReadValue();
                Vector2 posGUI = ToGui(t.position.ReadValue());
                bool pressedThisFrame = t.press.wasPressedThisFrame;
                if (pressedThisFrame) sawAnyPress = true;

                // 1) Joystick - left half of screen
                if (id == _joyTouchId)
                {
                    _joyCurrent = posGUI;
                    joyStillTracked = true;
                    continue;
                }
                if (_joyTouchId < 0 && posGUI.x < Screen.width * 0.5f && !IsOverAnyButton(posGUI))
                {
                    if (pressedThisFrame)
                    {
                        _joyTouchId = id;
                        _joyOrigin = posGUI;
                        _joyCurrent = posGUI;
                        joyStillTracked = true;
                        _joyActive = true;
                        continue;
                    }
                }

                // 2) Action buttons - hit-test each
                if (TryButton(ref _fire, id, posGUI, pressedThisFrame)) continue;
                if (TryButton(ref _kinzhal, id, posGUI, pressedThisFrame)) continue;
                if (TryButton(ref _dodge, id, posGUI, pressedThisFrame)) continue;
                if (TryButton(ref _saumal, id, posGUI, pressedThisFrame)) continue;
                if (TryButton(ref _throwBtn, id, posGUI, pressedThisFrame)) continue;
                if (TryButton(ref _swap, id, posGUI, pressedThisFrame)) continue;

                // 3) Nothing else? Count as an advance / interact tap.
                if (pressedThisFrame) AdvancePressed = true;
            }

            // If the joystick touch ended, release it
            if (_joyTouchId >= 0 && !joyStillTracked)
            {
                _joyTouchId = -1;
                _joyActive = false;
            }

            // Resolve joystick into a Move vector (length 0..1)
            if (_joyActive)
            {
                Vector2 delta = _joyCurrent - _joyOrigin;
                float maxR = Mathf.Min(Screen.width, Screen.height) * 0.13f;
                // GUI Y-down so screen-down means delta.y > 0; gameplay wants up = +y, so invert.
                Vector2 vel = new Vector2(delta.x, -delta.y) / maxR;
                if (vel.sqrMagnitude > 1f) vel = vel.normalized;
                if (vel.magnitude < 0.18f) vel = Vector2.zero; // dead-zone
                Move = vel;
            }
            else
            {
                Move = Vector2.zero;
            }

            FireHeld = _fire.pressed;
            FirePressedThisFrame = _fire.wasPressedThisFrame;
            KinzhalPressed = _kinzhal.wasPressedThisFrame;
            DodgePressed = _dodge.wasPressedThisFrame;
            SaumalPressed = _saumal.wasPressedThisFrame;
            ThrowPressed = _throwBtn.wasPressedThisFrame;
            SwapPressed = _swap.wasPressedThisFrame;
            AnyTouchPressedThisFrame = sawAnyPress;
        }

        bool TryButton(ref Btn b, int id, Vector2 posGUI, bool pressedThisFrame)
        {
            if (!b.rect.Contains(posGUI)) return false;
            b.pressed = true;
            if (pressedThisFrame || (b.touchId < 0))
            {
                b.wasPressedThisFrame = pressedThisFrame;
                b.touchId = id;
            }
            return true;
        }

        bool IsOverAnyButton(Vector2 posGUI)
        {
            return _fire.rect.Contains(posGUI)
                || _kinzhal.rect.Contains(posGUI)
                || _dodge.rect.Contains(posGUI)
                || _saumal.rect.Contains(posGUI)
                || _throwBtn.rect.Contains(posGUI)
                || _swap.rect.Contains(posGUI);
        }

        void ResetButtonPressFlags()
        {
            _fire.pressed = false; _fire.wasPressedThisFrame = false;
            _kinzhal.pressed = false; _kinzhal.wasPressedThisFrame = false;
            _dodge.pressed = false; _dodge.wasPressedThisFrame = false;
            _saumal.pressed = false; _saumal.wasPressedThisFrame = false;
            _throwBtn.pressed = false; _throwBtn.wasPressedThisFrame = false;
            _swap.pressed = false; _swap.wasPressedThisFrame = false;
        }

        static Vector2 ToGui(Vector2 screenPos)
            => new Vector2(screenPos.x, Screen.height - screenPos.y);

        // ---- Rendering ----
        void OnGUI()
        {
            if (Touchscreen.current == null) return;
            if (!ShowControls) return;
            Ui.EnsureSkin();
            // Hide during dialogue to keep the screen clean
            if (DialogueManager.I != null && DialogueManager.I.IsShowing) return;
            // Hide on title / note / credits screens
            if (GameState.I == null) return;
            int v = GameState.I.currentVignette;
            if (v < 1 || v > 5) return;

            float unit = Mathf.Min(Screen.width, Screen.height);
            float jr = unit * 0.13f;

            // Joystick base
            Vector2 baseC = _joyActive ? _joyOrigin : new Vector2(jr * 1.5f, Screen.height - jr * 1.5f);
            DrawCircle(baseC, jr, new Color(1, 1, 1, 0.10f), new Color(1, 1, 1, 0.30f));
            Vector2 knob = _joyActive ? _joyCurrent : baseC;
            // clamp knob length to base radius
            Vector2 d = knob - baseC;
            if (d.magnitude > jr) knob = baseC + d.normalized * jr;
            DrawCircle(knob, jr * 0.42f, new Color(1, 1, 1, 0.45f), new Color(1, 1, 1, 0.7f));

            // Action buttons
            DrawButton(_fire,    _fire.pressed,    new Color(0.85f, 0.30f, 0.20f, 0.55f));
            DrawButton(_kinzhal, _kinzhal.pressed, new Color(0.75f, 0.65f, 0.30f, 0.50f));
            DrawButton(_dodge,   _dodge.pressed,   new Color(0.35f, 0.55f, 0.85f, 0.50f));
            DrawButton(_saumal,  _saumal.pressed,  new Color(0.45f, 0.85f, 0.45f, 0.50f));
            DrawButton(_throwBtn,_throwBtn.pressed,new Color(0.75f, 0.55f, 0.30f, 0.45f));
            DrawButton(_swap,    _swap.pressed,    new Color(0.55f, 0.55f, 0.85f, 0.45f));
        }

        void DrawCircle(Vector2 centerGUI, float radius, Color fill, Color edge)
        {
            // IMGUI doesn't render circles directly; approximate by drawing a
            // pre-baked circle texture once and reusing.
            if (_circle == null) BakeCircle();
            var c = GUI.color;
            GUI.color = fill;
            GUI.DrawTexture(new Rect(centerGUI.x - radius, centerGUI.y - radius, radius * 2, radius * 2), _circle);
            GUI.color = edge;
            GUI.DrawTexture(new Rect(centerGUI.x - radius, centerGUI.y - radius, radius * 2, radius * 2), _circleEdge);
            GUI.color = c;
        }

        Texture2D _circle, _circleEdge;
        void BakeCircle()
        {
            _circle = ProcGfx.MakeCircle(40, Color.white, new Color(1, 1, 1, 0)).texture;
            _circleEdge = ProcGfx.MakeCircle(40, new Color(0, 0, 0, 0), Color.white).texture;
        }

        GUIStyle _btnLabel;
        void DrawButton(Btn b, bool down, Color tint)
        {
            if (_btnLabel == null)
                _btnLabel = new GUIStyle() { font = Ui.Font, fontSize = Ui.Sized(15), alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1, 1, 1, 0.95f) }, fontStyle = FontStyle.Bold };
            // body
            float r = b.rect.width * 0.5f;
            Vector2 c = new Vector2(b.rect.x + r, b.rect.y + r);
            var col = down ? new Color(tint.r * 1.2f, tint.g * 1.2f, tint.b * 1.2f, Mathf.Min(1f, tint.a + 0.25f)) : tint;
            DrawCircle(c, r, col, new Color(1, 1, 1, 0.65f));
            GUI.Label(b.rect, b.label, _btnLabel);
        }
    }
}
