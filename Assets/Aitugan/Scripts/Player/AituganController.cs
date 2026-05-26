using UnityEngine;
using Aitugan.Core;

namespace Aitugan.Player
{
    /// <summary>
    /// The only character the player controls. Handles top-down 4-direction
    /// movement, bow firing, kinzhal melee, dodge, and item use. Health and
    /// inventory live on GameState; this script reads/writes them.
    /// </summary>
    public class AituganController : MonoBehaviour
    {
        public static AituganController I { get; private set; }

        // Tuned down from 5.5 / 7.5 for smoother feel - the original numbers
        // made Aitugan zip across the screen and broke the framing of each
        // vignette. These values keep her readable at 30-60 fps.
        public float moveSpeed = 3.6f;
        public float sprintSpeed = 5.0f;

        public bool canMove = true;
        public bool canShoot = true;
        public bool canMelee = true;

        // Vignette-specific constraints
        public bool lockedToTrenchY = false;
        public float trenchMinX = -1.5f, trenchMaxX = 1.5f;
        public float trenchY = -3f;

        public bool stealthMode = false;
        public bool wounded = false;

        public Sprite arrowSpriteStandard;
        public Sprite arrowSpriteFire;
        public Sprite arrowSpriteWind;

        public ArrowKind selectedArrow = ArrowKind.Standard;

        SpriteRenderer _sr;
        Rigidbody2D _rb;
        BoxCollider2D _col;
        float _meleeCooldown = 0f;
        float _shootCooldown = 0f;
        float _hurtFlash = 0f;
        float _dodgeTimer = 0f;
        Vector2 _dodgeVel;

        public Vector2 AimDir { get; private set; } = Vector2.right;
        // Last cardinal facing for sprite swap (-1,0 = left, +1,0 = right, 0,+1 = up, 0,-1 = down)
        Vector2Int _facing = new Vector2Int(0, -1);

        void Awake()
        {
            I = this;
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = Art.AituganFront != null ? Art.AituganFront : ProcGfx.MakeAitugan();
            _sr.sortingOrder = 30;
            // Authored PNGs are 128x128 with PPU 400 (~0.32 units tall). Scale
            // them up so Aitugan reads at ~1 world-unit tall, matching the old
            // proc sprite. The bigger source resolution gives crisper sampling
            // than the original 32x32 PNGs.
            if (Art.AituganFront != null) transform.localScale = new Vector3(3f, 3f, 1f);

            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            _col = gameObject.AddComponent<BoxCollider2D>();
            _col.size = new Vector2(0.55f, 0.7f);
            _col.offset = new Vector2(0, 0.35f);

            // Use authored Arrow_.png when available; tint for fire / wind variants.
            arrowSpriteStandard = Art.Arrow != null ? Art.Arrow : ProcGfx.MakeArrow(ProcGfx.Hex("#8C5C2E"), ProcGfx.Hex("#C0C0C0"));
            arrowSpriteFire     = Art.Arrow != null ? Art.Arrow : ProcGfx.MakeArrow(ProcGfx.Hex("#8C5C2E"), ProcGfx.Hex("#FF8000"));
            arrowSpriteWind     = Art.Arrow != null ? Art.Arrow : ProcGfx.MakeArrow(ProcGfx.Hex("#8C5C2E"), ProcGfx.Hex("#9CD8FF"));
        }

        void OnDestroy()
        {
            if (I == this) I = null;
        }

        void Update()
        {
            if (DialogueManager.I != null && DialogueManager.I.IsShowing)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }
            if (!canMove) { _rb.linearVelocity = Vector2.zero; }

            var ib = InputBus.I;
            // Auto-sprint at full joystick tilt on mobile (no Ctrl key available).
            bool autoSprint = ib.IsTouchActive && ib.Move.sqrMagnitude > 0.85f * 0.85f;
            float speed = ((ib.SprintHeld || autoSprint) && !wounded) ? sprintSpeed : moveSpeed;
            if (wounded) speed *= 0.75f;

