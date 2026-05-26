using System.Collections;
using UnityEngine;
using Aitugan.Core;
using Aitugan.Player;
using Aitugan.UI;

namespace Aitugan.Vignettes
{
    /// <summary>
    /// Shared scaffolding for every vignette: world background, simple
    /// camera follow, palette swap, helpers for spawning props/enemies.
    /// </summary>
    public abstract class VignetteBase : MonoBehaviour
    {
        protected GameObject worldRoot;
        protected AituganController player;
        protected Color skyColor = ProcGfx.Hex("#1A1208");
        protected Color groundColor = ProcGfx.Hex("#3C2C18");

        // Each vignette can override which authored tile to use for its ground.
        // If null or unavailable, falls back to procedural noise of `groundColor`.
        protected enum GroundTile { None, Floor, Grass, Ground, LightGround }
        protected GroundTile groundTile = GroundTile.Ground;
        // Optional tint applied to the tiled ground (e.g. moonlight on grass).
        protected Color groundTint = Color.white;
        // World-units coverage of the ground - large enough for any vignette.
        protected Vector2 groundSize = new Vector2(40f, 24f);

        public abstract string Title { get; }

        public IEnumerator Run()
        {
            BuildWorld();
            SpawnPlayer();
            ApplyPalette();
            Hud.I.vignetteTitle = Title;
            yield return Play();
            Hud.I.vignetteTitle = "";
            BowController.ClearFirepots();
        }

        protected virtual void BuildWorld()
        {
            worldRoot = new GameObject("World");
            worldRoot.transform.SetParent(transform, false);

            SpawnTiledGround();
        }

        protected GameObject SpawnTiledGround()
        {
            Sprite tile = ResolveGroundTile();
            var go = new GameObject("Ground");
            go.transform.SetParent(worldRoot.transform, false);
            go.transform.position = Vector3.zero;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -10;

            if (tile != null)
            {
                sr.sprite = tile;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = groundSize;
                sr.color = groundTint;
            }
            else
            {
                // Fallback - procedural noise matching the vignette colour palette
                sr.sprite = ProcGfx.MakeNoise(64, 36, groundColor, 0.18f, 1234);
                go.transform.localScale = new Vector3(2.6f, 2.6f, 1f);
            }
            return go;
        }

        Sprite ResolveGroundTile()
        {
            return groundTile switch
            {
                GroundTile.Floor       => Art.Floor,
                GroundTile.Grass       => Art.Grass,
                GroundTile.Ground      => Art.Ground,
                GroundTile.LightGround => Art.LightGround,
                _ => null,
            };
        }

        protected virtual void SpawnPlayer()
        {
            var go = new GameObject("Aitugan");
            go.transform.SetParent(transform, false);
            player = go.AddComponent<AituganController>();
            go.transform.position = new Vector3(0, 0, 0);
            // Place camera target
            var cam = Camera.main;
            if (cam != null)
            {
                var follow = cam.gameObject.GetComponent<CameraFollow>();
                if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
                follow.target = go.transform;
            }
        }

        protected virtual void ApplyPalette()
        {
            if (Camera.main != null)
                Camera.main.backgroundColor = skyColor;
        }

        protected abstract IEnumerator Play();

