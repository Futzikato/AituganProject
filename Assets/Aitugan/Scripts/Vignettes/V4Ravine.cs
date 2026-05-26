using System.Collections;
using UnityEngine;
using Aitugan.Core;
using Aitugan.Player;
using Aitugan.Enemies;

namespace Aitugan.Vignettes
{
    public class V4Ravine : VignetteBase
    {
        public override string Title => "IV.  The Wolves at Dusk";
        bool _passedSleeper = false;
        bool _gotWindArrows = false;
        bool _championDown = false;

        protected override void BuildWorld()
        {
            skyColor = ProcGfx.Hex("#0E1228");
            groundColor = ProcGfx.Hex("#16203A");
            groundTile = GroundTile.Grass;
            // Moonlight wash - cold blue tint
            groundTint = new Color(0.55f, 0.65f, 0.85f);
            base.BuildWorld();

            // Long horizontal ravine (player walks east)
            // Cliffs on both sides
            for (int i = -16; i < 16; i++)
            {
                SpawnSprite(ProcGfx.MakeNoise(32, 16, ProcGfx.Hex("#1A2A3A"), 0.18f, 100 + i),
                    new Vector3(i, 3.0f, 0), "NorthCliff", -3);
                SpawnSprite(ProcGfx.MakeNoise(32, 16, ProcGfx.Hex("#1A2A3A"), 0.18f, 200 + i),
                    new Vector3(i, -3.0f, 0), "SouthCliff", -3);
            }

            // Stars (just dot decoration in upper sky)
            for (int i = 0; i < 24; i++)
            {
                var p = new Vector3(Random.Range(-15f, 15f), Random.Range(2.5f, 4f), 0);
                SpawnSprite(ProcGfx.MakeRect(2, 2, ProcGfx.Hex("#E0E8FF"), ProcGfx.Hex("#E0E8FF")), p, "Star", -2);
            }

            // Sleeping Dzungar
            var sleeper = SpawnEnemy(EnemyKind.Sleeper, new Vector3(-3f, 0.2f, 0));
            sleeper.OnDeath = () => { GameState.I.killedSleeper = true; };

            // Father's pack (mid-ravine). Scale bumped from 2.2 -> 3.8 so he
            // reads as a proper figure on the ravine floor rather than a
            // postage-stamp prop the player has to squint at.
            var pack = SpawnInteractable(new Vector3(2f, 0.4f, 0),
                Art.Father != null ? Art.Father : ProcGfx.MakeRect(20, 14, ProcGfx.Hex("#4A2A14"), ProcGfx.Hex("#1A0E06")),
                "father's pack", () => StartCoroutine(OpenPack()));
            if (Art.Father != null) pack.GetComponent<Interactable>().SetWorldScale(3.8f);

            // Two-Dzungar choke
            SpawnEnemy(EnemyKind.Basic, new Vector3(6f, 0.6f, 0));
            SpawnEnemy(EnemyKind.Basic, new Vector3(7f, -0.4f, 0));

            // The champion-grade Dzungar at the far end
            var champ = SpawnEnemy(EnemyKind.Champion, new Vector3(11f, 0.3f, 0));
            champ.OnDeath = () => OnChampionDown();
        }

        protected override void SpawnPlayer()
        {
            base.SpawnPlayer();
            player.transform.position = new Vector3(-7f, 0, 0);
        }

        IEnumerator OpenPack()
        {
            yield return DialogueManager.I.Show("V4-06");
            yield return DialogueManager.I.Show("V4-07");
            yield return DialogueManager.I.Show("V4-08");
            yield return DialogueManager.I.Show("V4-09");
            yield return DialogueManager.I.Show("V4-10");
            GameState.I.hasWindArrows = true;
            GameState.I.windArrows = 6;
            // Father topped up her standard quiver too.
            GameState.I.arrows = Mathf.Max(GameState.I.arrows, 18);
            _gotWindArrows = true;
        }

        void OnChampionDown()
        {
            _championDown = true;
            // Scripted shoulder graze (cosmetic): force Aitugan to take a wound state
            player.SetWoundedState(true);
            StartCoroutine(DialogueManager.I.Show("V4-19"));
        }

        protected override IEnumerator Play()
        {
            yield return FadeBlack(0.8f, true);

            // Companion fragment
            yield return DialogueManager.I.Show("V4-01");
            yield return DialogueManager.I.Show("V4-02");

            // Approach sleeper
            while (!_passedSleeper)
            {
                if (Vector3.Distance(player.transform.position, new Vector3(-3f, 0.2f, 0)) < 1.4f)
                {
                    _passedSleeper = true;
                }
                yield return null;
            }
            yield return DialogueManager.I.Show("V4-03");
            yield return DialogueManager.I.Show("V4-04");
            // Wait until player is past
            while (player.transform.position.x < -1.5f) yield return null;
            if (GameState.I.killedSleeper)
                yield return DialogueManager.I.Show("V4-05");

            // Wait for father's pack interaction
            while (!_gotWindArrows) yield return null;

            // Approach the choke
            while (player.transform.position.x < 5f) yield return null;
            yield return DialogueManager.I.Show("V4-11");
            yield return DialogueManager.I.Show("V4-12");

            // During fight, fragment fires
            float t = 0;
            bool firedMidFight = false;
            while (player.transform.position.x < 9f)
            {
                t += Time.deltaTime;
                if (!firedMidFight && t > 4f)
                {
                    firedMidFight = true;
                    yield return DialogueManager.I.Show("V4-13");
                }
                yield return null;
            }

            // Companion goes silent
            yield return DialogueManager.I.Show("V4-14");
            yield return DialogueManager.I.Show("V4-15");
            yield return DialogueManager.I.Show("V4-16");
            yield return DialogueManager.I.Show("V4-17");

            // Champion encounter prompt
            yield return DialogueManager.I.Show("V4-18");

            // Wait for champion down
            while (!_championDown) yield return null;

            yield return DialogueManager.I.Show("V4-20");
            yield return DialogueManager.I.Show("V4-21");

            yield return FadeBlack(1.2f, false);
        }
    }
}