            if (_dodgeTimer > 0)
            {
                _dodgeTimer -= Time.deltaTime;
                _rb.linearVelocity = _dodgeVel;
            }
            else if (canMove)
            {
                // Smoothly accelerate / decelerate toward the target velocity
                // rather than snapping to it - this removes the jittery,
                // "stop on a dime" feel and helps the game read more cinematic.
                Vector2 target = ib.Move * speed;
                Vector2 current = _rb.linearVelocity;
                float accel = (target.sqrMagnitude > 0.01f) ? 38f : 28f;
                Vector2 v = Vector2.MoveTowards(current, target, accel * Time.deltaTime);
                _rb.linearVelocity = v;
                if (lockedToTrenchY)
                {
                    var p = transform.position;
                    if (p.y != trenchY) transform.position = new Vector3(Mathf.Clamp(p.x, trenchMinX, trenchMaxX), trenchY, p.z);
                    if (p.x < trenchMinX) transform.position = new Vector3(trenchMinX, trenchY, p.z);
                    if (p.x > trenchMaxX) transform.position = new Vector3(trenchMaxX, trenchY, p.z);
                }
            }

            // ---- aim resolution ----
            // On touch, auto-aim at nearest enemy (no mouse cursor on mobile).
            // On desktop, point at mouse world position.
            AimDir = ResolveAimDir(ib);

            // ---- sprite facing ----
            UpdateFacing(ib);

            _shootCooldown -= Time.deltaTime;
            _meleeCooldown -= Time.deltaTime;
            _hurtFlash -= Time.deltaTime;
            _sr.color = _hurtFlash > 0 ? Color.red : Color.white;

            if (canShoot && ib.AttackPressed && _shootCooldown <= 0 && GameState.I.arrows > 0)
            {
                FireArrow();
                _shootCooldown = 0.35f;
            }

            if (canMelee && ib.MeleePressed && _meleeCooldown <= 0)
            {
                Melee();
                _meleeCooldown = 0.45f;
            }

            if (ib.DodgePressed && _dodgeTimer <= 0)
            {
                _dodgeTimer = 0.18f;
                _dodgeVel = ib.Move.sqrMagnitude > 0.01f ? ib.Move.normalized * 7f : AimDir * 6f;
            }

            if (ib.ItemPressed)
            {
                UseSaumal();
            }

            if (ib.ThrowPressed && GameState.I.throwingStones > 0)
            {
                ThrowStone();
                GameState.I.throwingStones--;
            }

            // Toggle arrow type via InputBus (Tab on desktop, SWAP button on mobile)
            if (ib.SwapArrowPressed && GameState.I.hasWindArrows)
            {
                selectedArrow = (selectedArrow == ArrowKind.Standard) ? ArrowKind.Wind : ArrowKind.Standard;
            }
        }

        void UpdateFacing(InputBus ib)
        {
            // Prefer movement direction; fall back to aim direction when standing still.
            Vector2 dir = ib.Move.sqrMagnitude > 0.02f ? ib.Move : AimDir;
            if (dir.sqrMagnitude < 0.01f) return;
            // Pick the dominant axis so we don't flicker between two sprites at diagonals
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                _facing = new Vector2Int(dir.x >= 0 ? 1 : -1, 0);
            else
                _facing = new Vector2Int(0, dir.y >= 0 ? 1 : -1);

            Sprite next = _facing switch
            {
                { x:  1, y: 0 } => Art.AituganRight,
                { x: -1, y: 0 } => Art.AituganLeft,
                { x: 0, y:  1 } => Art.AituganBack,
                { x: 0, y: -1 } => Art.AituganFront,
                _ => Art.AituganFront,
            };
            if (next != null && _sr.sprite != next) _sr.sprite = next;
        }

