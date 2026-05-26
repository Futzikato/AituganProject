using UnityEngine;

namespace Aitugan.Player
{
    public enum ArrowKind { Standard, Fire, Wind }

    /// <summary>
    /// Simple kinematic projectile. Damages enemies on contact, then despawns.
    /// Wind variant applies knockback instead of damage. Fire variant carries
    /// an extra "ignites shield" flag for the V2 shielded Dzungars.
    /// </summary>
    public class Arrow : MonoBehaviour
    {
        public ArrowKind kind = ArrowKind.Standard;
        public Vector2 velocity;
        public float lifetime = 2.5f;
        public int damage = 1;

        SpriteRenderer _sr;

        void Awake()
        {
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = 50;

            var col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.35f, 0.1f);
            col.isTrigger = true;
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;
        }

        public void Setup(ArrowKind k, Sprite sprite, Vector2 vel)
        {
            kind = k;
            velocity = vel;
            _sr.sprite = sprite;
            _sr.color = k switch
            {
                ArrowKind.Fire => new Color(1f, 0.55f, 0.20f),
                ArrowKind.Wind => new Color(0.65f, 0.85f, 1f),
                _ => Color.white,
            };
            // Authored Arrow_.png is small; scale up to match world units.
            transform.localScale = new Vector3(2f, 2f, 1f);
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg);
            damage = (k == ArrowKind.Wind) ? 0 : 1;
        }

        void Update()
        {
            transform.position += (Vector3)(velocity * Time.deltaTime);
            lifetime -= Time.deltaTime;
            if (lifetime <= 0) Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var enemy = other.GetComponent<Aitugan.Enemies.EnemyBase>();
            if (enemy != null)
            {
                if (kind == ArrowKind.Wind)
                {
                    enemy.Knockback((Vector2)(other.transform.position - transform.position).normalized * 6f, 0.4f);
                }
                else if (enemy.shielded && kind != ArrowKind.Fire)
                {
                    // pinged off the shield - no damage
                    Destroy(gameObject);
                    return;
                }
                else
                {
                    enemy.Damage(damage);
                }
                Destroy(gameObject);
                return;
            }
        }
    }
}
