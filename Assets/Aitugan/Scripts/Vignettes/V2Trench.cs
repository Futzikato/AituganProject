using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Aitugan.Core;
using Aitugan.Player;
using Aitugan.Enemies;

namespace Aitugan.Vignettes
{
    public class V2Trench : VignetteBase
    {
        public override string Title => "II.  The Trench";

        readonly List<EnemyBase> _alive = new();
        bool _learnedFireArrow = false;

        protected override void BuildWorld()
        {
            skyColor = ProcGfx.Hex("#241A0E");
            groundColor = ProcGfx.Hex("#3A2A18");
            groundTile = GroundTile.Ground;
            base.BuildWorld();

            // Trench wall behind player (south)
            for (int i = 0; i < 14; i++)
            {
                var w = SpawnSolid(ProcGfx.MakeNoise(32, 32, ProcGfx.Hex("#3A2614"), 0.15f, 200 + i),
                    new Vector3(-7 + i, -4f, 0), new Vector2(1, 1), $"TrenchBackWall{i}", -3);
                Destroy(w.GetComponent<BoxCollider2D>()); // backdrop only
            }
            // Trench front lip (north) - hard wall, enemies cross over but player can't
            for (int i = 0; i < 14; i++)
            {
                var w = SpawnSolid(ProcGfx.MakeRect(32, 12, ProcGfx.Hex("#5A3A1C"), ProcGfx.Hex("#1A0E06")),
                    new Vector3(-7 + i, -1.4f, 0), new Vector2(1, 0.4f), $"TrenchLip{i}", -1);
                Destroy(w.GetComponent<BoxCollider2D>());
            }

            // Firepot beside the player
            var firepot = SpawnSprite(MakeFirepot(), new Vector3(-1.6f, -2.8f, 0), "Firepot", 4);
            BowController.RegisterFirepot(firepot.transform.position);
            // The flicker
            firepot.AddComponent<Flicker>();

            // Quiver stand
            SpawnSprite(MakeQuiver(), new Vector3(1.6f, -3.2f, 0), "QuiverStand", 3);

            // Distant silhouette warriors three slots down - use the authored
            // Allies sprite when available.
            Sprite allySprite = Art.Allies != null ? Art.Allies : ProcGfx.MakeDzungar(ProcGfx.Hex("#352618"));
            for (int i = 0; i < 3; i++)
            {
                var sx = -5 + i * 4f;
                var s = SpawnSprite(allySprite, new Vector3(sx, -3.1f, 0), $"AllyDistant{i}", 2);
                s.GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.5f, 0.4f);
                float sc = Art.Allies != null ? 2.5f : 0.8f;
                s.transform.localScale = new Vector3(sc, sc, 1f);
            }

            // Pinned order on the trench wall
            var order = SpawnInteractable(new Vector3(2.6f, -2.6f, 0), MakeNote(),
                "torn order", () => StartCoroutine(DialogueManager.I.Show("V2-05")));
            if (Art.Paper != null) order.GetComponent<Interactable>().SetWorldScale(1.8f);

            // The half-burnt scrap appears between waves; spawn it later
        }

        protected override void SpawnPlayer()
        {
            base.SpawnPlayer();
            player.transform.position = new Vector3(0f, -3f, 0);
            player.lockedToTrenchY = true;
            player.trenchY = -3f;
            player.trenchMinX = -2.2f;
            player.trenchMaxX = 2.2f;
        }

        Sprite MakeFirepot() => ProcGfx.MakeRect(20, 24, ProcGfx.Hex("#FF6020"), ProcGfx.Hex("#1A1108"));
        Sprite MakeQuiver() => ProcGfx.MakeRect(8, 24, ProcGfx.Hex("#5C3414"), ProcGfx.Hex("#1A0E06"));
        Sprite MakeNote() => Art.Paper != null ? Art.Paper : ProcGfx.MakeRect(14, 18, ProcGfx.Hex("#C7A266"), ProcGfx.Hex("#5C3414"));