        Vector2 _lastNonZeroMove = Vector2.right;
        Vector2 ResolveAimDir(InputBus ib)
        {
            if (ib.Move.sqrMagnitude > 0.01f) _lastNonZeroMove = ib.Move.normalized;

            // Desktop mouse aim - has priority unless touch is being used
            if (!ib.IsTouchActive)
            {
                Vector2 toMouse = (Vector2)((Vector3)ib.AimWorld - transform.position);
                if (toMouse.sqrMagnitude > 0.04f) return toMouse.normalized;
            }

            // Auto-aim: nearest enemy within 9 units
            float bestSq = 81f;
            Vector2 best = _lastNonZeroMove;
            bool found = false;
            var enemies = Aitugan.Enemies.EnemyBase.AllAlive;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i]; if (e == null) continue;
                Vector2 to = (Vector2)(e.transform.position + new Vector3(0, 0.4f, 0) - transform.position);
                float sq = to.sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = to.normalized; found = true; }
            }
            if (found) return best;
            return _lastNonZeroMove;
        }

        void FireArrow()
        {
            // Drain a stone arrow from inventory (or a wind arrow if selected).
            if (selectedArrow == ArrowKind.Wind)
            {
                if (GameState.I.windArrows <= 0) { selectedArrow = ArrowKind.Standard; return; }
                GameState.I.windArrows--;
            }
            else
            {
                GameState.I.arrows--;
            }

            var go = new GameObject("Arrow");
            go.transform.position = transform.position + (Vector3)AimDir * 0.4f + new Vector3(0, 0.5f, 0);
            var a = go.AddComponent<Arrow>();
            ArrowKind k = selectedArrow;
            // Fire kind is decided by proximity to a firepot (V2 only).
            if (k == ArrowKind.Standard && BowController.IsNearFirepot(transform.position))
                k = ArrowKind.Fire;
            Sprite s = k switch { ArrowKind.Fire => arrowSpriteFire, ArrowKind.Wind => arrowSpriteWind, _ => arrowSpriteStandard };
            a.Setup(k, s, AimDir * 11f);
            AudioBus.I.Blip("T");
        }

        void Melee()
        {
            // Quick swipe in front of player; finds enemy in arc and damages.
            Vector2 origin = transform.position + new Vector3(0, 0.5f, 0);
            var hits = Physics2D.OverlapCircleAll(origin + AimDir * 0.5f, 0.55f);
            foreach (var h in hits)
            {
                var e = h.GetComponent<Aitugan.Enemies.EnemyBase>();
                if (e != null) e.Damage(1);
            }
        }

        void ThrowStone()
        {
            var go = new GameObject("Stone");
            go.transform.position = transform.position + (Vector3)AimDir * 0.4f + new Vector3(0, 0.5f, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProcGfx.MakeCircle(2, ProcGfx.Hex("#888888"), ProcGfx.Hex("#444444"));
            sr.sortingOrder = 50;
            var s = go.AddComponent<Stone>();
            s.velocity = AimDir * 8f;
        }

        void UseSaumal()
        {
            if (GameState.I.saumalFlasks <= 0) return;
            if (GameState.I.hp >= GameState.I.hpMax) return;
            GameState.I.saumalFlasks--;
            GameState.I.hp = Mathf.Min(GameState.I.hpMax, GameState.I.hp + 1);
        }

        public void TakeDamage(int amount, Vector2 source)
        {
            if (_dodgeTimer > 0) return;
            if (_hurtFlash > 0) return;
            GameState.I.hp -= amount;
            _hurtFlash = 0.3f;
            // Knock player away from source briefly
            Vector2 dir = ((Vector2)transform.position - source).normalized;
            _rb.AddForce(dir * 4f, ForceMode2D.Impulse);
            if (GameState.I.hp <= 0)
            {
                if (GameState.I.hasTumar && !GameState.I.tumarUsed)
                {
                    GameState.I.tumarUsed = true;
                    GameState.I.hp = GameState.I.hpMax;
                    _hurtFlash = 0.9f;
                }
                else
                {
                    Die();
                }
            }
        }

        void Die()
        {
            // Soft death: respawn at same vignette start. Real shipping build
            // would show a vignette-specific death card; for the placeholder
            // we just restore HP and keep the player going.
            GameState.I.hp = GameState.I.hpMax;
        }

        public void SetWoundedState(bool w)
        {
            wounded = w;
            GameState.I.shoulderWound = w;
        }
    }

    public class Stone : MonoBehaviour
    {
        public Vector2 velocity;
        float life = 1.5f;
        void Update()
        {
            transform.position += (Vector3)(velocity * Time.deltaTime);
            life -= Time.deltaTime;
            if (life <= 0) Destroy(gameObject);
        }
    }
}
