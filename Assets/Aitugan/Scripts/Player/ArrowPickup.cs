using UnityEngine;
using Aitugan.Core;

namespace Aitugan.Player
{
    /// <summary>
    /// A bundle of arrows on the ground. Player walks over it to pick up.
    /// Used between V2 waves and at V5 climb start so the player isn't soft-locked
    /// when their quiver runs dry.
    /// </summary>
    public class ArrowPickup : MonoBehaviour
    {
        public int amount = 12;
        SpriteRenderer _sr;
        float _bobT;
        Vector3 _basePos;

        void Awake()
        {
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = ProcGfx.MakeRect(16, 6, ProcGfx.Hex("#8C5C2E"), ProcGfx.Hex("#FFE0A0"));
            _sr.sortingOrder = 4;
            var c = gameObject.AddComponent<CircleCollider2D>();
            c.radius = 0.45f;
            c.isTrigger = true;
        }

        void Start() { _basePos = transform.position; }

        void Update()
        {
            _bobT += Time.deltaTime;
            transform.position = _basePos + new Vector3(0, Mathf.Sin(_bobT * 3f) * 0.06f, 0);
            // glint pulse
            float k = 0.85f + Mathf.Sin(_bobT * 5f) * 0.15f;
            _sr.color = new Color(1f, k, k * 0.7f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<AituganController>() == null) return;
            GameState.I.arrows += amount;
            Destroy(gameObject);
        }
    }
}
