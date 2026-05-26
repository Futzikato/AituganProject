using System.Collections;
using UnityEngine;
using Aitugan.Core;
using Aitugan.Player;

namespace Aitugan.Vignettes
{
    public class V1Yurt : VignetteBase
    {
        public override string Title => "I.  Before First Light";

        bool _gotBow = false, _gotKinzhal = false;
        Interactable _doorflap;

        protected override void BuildWorld()
        {
            skyColor = ProcGfx.Hex("#1B1004");
            groundColor = ProcGfx.Hex("#3A2614");
            // Inside the yurt - use the authored Floor tile.
            groundTile = GroundTile.Floor;
            groundTint = new Color(0.85f, 0.78f, 0.68f);
            base.BuildWorld();

            // Yurt floor: a larger circular felt mat that fills the interior
            // ring. Bumped up from 2x to 2.6x and warmed up a touch for better
            // contrast with the props above it.
            var floor = SpawnSprite(ProcGfx.MakeNoise(48, 48, ProcGfx.Hex("#5C3A1E"), 0.12f, 11), Vector3.zero, "YurtFloor");
            floor.transform.localScale = new Vector3(2.6f, 2.6f, 1f);
            floor.GetComponent<SpriteRenderer>().sortingOrder = -8;
            floor.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.6f);

            // central pole (smoke-hole indicator) - nudged toward the top
            // of the floor mat so it doesn't crowd the firepit underneath.
            var pole = SpawnSprite(ProcGfx.MakeRect(4, 64, ProcGfx.Hex("#8C6420"), ProcGfx.Hex("#5A4014")), new Vector3(0, 1.2f, 0), "Pole");
            pole.GetComponent<SpriteRenderer>().sortingOrder = 1;

            // Lattice walls - extended to 6 bars around a wider ring so the
            // yurt reads as roomy rather than cramped.
            for (int i = 0; i < 6; i++)
            {
                float ang = i * Mathf.PI / 3f;
                var w = SpawnSprite(ProcGfx.MakeRect(96, 8, ProcGfx.Hex("#46301A"), ProcGfx.Hex("#26180C")),
                    new Vector3(Mathf.Cos(ang) * 4.0f, Mathf.Sin(ang) * 3.0f, 0), $"Wall{i}");
                w.transform.rotation = Quaternion.Euler(0, 0, ang * Mathf.Rad2Deg + 90f);
                w.GetComponent<SpriteRenderer>().sortingOrder = -2;
            }

            // Authored-art sprites are PPU 100, much smaller than the procedural
            // shapes they replace. The interior props get a slightly larger
            // scale so the bow/kinzhal/box read clearly without crowding.
            const float artScale = 2.6f;

            // Layout: instead of cramming everything into a 1.5u box, the
            // interactables are spread along a 2.0u radius ring. Bedrolls
            // bookend the upper half of the yurt; ranged-weapon and melee
            // pickups bookend the lower half; the firepit is the visual
            // anchor in the south-center; the box sits up top-left.

            // Firepit / ashes - bottom-center, the warm focal point
            var firepit = SpawnInteractable(new Vector3(0f, -0.4f, 0), MakeFirepit(), "ashes", () =>
                StartCoroutine(DialogueManager.I.Show("V1-05")));
            if (Art.Ashes != null) firepit.GetComponent<Interactable>().SetWorldScale(artScale * 1.1f);

            // Father's bedroll - upper-right, slightly larger for presence
            var dadRoll = SpawnInteractable(new Vector3(2.0f, 1.6f, 0), MakeBedroll(true), "father's bedroll", () =>
                StartCoroutine(DialogueManager.I.Show("V1-06")));
            dadRoll.GetComponent<Interactable>().SetWorldScale(1.2f);

            // Aitugan's bedroll - upper-left, mirrors father's bedroll
            var herRoll = SpawnSprite(MakeBedroll(false), new Vector3(-2.0f, 1.6f, 0), "AituganBedroll", -1);
            herRoll.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

