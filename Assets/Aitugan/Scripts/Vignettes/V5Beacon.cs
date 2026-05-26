using System.Collections;
using UnityEngine;
using Aitugan.Core;
using Aitugan.Player;
using Aitugan.Enemies;

namespace Aitugan.Vignettes
{
    public class V5Beacon : VignetteBase
    {
        public override string Title => "V.  The Beacon";
        bool _championDown = false;
        bool _beaconLit = false;

        protected override void BuildWorld()
        {
            skyColor = ProcGfx.Hex("#A88848");
            groundColor = ProcGfx.Hex("#86643C");
            groundTile = GroundTile.LightGround;
            // Pale golden dawn wash
            groundTint = new Color(1.0f, 0.92f, 0.78f);
            // Wide enough for the V5 vertical climb + panorama pull-back
            groundSize = new Vector2(50f, 32f);
            base.BuildWorld();

            // The cliff path winds up; we represent it as a wide vertical band.
            for (int i = -10; i < 10; i++)
            {
                SpawnSprite(ProcGfx.MakeNoise(32, 32, ProcGfx.Hex("#86643C"), 0.18f, 300 + i),
                    new Vector3(0, i * 1f, 0), "Path", -5);
            }
            // Cliff edges left/right
            for (int i = -10; i < 10; i++)
            {
                SpawnSprite(ProcGfx.MakeNoise(32, 16, ProcGfx.Hex("#5C3414"), 0.2f, 400 + i),
                    new Vector3(-3f, i * 1f, 0), "CliffL", -3);
                SpawnSprite(ProcGfx.MakeNoise(32, 16, ProcGfx.Hex("#5C3414"), 0.2f, 500 + i),
                    new Vector3(3f, i * 1f, 0), "CliffR", -3);
            }

            // Pickup at the climb start so V5 isn't softlocked if quiver is dry
            DropArrowPickup(new Vector3(0.6f, -4.2f, 0), 16);

            // Three rest points
            SpawnInteractable(new Vector3(-1.0f, -3f, 0), MakeRest(), "rest", () =>
                StartCoroutine(DialogueManager.I.Show("V5-04")));
            var rag = SpawnInteractable(new Vector3(1.0f, 0f, 0),
                Art.Paper != null ? Art.Paper : MakeRest(),
                "prayer rag", () => StartCoroutine(DialogueManager.I.Show("V5-05")));
            if (Art.Paper != null) rag.GetComponent<Interactable>().SetWorldScale(1.6f);
            SpawnInteractable(new Vector3(-1.0f, 3f, 0), MakeRest(), "high view", () =>
                StartCoroutine(DialogueManager.I.Show("V5-06")));

            // Beacon stack at top
            var beaconLot = SpawnSprite(ProcGfx.MakeRect(40, 50, ProcGfx.Hex("#2A1A0C"), ProcGfx.Hex("#5C3414")),
                new Vector3(0, 6.5f, 0), "BeaconUnlit", 4);

            // Champion mini-boss
            var champ = SpawnEnemy(EnemyKind.Champion, new Vector3(0.6f, 5.6f, 0));
            champ.OnDeath = () => { _championDown = true; };

            // Light flint trigger (after champion falls)
            var lit = new GameObject("LightFlintTrigger");
            lit.transform.SetParent(worldRoot.transform, false);
            lit.transform.position = new Vector3(0, 6.2f, 0);
            var c = lit.AddComponent<CircleCollider2D>();
            c.radius = 0.8f;
            c.isTrigger = true;
            lit.AddComponent<LightTrigger>().vignette = this;
        }

        Sprite MakeRest() => ProcGfx.MakeCircle(8, ProcGfx.Hex("#A88848"), ProcGfx.Hex("#5C3414"));

        protected override void SpawnPlayer()
        {
            base.SpawnPlayer();
            player.transform.position = new Vector3(0, -5f, 0);
            player.SetWoundedState(GameState.I.shoulderWound);
        }

