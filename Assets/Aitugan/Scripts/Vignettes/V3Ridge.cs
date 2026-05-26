using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Aitugan.Core;
using Aitugan.Player;
using Aitugan.Enemies;

namespace Aitugan.Vignettes
{
    public class V3Ridge : VignetteBase
    {
        public override string Title => "III.  The Errand";
        bool _hasPowder = false;
        bool _returned = false;

        protected override void BuildWorld()
        {
            skyColor = ProcGfx.Hex("#876C40");
            groundColor = ProcGfx.Hex("#A88848");
            groundTile = GroundTile.LightGround;
            base.BuildWorld();

            // Ridge: a long horizontal band of rock, with the cliff edge to the south.
            // Cliff drop indicated by a darker strip below.
            var cliff = SpawnSprite(ProcGfx.MakeNoise(160, 30, ProcGfx.Hex("#5C3414"), 0.2f, 31),
                new Vector3(0, -4f, 0), "Cliff");
            cliff.transform.localScale = new Vector3(5f, 4f, 1f);
            cliff.GetComponent<SpriteRenderer>().sortingOrder = -8;

            // Distant battle below (south, parallax)
            var battle = SpawnSprite(ProcGfx.MakeNoise(160, 30, ProcGfx.Hex("#3A2614"), 0.4f, 99),
                new Vector3(0, -7f, 0), "DistantBattle");
            battle.transform.localScale = new Vector3(7f, 3f, 1f);
            battle.GetComponent<SpriteRenderer>().sortingOrder = -9;
            battle.AddComponent<DistantChaos>();

            // Scrub bushes scattered along the path
            for (int i = 0; i < 18; i++)
            {
                float x = -16f + i * 2f + Random.Range(-0.3f, 0.3f);
                float y = Random.Range(-1f, 2f);
                SpawnSprite(ProcGfx.MakeCircle(6, ProcGfx.Hex("#3A4A20"), ProcGfx.Hex("#1A2410")),
                    new Vector3(x, y, 0), $"Scrub{i}", -3);
            }

            // Carved boulder (V3-07)
            SpawnInteractable(new Vector3(-3f, 1.5f, 0),
                ProcGfx.MakeRect(36, 28, ProcGfx.Hex("#86643C"), ProcGfx.Hex("#3A2614")),
                "carved boulder", () => StartCoroutine(DialogueManager.I.Show("V3-07")));

            // First scout patrol
            var s1 = SpawnEnemy(EnemyKind.Scout, new Vector3(-1f, 0.5f, 0));
            // The dead messenger + powder cache
            SpawnSprite(ProcGfx.MakeRect(28, 14, ProcGfx.Hex("#806038"), ProcGfx.Hex("#3A2614")),
                new Vector3(8f, 1f, 0), "MessengerBody", 1).GetComponent<SpriteRenderer>().color = new Color(0.7f, 0.6f, 0.5f);

            SpawnInteractable(new Vector3(8.4f, 0.4f, 0),
                ProcGfx.MakeRect(20, 14, ProcGfx.Hex("#5A3614"), ProcGfx.Hex("#1A0E06")),
                "powder satchel", () => StartCoroutine(GrabPowder()));

            var letter = SpawnInteractable(new Vector3(7.6f, 1.2f, 0),
                Art.Paper != null ? Art.Paper : ProcGfx.MakeRect(14, 18, ProcGfx.Hex("#C7A266"), ProcGfx.Hex("#5C3414")),
                "messenger's letter", () => StartCoroutine(ReadLetter()));
            if (Art.Paper != null) letter.GetComponent<Interactable>().SetWorldScale(1.8f);

            // Second patrol on the return path
            var s2 = SpawnEnemy(EnemyKind.Scout, new Vector3(3f, 0.8f, 0));

            // Trigger zone for return
            var ret = new GameObject("ReturnZone");
            ret.transform.SetParent(worldRoot.transform, false);
            ret.transform.position = new Vector3(-7f, 0, 0);
            var c = ret.AddComponent<BoxCollider2D>();
            c.size = new Vector2(2, 4);
            c.isTrigger = true;
            ret.AddComponent<ReturnZone>().vignette = this;
        }

        protected override void SpawnPlayer()
        {
            base.SpawnPlayer();
            player.transform.position = new Vector3(-6f, 0, 0);
        }

        IEnumerator GrabPowder()
        {
            yield return DialogueManager.I.Show("V3-08");
            yield return DialogueManager.I.Show("V3-09");
            yield return DialogueManager.I.Show("V3-14");
            _hasPowder = true;
        }

        IEnumerator ReadLetter()
        {
            yield return DialogueManager.I.Show("V3-10");
            yield return DialogueManager.I.Show("V3-11");
            yield return DialogueManager.I.Show("V3-12");
            yield return DialogueManager.I.Show("V3-13");
            GameState.I.readMessengerLetter = true;
        }

        public IEnumerator OnReturn()
        {
            if (_returned) yield break;
            if (!_hasPowder)
            {
                yield return DialogueManager.I.Show("V3-15");
                yield break;
            }
            _returned = true;
            yield return DialogueManager.I.Show("V3-17");
            yield return DialogueManager.I.Show("V3-18");
            _exit = true;
        }

        bool _exit = false;

        protected override IEnumerator Play()
        {
            yield return FadeBlack(0.8f, true);

            // Cresting the ridge
            yield return DialogueManager.I.Show("V3-01");
            yield return DialogueManager.I.Show("V3-02");
            yield return DialogueManager.I.Show("V3-03");

            // Approaching first scout (proximity-based)
            bool firstScoutTalked = false;
            while (!_exit)
            {
                if (!firstScoutTalked && player.transform.position.x > -2f)
                {
                    firstScoutTalked = true;
                    yield return DialogueManager.I.Show("V3-04");
                    yield return DialogueManager.I.Show("V3-05");
                }
                yield return null;
            }
            yield return FadeBlack(0.8f, false);
        }

        class DistantChaos : MonoBehaviour
        {
            SpriteRenderer sr;
            float t;
            void Awake() { sr = GetComponent<SpriteRenderer>(); }
            void Update()
            {
                t += Time.deltaTime;
                float k = 0.7f + Mathf.Sin(t * 0.6f) * 0.05f;
                sr.color = new Color(0.3f * k, 0.2f * k, 0.12f * k, 1f);
            }
        }

        class ReturnZone : MonoBehaviour
        {
            public V3Ridge vignette;
            bool _used;
            void OnTriggerEnter2D(Collider2D other)
            {
                if (_used) return;
                if (other.GetComponent<AituganController>() != null && vignette._hasPowder)
                {
                    _used = true;
                    StartCoroutine(vignette.OnReturn());
                }
            }
        }
    }
}