            // Tumar box - mid-left along the wall (away from the bow so the
            // story beat of grabbing the tumar reads on its own).
            var box = SpawnInteractable(new Vector3(-2.4f, 0.2f, 0), MakeTumarBox(), "small wooden box", () =>
                StartCoroutine(GrabTumar()));
            if (Art.Box != null) box.GetComponent<Interactable>().SetWorldScale(artScale);

            // Bow - lower-left, leaning against the wall
            var bowGo = SpawnInteractable(new Vector3(-1.6f, -1.2f, 0), MakeBow(), "bow", () =>
                StartCoroutine(GrabBow()));
            if (Art.Bow != null) bowGo.GetComponent<Interactable>().SetWorldScale(artScale);

            // Kinzhal - lower-right, balancing the bow
            var kinzhal = SpawnInteractable(new Vector3(1.6f, -1.2f, 0), MakeKinzhal(), "kinzhal", () =>
                StartCoroutine(GrabKinzhal()));
            if (Art.Kinzhal != null) kinzhal.GetComponent<Interactable>().SetWorldScale(artScale);

            // Door flap (exit, gated) - bottom-center, just outside the floor mat
            _doorflap = SpawnInteractable(new Vector3(0, -3.2f, 0), MakeDoorflap(), "door flap", () =>
                StartCoroutine(TryExit())).GetComponent<Interactable>();
        }

        Sprite MakeFirepit() => Art.Ashes != null ? Art.Ashes : ProcGfx.MakeCircle(8, ProcGfx.Hex("#1A1208"), ProcGfx.Hex("#5C3A1E"));
        Sprite MakeBedroll(bool fathersHand)
        {
            // Both bedrolls use the procedural colored-rect look (no Father.png).
            var c = fathersHand ? ProcGfx.Hex("#5C3414") : ProcGfx.Hex("#3C2A18");
            return ProcGfx.MakeRect(20, 36, c, ProcGfx.Hex("#1A1108"));
        }
        Sprite MakeTumarBox() => Art.Box != null ? Art.Box : ProcGfx.MakeRect(20, 16, ProcGfx.Hex("#5A3618"), ProcGfx.Hex("#2A1A0C"));
        Sprite MakeBow() => Art.Bow != null ? Art.Bow : ProcGfx.MakeCircle(12, ProcGfx.Hex("#3C2614"), ProcGfx.Hex("#7A5630"));
        Sprite MakeKinzhal() => Art.Kinzhal != null ? Art.Kinzhal : ProcGfx.MakeRect(8, 24, ProcGfx.Hex("#7A5630"), ProcGfx.Hex("#1F1108"));
        Sprite MakeDoorflap() => ProcGfx.MakeRect(48, 12, ProcGfx.Hex("#46301A"), ProcGfx.Hex("#26180C"));

        IEnumerator GrabTumar()
        {
            yield return DialogueManager.I.Show("V1-07");
            yield return DialogueManager.I.Show("V1-08");
            yield return DialogueManager.I.Show("V1-09");
            yield return DialogueManager.I.Show("V1-10");
            GameState.I.hasTumar = true;
        }

        IEnumerator GrabBow()
        {
            yield return DialogueManager.I.Show("V1-11");
            yield return DialogueManager.I.Show("V1-12");
            GameState.I.hasBow = true;
            _gotBow = true;
        }

        IEnumerator GrabKinzhal()
        {
            yield return DialogueManager.I.Show("V1-13");
            GameState.I.hasKinzhal = true;
            _gotKinzhal = true;
        }

        IEnumerator TryExit()
        {
            if (!_gotBow || !_gotKinzhal)
            {
                yield return DialogueManager.I.Show("V1-14");
                yield break;
            }
            // Move outside
            yield return ExitYurt();
        }