        protected override IEnumerator Play()
        {
            yield return FadeBlack(0.8f, true);
            yield return DialogueManager.I.Show("V2-01");
            yield return DialogueManager.I.Show("V2-02");
            yield return DialogueManager.I.Show("V2-03");
            yield return DialogueManager.I.Show("V2-04");

            // Wave 1
            yield return DialogueManager.I.Show("V2-06");
            yield return SpawnWave(new[] { EnemyKind.Basic, EnemyKind.Basic, EnemyKind.Basic, EnemyKind.Basic });
            // taunt during the wave (fire at idle - just play once)
            // (omitted timer-based variant for brevity)

            yield return WaitForWaveClear();
            DropArrowPickup(new Vector3(-2.0f, -3.4f, 0), 14);
            DropArrowPickup(new Vector3( 2.0f, -3.4f, 0), 14);
            yield return DialogueManager.I.Show("V2-07");
            yield return DialogueManager.I.Show("V2-08");
            yield return DialogueManager.I.Show("V2-09");

            // Spawn the half-burnt scrap mid-trench
            var scrap = SpawnInteractable(new Vector3(0.8f, -3.4f, 0), MakeNote(),
                "burnt scrap", () => StartCoroutine(DialogueManager.I.Show("V2-10")));
            if (Art.Paper != null) scrap.GetComponent<Interactable>().SetWorldScale(1.5f);

            yield return new WaitForSeconds(2.5f);

            // Wave 2 - shielded
            yield return DialogueManager.I.Show("V2-11");
            yield return SpawnWave(new[] { EnemyKind.Basic, EnemyKind.Shielded, EnemyKind.Basic, EnemyKind.Shielded, EnemyKind.Basic, EnemyKind.Basic });
            yield return WaitForWaveClear();
            DropArrowPickup(new Vector3(-1.4f, -3.4f, 0), 14);
            DropArrowPickup(new Vector3( 1.4f, -3.4f, 0), 14);
            yield return DialogueManager.I.Show(_learnedFireArrow ? "V2-13" : "V2-12");

            // Inter-wave
            yield return DialogueManager.I.Show("V2-14");
            yield return DialogueManager.I.Show("V2-15");

            // Wave 3 - mixed, with a "matchlock" cinematic
            yield return SpawnWave(new[] { EnemyKind.Basic, EnemyKind.Basic, EnemyKind.Shielded, EnemyKind.Mounted, EnemyKind.Basic, EnemyKind.Basic, EnemyKind.Shielded, EnemyKind.Basic });
            // Halfway through, an offscreen matchlock fells the mounted attacker
            yield return new WaitForSeconds(2.0f);
            FellMounted();
            yield return DialogueManager.I.Show("V2-16");
            yield return DialogueManager.I.Show("V2-17");
            yield return WaitForWaveClear();
            DropArrowPickup(new Vector3(-2.0f, -3.4f, 0), 16);
            DropArrowPickup(new Vector3( 0.0f, -3.4f, 0), 16);
            DropArrowPickup(new Vector3( 2.0f, -3.4f, 0), 16);

            yield return DialogueManager.I.Show("V2-18");
            yield return DialogueManager.I.Show("V2-19");
            yield return DialogueManager.I.Show("V2-20");

            // Wave 4 - chaotic
            yield return DialogueManager.I.Show("V2-21");
            yield return SpawnWave(new[] { EnemyKind.Basic, EnemyKind.Basic, EnemyKind.Basic, EnemyKind.Shielded, EnemyKind.Shielded, EnemyKind.Basic, EnemyKind.Basic, EnemyKind.Mounted, EnemyKind.Basic, EnemyKind.Basic, EnemyKind.Shielded, EnemyKind.Basic });

            // Mid-wave: ally three-slots-down falls (visual only)
            yield return new WaitForSeconds(3f);
            FellDistantAlly();
            yield return DialogueManager.I.Show("V2-22");

            yield return WaitForWaveClear();

            // Whistle for the runner
            yield return DialogueManager.I.Show("V2-23");
            yield return DialogueManager.I.Show("V2-24");

            yield return FadeBlack(0.8f, false);
        }

        IEnumerator SpawnWave(EnemyKind[] kinds)
        {
            foreach (var k in kinds)
            {
                float x = Random.Range(-3f, 3f);
                var e = SpawnEnemy(k, new Vector3(x, 4f, 0));
                _alive.Add(e);
                e.OnDeath = () => _alive.Remove(e);
                if (k == EnemyKind.Shielded && !_learnedFireArrow)
                {
                    // Detect first kill of a shielded enemy via fire arrow as success
                    e.OnDeath = () => { _alive.Remove(e); _learnedFireArrow = true; };
                }
                yield return new WaitForSeconds(0.7f);
            }
        }

        IEnumerator WaitForWaveClear()
        {
            while (_alive.Count > 0)
            {
                _alive.RemoveAll(x => x == null);
                yield return null;
            }
        }

        void FellMounted()
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                if (_alive[i] != null && _alive[i].kind == EnemyKind.Mounted)
                {
                    var e = _alive[i];
                    _alive.RemoveAt(i);
                    Destroy(e.gameObject);
                    SpawnFlash(e.transform.position);
                    return;
                }
            }
        }

        void FellDistantAlly()
        {
            // Find AllyDistant and fade one
            var t = worldRoot.transform.Find("AllyDistant1");
            if (t != null) t.GetComponent<SpriteRenderer>().color = new Color(0.2f, 0.15f, 0.12f, 0.4f);
        }

        void SpawnFlash(Vector3 p)
        {
            var go = SpawnSprite(ProcGfx.MakeCircle(20, ProcGfx.Hex("#FFE0A0"), ProcGfx.Hex("#FF8000")), p, "Flash", 60);
            Destroy(go, 0.3f);
        }

        class Flicker : MonoBehaviour
        {
            SpriteRenderer sr;
            void Awake() { sr = GetComponent<SpriteRenderer>(); }
            void Update()
            {
                if (sr != null)
                {
                    float k = 0.85f + Mathf.Sin(Time.time * 12f + GetInstanceID()) * 0.15f;
                    sr.color = new Color(k, k * 0.6f, k * 0.2f);
                }
            }
        }
    }
}