        public IEnumerator OnLightBeacon()
        {
            if (_beaconLit) yield break;
            if (!_championDown) yield break;
            _beaconLit = true;
            yield return DialogueManager.I.Show("V5-11");

            // Visual ignition
            var beacon = worldRoot.transform.Find("BeaconUnlit");
            if (beacon != null)
            {
                var sr = beacon.GetComponent<SpriteRenderer>();
                sr.sprite = ProcGfx.MakeRect(40, 60, ProcGfx.Hex("#FF8000"), ProcGfx.Hex("#FFE0A0"));
                beacon.gameObject.AddComponent<BeaconBlaze>();
            }

            yield return new WaitForSeconds(1.2f);

            // Pull camera back wide
            var cam = Camera.main;
            var follow = cam.GetComponent<CameraFollow>();
            if (follow != null) follow.enabled = false;

            float t0 = 0;
            float startSize = cam.orthographicSize;
            Vector3 startPos = cam.transform.position;
            Vector3 endPos = new Vector3(0, 6.5f, -10);
            while (t0 < 2.5f)
            {
                t0 += Time.deltaTime;
                float k = t0 / 2.5f;
                cam.orthographicSize = Mathf.Lerp(startSize, 9f, k);
                cam.transform.position = Vector3.Lerp(startPos, endPos, k);
                yield return null;
            }

            // Spawn the Bukharan column on the southern horizon
            Sprite bukSprite = Art.Allies != null ? Art.Allies : ProcGfx.MakeDzungar(ProcGfx.Hex("#1A4A78"));
            float bukScale = Art.Allies != null ? 1.8f : 0.5f;
            for (int i = 0; i < 14; i++)
            {
                var go = new GameObject("Bukharan");
                go.transform.SetParent(worldRoot.transform, false);
                go.transform.position = new Vector3(-12 + i * 0.7f, -2.5f, 0);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = bukSprite;
                sr.sortingOrder = 10;
                sr.color = new Color(0.7f, 0.7f, 0.85f);
                go.transform.localScale = new Vector3(bukScale, bukScale, 1f);
            }
            // Banner glints
            for (int i = 0; i < 5; i++)
            {
                var b = new GameObject("Banner");
                b.transform.SetParent(worldRoot.transform, false);
                b.transform.position = new Vector3(-9 + i * 1.6f, -1.6f, 0);
                var sr = b.AddComponent<SpriteRenderer>();
                sr.sprite = ProcGfx.MakeRect(8, 24, ProcGfx.Hex("#1A8030"), ProcGfx.Hex("#FFD040"));
                sr.sortingOrder = 12;
            }

            yield return new WaitForSeconds(2f);
            yield return DialogueManager.I.Show("V5-12");
            yield return DialogueManager.I.Show("V5-13");
            yield return DialogueManager.I.Show("V5-14");

            // Aitugan sits down beside the beacon (just stop input)
            player.canMove = false;
            yield return new WaitForSeconds(2f);
            yield return FadeBlack(2f, false);

            _exit = true;
        }

        bool _exit = false;

        protected override IEnumerator Play()
        {
            yield return FadeBlack(0.8f, true);
            yield return DialogueManager.I.Show("V5-01");
            yield return DialogueManager.I.Show("V5-02");
            yield return DialogueManager.I.Show("V5-03");

            // Wait until they reach top and confront the champion
            while (player.transform.position.y < 4f) yield return null;
            yield return DialogueManager.I.Show("V5-07");
            yield return DialogueManager.I.Show("V5-08");
            yield return DialogueManager.I.Show("V5-09");

            while (!_championDown) yield return null;

            // The tumar moment, if it happened
            if (GameState.I.tumarUsed) yield return DialogueManager.I.Show("V5-10");

            while (!_exit) yield return null;
        }

        class LightTrigger : MonoBehaviour
        {
            public V5Beacon vignette;
            bool _used;
            void OnTriggerStay2D(Collider2D other)
            {
                if (_used) return;
                if (other.GetComponent<AituganController>() != null && InputBus.I.InteractPressed)
                {
                    _used = true;
                    StartCoroutine(vignette.OnLightBeacon());
                }
            }
        }

        class BeaconBlaze : MonoBehaviour
        {
            SpriteRenderer sr;
            void Awake() { sr = GetComponent<SpriteRenderer>(); }
            void Update()
            {
                float k = 0.85f + Mathf.Sin(Time.time * 18f + GetInstanceID()) * 0.15f;
                sr.color = new Color(k, k * 0.7f, k * 0.3f);
            }
        }
    }
}