        IEnumerator ExitYurt()
        {
            yield return FadeBlack(0.6f, false);
            // Tear the yurt props down and rebuild as exterior
            for (int i = worldRoot.transform.childCount - 1; i >= 0; i--)
                Destroy(worldRoot.transform.GetChild(i).gameObject);

            // Camp exterior: switch to outdoor steppe tile
            groundTile = GroundTile.Ground;
            groundTint = new Color(0.85f, 0.78f, 0.65f);
            SpawnTiledGround();
            // Distant yurt silhouettes - bigger and arranged in a gentle
            // arc so the camp reads as a real settlement instead of a row.
            // The hero yurt (just behind the player) is the largest and
            // centered; satellites flank it at smaller scale and varied depth.
            Sprite yurtSprite = Art.Yurt != null ? Art.Yurt
                : ProcGfx.MakeCircle(20, ProcGfx.Hex("#1A0E06"), ProcGfx.Hex("#46301A"));
            // Layout: (xOffset, yOffset, scaleMultiplier, tint). Tints lifted
            // slightly so the brightened Yurt.png reads warm against the
            // pre-dawn steppe instead of getting flattened by atmospheric
            // perspective.
            float baseS = Art.Yurt != null ? 4.0f : 1.4f;
            var camp = new (float x, float y, float s, float tint)[]
            {
                (-6.2f, 3.6f, 0.80f, 0.68f), // far-left back
                (-3.6f, 3.0f, 0.95f, 0.75f),
                ( 0.0f, 3.8f, 1.20f, 0.88f), // hero yurt, biggest, centered
                ( 3.6f, 3.0f, 0.95f, 0.75f),
                ( 6.2f, 3.6f, 0.80f, 0.68f), // far-right back
            };
            for (int i = 0; i < camp.Length; i++)
            {
                var c = camp[i];
                var y = SpawnSprite(yurtSprite, new Vector3(c.x, c.y, 0), $"FarYurt{i}", -2);
                float s = baseS * c.s;
                y.transform.localScale = new Vector3(s, s * 0.82f, 1f);
                if (Art.Yurt != null)
                    y.GetComponent<SpriteRenderer>().color = new Color(c.tint + 0.05f, c.tint, c.tint - 0.05f);
            }

            // The horse Burken - off to the right, framed against the open
            // steppe so the player can read it as the next objective.
            var horse = SpawnInteractable(new Vector3(3.6f, -0.2f, 0), MakeHorse(), "Burken", () =>
                StartCoroutine(MountAndRide()));
            if (Art.Horse != null) horse.GetComponent<Interactable>().SetWorldScale(3.8f);

            player.transform.position = new Vector3(-0.5f, -1.4f, 0);

            yield return FadeBlack(0.6f, true);

            yield return DialogueManager.I.Show("V1-15");
            yield return DialogueManager.I.Show("V1-16");
            yield return DialogueManager.I.Show("V1-17");
        }

        Sprite MakeHorse() => Art.Horse != null ? Art.Horse : ProcGfx.MakeRect(40, 28, ProcGfx.Hex("#5C3414"), ProcGfx.Hex("#1A0E06"));

        IEnumerator MountAndRide()
        {
            yield return DialogueManager.I.Show("V1-18");
            // Walk player to horse, fade
            float t = 0;
            Vector3 from = player.transform.position;
            Vector3 to = new Vector3(3.6f, -0.2f, 0);
            player.canMove = false;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.4f;
                player.transform.position = Vector3.Lerp(from, to, t);
                yield return null;
            }
            yield return DialogueManager.I.Show("V1-19");
            yield return FadeBlack(1.2f, false);
            _exitTriggered = true;
        }

        protected override IEnumerator Play()
        {
            yield return FadeBlack(0.8f, true);
            yield return DialogueManager.I.Show("V1-01");
            yield return DialogueManager.I.Show("V1-02");
            yield return DialogueManager.I.Show("V1-03");
            yield return DialogueManager.I.Show("V1-04");

            while (!_exitTriggered) yield return null;
            yield return new WaitForSeconds(0.6f);
        }

        bool _exitTriggered = false;
    }
}
