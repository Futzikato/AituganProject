using UnityEngine;
using UnityEngine.InputSystem;

namespace Aitugan.Core
{
    /// <summary>
    /// Unified input bus: merges keyboard/mouse/gamepad (desktop) with
    /// the on-screen TouchControls (mobile). Polled directly so no
    /// PlayerInput wiring is required.
    /// </summary>
    public class InputBus : MonoBehaviour
    {
        public static InputBus I { get; private set; }

        public Vector2 Move { get; private set; }
        public Vector2 AimWorld { get; private set; }
        public bool AttackHeld { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool MeleePressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool DodgePressed { get; private set; }
        public bool ItemPressed { get; private set; }
        public bool ThrowPressed { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool AnyKeyPressed { get; private set; }
        public bool SwapArrowPressed { get; private set; }

        /// <summary>True when input is coming from a touchscreen this frame.</summary>
        public bool IsTouchActive { get; private set; }

        Camera _cam;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            _cam = _cam != null ? _cam : Camera.main;
            var kb = Keyboard.current;
            var ms = Mouse.current;
            var gp = Gamepad.current;
            var tc = TouchControls.I;

            IsTouchActive = tc != null && tc.HasTouch
                && (tc.Move.sqrMagnitude > 0.01f || tc.FireHeld || tc.AnyTouchPressedThisFrame);

            // ---- Move ----
            Vector2 move = Vector2.zero;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1;
            }
            if (gp != null) move += gp.leftStick.ReadValue();
            move = Vector2.ClampMagnitude(move, 1f);
            if (tc != null && tc.Move.sqrMagnitude > 0.01f) move = tc.Move;
            Move = move;

            // ---- Aim (desktop mouse only; mobile uses auto-aim in AituganController) ----
            if (ms != null && _cam != null)
            {
                Vector3 mp = ms.position.ReadValue();
                mp.z = -_cam.transform.position.z;
                AimWorld = _cam.ScreenToWorldPoint(mp);
            }

            // ---- Attack / Fire ----
            bool tcFireHeld = tc != null && tc.FireHeld;
            bool tcFirePressed = tc != null && tc.FirePressedThisFrame;
            AttackHeld = tcFireHeld
                || (ms != null && ms.leftButton.isPressed)
                || (kb != null && kb.jKey.isPressed)
                || (gp != null && gp.rightTrigger.isPressed);
            AttackPressed = tcFirePressed
                || (ms != null && ms.leftButton.wasPressedThisFrame)
                || (kb != null && kb.jKey.wasPressedThisFrame)
                || (gp != null && gp.rightTrigger.wasPressedThisFrame);

            // ---- Melee ----
            MeleePressed = (tc != null && tc.KinzhalPressed)
                || (kb != null && kb.kKey.wasPressedThisFrame)
                || (gp != null && gp.buttonWest.wasPressedThisFrame);

            // ---- Interact / Advance dialogue ----
            InteractPressed = (tc != null && tc.AdvancePressed)
                || (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame))
                || (gp != null && gp.buttonSouth.wasPressedThisFrame);

            // ---- Dodge ----
            DodgePressed = (tc != null && tc.DodgePressed)
                || (kb != null && kb.leftShiftKey.wasPressedThisFrame)
                || (gp != null && gp.buttonEast.wasPressedThisFrame);

            // ---- Item / Saumal ----
            ItemPressed = (tc != null && tc.SaumalPressed)
                || (kb != null && kb.qKey.wasPressedThisFrame)
                || (gp != null && gp.buttonNorth.wasPressedThisFrame);

            // ---- Throw stone ----
            ThrowPressed = (tc != null && tc.ThrowPressed)
                || (kb != null && kb.rKey.wasPressedThisFrame)
                || (gp != null && gp.leftTrigger.wasPressedThisFrame);

            // ---- Swap arrow type ----
            SwapArrowPressed = (tc != null && tc.SwapPressed)
                || (kb != null && kb.tabKey.wasPressedThisFrame)
                || (gp != null && gp.rightShoulder.wasPressedThisFrame);

            SprintHeld = (kb != null && kb.leftCtrlKey.isPressed) || (gp != null && gp.leftStickButton.isPressed);

            AnyKeyPressed = (kb != null && kb.anyKey.wasPressedThisFrame)
                || (ms != null && ms.leftButton.wasPressedThisFrame)
                || (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.startButton.wasPressedThisFrame))
                || (tc != null && tc.AnyTouchPressedThisFrame);
        }
    }
}