        protected GameObject SpawnSprite(Sprite sprite, Vector3 pos, string name = null, int sortingOrder = 0)
        {
            var go = new GameObject(name ?? "Sprite");
            go.transform.SetParent(worldRoot.transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            return go;
        }

        protected GameObject SpawnSolid(Sprite sprite, Vector3 pos, Vector2 size, string name = null, int sortingOrder = 0)
        {
            var go = SpawnSprite(sprite, pos, name, sortingOrder);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            go.tag = "Untagged";
            return go;
        }

        protected GameObject SpawnInteractable(Vector3 pos, Sprite sprite, string promptLabel, System.Action onInteract, string name = null)
        {
            var go = SpawnSprite(sprite, pos, name, 5);
            var t = go.AddComponent<Interactable>();
            t.label = promptLabel;
            t.onInteract = onInteract;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.45f; // tight, so neighbouring props don't share range
            col.isTrigger = true;
            return go;
        }

        protected void DropArrowPickup(Vector3 pos, int amount = 12)
        {
            var go = new GameObject("ArrowPickup");
            go.transform.SetParent(worldRoot.transform, false);
            go.transform.position = pos;
            var p = go.AddComponent<Aitugan.Player.ArrowPickup>();
            p.amount = amount;
        }

        protected Aitugan.Enemies.EnemyBase SpawnEnemy(Aitugan.Enemies.EnemyKind kind, Vector3 pos)
        {
            var go = new GameObject("Enemy");
            go.transform.SetParent(worldRoot.transform, false);
            go.transform.position = pos;
            var e = go.AddComponent<Aitugan.Enemies.EnemyBase>();
            e.Setup(kind);
            return e;
        }

        protected IEnumerator Wait(float seconds)
        {
            float t = 0;
            while (t < seconds) { t += Time.deltaTime; yield return null; }
        }

        protected IEnumerator FadeBlack(float seconds, bool fadeIn)
        {
            var fader = Fader.Get();
            yield return fader.Fade(seconds, fadeIn);
        }
    }

    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float lerp = 4f;
        public Vector2 offset = new Vector2(0, 1.4f);
        public Vector2 deadzone = new Vector2(0.5f, 0.5f);
        void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * lerp);
        }
    }

    public class Interactable : MonoBehaviour
    {
        public string label;
        public System.Action onInteract;
        public bool consumed = false;
        bool _inRange = false;
        SpriteRenderer _sr;

        static readonly System.Collections.Generic.List<Interactable> InRange = new();

        void Awake() { _sr = GetComponent<SpriteRenderer>(); }
        void OnDestroy() { InRange.Remove(this); }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<AituganController>() != null)
            {
                _inRange = true;
                if (!InRange.Contains(this)) InRange.Add(this);
            }
        }
        void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<AituganController>() != null)
            {
                _inRange = false;
                InRange.Remove(this);
            }
        }

        /// <summary>
        /// Scale the sprite visually while keeping the world-space interaction
        /// radius at a constant ~0.45u. Without this compensation, scaling the
        /// GameObject also scales the CircleCollider2D, which made the bedroll
        /// reach all the way to the bow / kinzhal at the other side of the yurt.
        /// </summary>
        public void SetWorldScale(float scale)
        {
            transform.localScale = Vector3.one * scale;
            var col = GetComponent<CircleCollider2D>();
            if (col != null) col.radius = 0.45f / scale;
        }

        bool IsClosestInRange()
        {
            var p = AituganController.I; if (p == null) return false;
            float bestSq = float.MaxValue;
            Interactable best = null;
            for (int i = 0; i < InRange.Count; i++)
            {
                var x = InRange[i];
                if (x == null || x.consumed) continue;
                float sq = ((Vector2)(x.transform.position - p.transform.position)).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = x; }
            }
            return best == this;
        }

        void Update()
        {
            if (consumed) { InRange.Remove(this); return; }
            bool closest = _inRange && IsClosestInRange();
            if (_sr != null)
                _sr.color = closest
                    ? Color.Lerp(Color.white, new Color(1.4f, 1.3f, 1f), Mathf.PingPong(Time.time * 2f, 1f))
                    : Color.white;
            if (closest && InputBus.I.InteractPressed && DialogueManager.I != null && !DialogueManager.I.IsShowing)
            {
                onInteract?.Invoke();
            }
        }

        void OnGUI()
        {
            if (consumed || !_inRange) return;
            if (!IsClosestInRange()) return;
            if (DialogueManager.I != null && DialogueManager.I.IsShowing) return;
            var cam = Camera.main; if (cam == null) return;
            var screen = cam.WorldToScreenPoint(transform.position + new Vector3(0, 1f, 0));
            Aitugan.Core.Ui.EnsureSkin();
            var style = new GUIStyle() { font = Aitugan.Core.Ui.Font, fontSize = Aitugan.Core.Ui.Sized(12), alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.95f, 0.85f, 0.55f) } };
            float w = Aitugan.Core.Ui.Px(200f);
            bool touch = UnityEngine.InputSystem.Touchscreen.current != null;
            GUI.Label(new Rect(screen.x - w / 2, Screen.height - screen.y - Aitugan.Core.Ui.Px(12), w, Aitugan.Core.Ui.Px(24)), (touch ? "[tap] " : "[E] ") + label, style);
        }
    }

    public class Fader : MonoBehaviour
    {
        static Fader _i;
        Texture2D _tex;
        float _alpha;

        public static Fader Get()
        {
            if (_i != null) return _i;
            var go = new GameObject("[Fader]");
            DontDestroyOnLoad(go);
            _i = go.AddComponent<Fader>();
            _i._tex = ProcGfx.MakeRect(2, 2, Color.black, Color.black).texture;
            return _i;
        }

        public IEnumerator Fade(float seconds, bool fadeIn)
        {
            float t = 0;
            float start = fadeIn ? 1f : 0f;
            float end = fadeIn ? 0f : 1f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                _alpha = Mathf.Lerp(start, end, t / seconds);
                yield return null;
            }
            _alpha = end;
        }

        void OnGUI()
        {
            if (_alpha <= 0.001f) return;
            var c = GUI.color;
            GUI.color = new Color(0, 0, 0, _alpha);
            GUI.depth = -1000;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _tex);
            GUI.color = c;
        }
    }
}
